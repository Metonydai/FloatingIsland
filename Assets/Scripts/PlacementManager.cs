using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

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

    private void Awake()
    {
        Instance = this;
        if (guideLine != null) guideLine.enabled = false;
        skyPlane = new Plane(Vector3.up, new Vector3(0, skyPlaneHeight, 0));
    }

    private void Update()
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
}