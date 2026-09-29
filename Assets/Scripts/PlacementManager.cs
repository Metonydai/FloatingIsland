using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("VR Settings")]
    public XRInteractionManager xrInteractionManager;
    public NearFarInteractor vrRightRayInteractor;
    public float vrSpawnDistance = 0.6f;
    public InputActionProperty vrClickAction;
    public InputActionProperty vrGripAction;

    [Header("Layer Masks")]
    public LayerMask terrainLayer;
    public LayerMask placeableLayer;

    [Header("Line Visual")]
    public LineRenderer guideLine;
    public float skyPlaneHeight = 3.5f;
    public float dropDuration = 0.18f;

    private ItemData currentItemData;
    private GameObject currentGhost;
    private bool isDragging = false;
    private bool hasValidGround = false;
    private Vector3 dropTargetPos;
    private Quaternion dropTargetRot;
    private Plane skyPlane;

    private GameObject vrCurrentHeldObject;
    private ItemData vrCurrentItemData;
    private bool isVRDragging = false;
    private bool vrHasValidGround = false;
    private Vector3 vrDropTargetPos;
    private Quaternion vrDropTargetRot;

    private void Awake()
    {
        Instance = this;
        if (guideLine != null) guideLine.enabled = false;
        skyPlane = new Plane(Vector3.up, new Vector3(0, skyPlaneHeight, 0));
    }

    private void OnEnable()
    {
        // 啟用 VR 點擊按鍵動作
        if (vrClickAction.action != null)
            vrClickAction.action.Enable();

        if (vrGripAction.action != null) 
            vrGripAction.action.Enable();
    }

    private void OnDisable()
    {
        if (vrClickAction.action != null)
            vrClickAction.action.Disable();
        if (vrGripAction.action != null) 
            vrGripAction.action.Disable();
    }

    public bool IsVRGripPressedThisFrame()
    {
        return vrGripAction.action != null && vrGripAction.action.WasPressedThisFrame();
    }

    private void Update()
    {
        if (PlatformModeSwitcher.IsVRMode)
        {
            if (isVRDragging)
            {
                UpdateVRGuideLine();

                bool gripReleased = vrGripAction.action != null && vrGripAction.action.WasReleasedThisFrame();
                bool triggerReleased = vrClickAction.action != null && vrClickAction.action.WasReleasedThisFrame();

                if (gripReleased || triggerReleased)
                {
                    Debug.Log($"[PlacementManager] 偵測到放開按鍵！grip: {gripReleased}, trigger: {triggerReleased}");
                    EndVRPlacement();
                }
            }
            else
            {
                HandleVRSelection();
            }
        }
        else
        {
            if (isDragging)
            {
                HandleHighSkyDrag();
            }
            else
            {
                HandleSelection();
            }
        }
    }

    public void SpawnAndGrabInVR(ItemData data)
    {
        if (data == null || data.prefab == null) return;
        if (vrRightRayInteractor == null || xrInteractionManager == null)
        {
            Debug.LogWarning("[PlacementManager] 請在 Inspector 綁定 xrInteractionManager 與 vrRightRayInteractor！");
            return;
        }

        vrCurrentItemData = data;

        Transform handTransform = vrRightRayInteractor.transform;
        Ray ray = new Ray(handTransform.position, handTransform.forward);

        Vector3 spawnPos;
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            spawnPos = hit.point - ray.direction * 0.05f;
        }
        else
        {
            spawnPos = ray.origin + ray.direction * vrSpawnDistance;
        }

        // 3. 在射線末端位置生成 Prefab 實體
        vrCurrentHeldObject = Instantiate(data.prefab, spawnPos, Quaternion.identity);
        vrCurrentHeldObject.SetActive(true);

        // 關閉實體 Collider 避免干擾射線追蹤
        Collider[] colliders = vrCurrentHeldObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        XRGrabInteractable grabInteractable = vrCurrentHeldObject.GetComponentInChildren<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            isVRDragging = true;

            // 強制讓射線抓住物件
            xrInteractionManager.SelectEnter((IXRSelectInteractor)vrRightRayInteractor, (IXRSelectInteractable)grabInteractable);
        }
    }

    // 由手把鬆開時主動呼叫
    private void EndVRPlacement()
    {
        if (!isVRDragging) return;
        isVRDragging = false;

        if (guideLine != null) guideLine.enabled = false;

        GameObject releasedObj = vrCurrentHeldObject;
        if (releasedObj == null) return;

        // 1. 強制讓 Interactor 放開物體，切斷手把控制線
        XRGrabInteractable grabInteractable = releasedObj.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null && vrRightRayInteractor != null)
        {
            if (grabInteractable.isSelected)
            {
                xrInteractionManager.SelectExit((IXRSelectInteractor)vrRightRayInteractor, (IXRSelectInteractable)grabInteractable);
            }
        }

        // 2. 檢查放開時是否停留在 UI 上
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // 3. 落在有效地面上且不在 UI 上 -> 執行動畫落下
        if (vrHasValidGround)
        {
            Vector3 startPos = releasedObj.transform.position;
            Collider[] colliders = releasedObj.GetComponentsInChildren<Collider>();

            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Rigidbody rb = releasedObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            StartCoroutine(AnimateDrop(releasedObj.transform, startPos, vrDropTargetPos, dropDuration, colliders));
            Debug.Log("[PlacementManager] VR 物品合法吸附落下！");
        }
        else
        {
            // 落在 UI 上或無效地面 -> 銷毀
            Debug.Log("[PlacementManager] 放開在無效區域或 UI 上，銷毀物件。");
            Destroy(releasedObj);
        }

        vrCurrentHeldObject = null;
        vrCurrentItemData = null;
    }

    private void UpdateVRGuideLine()
    {
        if (vrCurrentHeldObject == null)
        {
            if (guideLine != null) guideLine.enabled = false;
            return;
        }

        Vector3 objPos = vrCurrentHeldObject.transform.position;
        Ray downRay = new Ray(objPos, Vector3.down);

        if (Physics.Raycast(downRay, out RaycastHit groundHit, 50f, terrainLayer))
        {
            vrHasValidGround = true;
            vrDropTargetPos = groundHit.point + (vrCurrentItemData != null ? vrCurrentItemData.placementOffset : Vector3.zero);
            vrDropTargetRot = Quaternion.FromToRotation(Vector3.up, groundHit.normal);

            if (guideLine != null)
            {
                guideLine.enabled = true;
                guideLine.SetPosition(0, objPos);
                guideLine.SetPosition(1, groundHit.point);
            }
        }
        else
        {
            vrHasValidGround = false;
            if (guideLine != null) guideLine.enabled = false;
        }
    }

    private void OnVRObjectReleased(SelectExitEventArgs args)
    {
        isVRDragging = false;
        if (guideLine != null) guideLine.enabled = false;

        GameObject releasedObj = args.interactableObject.transform.gameObject;
        XRGrabInteractable grabInteractable = releasedObj.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnVRObjectReleased);
        }

        // 檢查放開時是否停留在 UI 上
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (vrHasValidGround && !isPointerOverUI && releasedObj != null)
        {
            Vector3 startPos = releasedObj.transform.position;
            Collider[] colliders = releasedObj.GetComponentsInChildren<Collider>();

            // 下墜動畫期間暫時關閉碰撞體，避免卡在半空中
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 確保剛體保持 Kinematic，不受重力干擾
            Rigidbody rb = releasedObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // 走自訂的 AnimateDrop (帶果凍彈跳 AnimateBounce)
            StartCoroutine(AnimateDrop(releasedObj.transform, startPos, vrDropTargetPos, dropDuration, colliders));
            Debug.Log("[PlacementManager] VR 物品執行自訂動畫下墜！");
        }
        else
        {
            // 落在 UI 上或無效地面 -> 取消擺放並銷毀
            if (releasedObj != null)
            {
                Destroy(releasedObj);
            }
        }

        vrCurrentHeldObject = null;
        vrCurrentItemData = null;
    }

    // 由 UI 呼叫：開始拖曳
    public void StartPlacement(ItemData data)
    {
        currentItemData = data;
        isDragging = true;
        hasValidGround = false;

        // 如果有特定的 Ghost Prefab 就用它，沒有就先用正式 Prefab
        GameObject ghostToInstantiate = data.previewPrefab != null ? data.previewPrefab : data.prefab;
        currentGhost = Instantiate(ghostToInstantiate);
        
        // 確保 Ghost 上的 Collider 關閉，避免 Raycast 打中自己
        foreach (var col in currentGhost.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        if (guideLine != null) guideLine.enabled = true;
    }

    // 拖曳中的每幀更新
    private void HandleHighSkyDrag()
    {

        // 如果游標在 UI 上，隱藏 Ghost
        if (EventSystem.current.IsPointerOverGameObject())
        {
            hasValidGround = false;
            if (currentGhost != null) currentGhost.SetActive(false);
            if (guideLine != null) guideLine.enabled = false;
            return;
        }

        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 偵測地形
        if (skyPlane.Raycast(camRay, out float enterDist) && enterDist > 0f && enterDist < 100f)
        {
            Vector3 skyPos = camRay.GetPoint(enterDist);

            if (currentGhost != null)
            {
                currentGhost.SetActive(true);
                currentGhost.transform.position = skyPos;
                currentGhost.transform.rotation = Quaternion.identity;
            }

            Ray downRay = new Ray(skyPos, Vector3.down);
            if (Physics.Raycast(downRay, out RaycastHit groundHit, 50f, terrainLayer))
            {
                hasValidGround = true;
                dropTargetPos = groundHit.point + currentItemData.placementOffset;
                dropTargetRot = Quaternion.FromToRotation(Vector3.up, groundHit.normal);

                // 3. 指引線：從高空 Ghost 垂直連向曲面精確落點
                if (guideLine != null)
                {
                    guideLine.enabled = true;
                    guideLine.SetPosition(0, skyPos);
                    guideLine.SetPosition(1, groundHit.point);
                }
            }
            else
            {
                // 高空位置的正下方沒有曲面（移出邊界/懸空）
                hasValidGround = false;
                if (guideLine != null) guideLine.enabled = false;
                if (currentGhost != null) currentGhost.SetActive(false);
            }
        }
        else
        {
            hasValidGround = false;
            if (currentGhost != null) currentGhost.SetActive(false);
            if (guideLine != null) guideLine.enabled = false;
        }
    }

    // 由 UI 呼叫：放開滑鼠結束拖曳
    public void EndPlacement()
    {
        if (!isDragging) return;
        isDragging = false;

        if (guideLine != null) guideLine.enabled = false;

        // 判定放開時是否落在有效曲面/平面上，且游標不在 UI 上
        if (hasValidGround && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 dropStartPos = currentGhost.transform.position;
            GameObject placeObj = Instantiate(currentItemData.prefab, dropStartPos, dropTargetRot);

            Collider[] colliders = placeObj.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Rigidbody rb = placeObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            StartCoroutine(AnimateDrop(placeObj.transform, dropStartPos, dropTargetPos, dropDuration, colliders));
        }

        // 清理 Ghost 預覽物件
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }

        currentItemData = null;
    }

    // 使用協程讓物件從空中精準掉落並吸附至目標點，帶有輕微落地微彈跳
    private IEnumerator AnimateDrop(Transform targetTransform, Vector3 startPos, Vector3 endPos, float duration, Collider[] colliders)
    {
        float elapsed = 0f;
        Vector3 originalScale = targetTransform.localScale;

        while (elapsed < duration)
        {
            if (targetTransform == null) yield break; // 防呆：避免中途被銷毀

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 使用 Ease-In (t^2) 模擬重力加速度下墜
            float easeInT = t * t;
            targetTransform.position = Vector3.Lerp(startPos, endPos, easeInT);

            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.position = endPos; // 100% 精確錨定在曲面目標點

            // 落地果凍微彈跳動效 (Squash & Stretch)
            yield return StartCoroutine(AnimateBounce(targetTransform, originalScale));

            if (colliders != null)
            {
                foreach (var col in colliders)
                {
                    if (col != null) col.enabled = true;
                }
            }

            Rigidbody rb = targetTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    private IEnumerator AnimateBounce(Transform targetTransform, Vector3 originalScale)
    {
        float bounceDuration = 0.12f;
        float elapsed = 0f;

        // 稍微壓扁橫向擴張
        Vector3 squashedScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.8f, originalScale.z * 1.15f);

        while (elapsed < bounceDuration)
        {
            if (targetTransform == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            targetTransform.localScale = Vector3.Lerp(squashedScale, originalScale, t);
            yield return null;
        }

        if (targetTransform != null)
        {
            targetTransform.localScale = originalScale;
        }
    }

    // 未拖曳時點擊物件開啟選單
    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, placeableLayer))
            {
                PlaceableObject target = hit.collider.GetComponentInParent<PlaceableObject>();
                if (target != null)
                {
                    ObjectContextMenu.Instance.Show(target);
                    return;
                }
            }

            // 點擊空白處關閉已開的選單
            ObjectContextMenu.Instance.Hide();
        }
    }

    private void HandleVRSelection()
    {
        // 檢查右手板機鍵（Trigger / Activate）是否在此幀被按下
        bool isTriggerPressed = vrClickAction.action != null && vrClickAction.action.WasPressedThisFrame();

        if (isTriggerPressed && vrRightRayInteractor != null)
        {
            // 避免點在 World Space UI (例如刪除選單本身或選單面板) 上時重複判定
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Transform handTransform = vrRightRayInteractor.transform;
            Ray ray = new Ray(handTransform.position, handTransform.forward);

            // 朝手把前方發射射線偵測 placeableLayer
            if (Physics.Raycast(ray, out RaycastHit hit, 20f, placeableLayer))
            {
                PlaceableObject target = hit.collider.GetComponentInParent<PlaceableObject>();
                if (target != null)
                {
                    // 彈出刪除選單
                    if (ObjectContextMenu.Instance != null)
                    {
                        ObjectContextMenu.Instance.Show(target);
                    }
                    return;
                }
            }

            // 若打在空白處或非 Placeable 物件，關閉已打開的選單
            if (ObjectContextMenu.Instance != null)
            {
                ObjectContextMenu.Instance.Hide();
            }
        }
    }
}