using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDraggableItem : MonoBehaviour, 
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public ItemData itemData;
    private Image cachedIcon;
    private bool isHovered = false;

    private void Awake()
    {
        InitializeIcon();
    }

    private void OnValidate()
    {
        // Updates visual representation automatically inside the Editor when itemData changes
        InitializeIcon();
    }

    private void InitializeIcon()
    {
        if (itemData == null) return;

        // Auto-detect the Icon component under children without manual inspector assignment
        if (cachedIcon == null)
        {
            Transform iconTransform = transform.Find("Icon");
            if (iconTransform != null)
            {
                cachedIcon = iconTransform.GetComponent<Image>();
            }
        }

        // Apply sprite from ItemData to the target Image
        if (cachedIcon != null)
        {
            if (itemData.icon != null)
            {
                cachedIcon.sprite = itemData.icon;
                cachedIcon.gameObject.SetActive(true);
            }
            else
            {
                cachedIcon.gameObject.SetActive(false);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!PlatformModeSwitcher.IsVRMode && itemData != null)
        {
            PlacementManager.Instance.StartPlacement(itemData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!PlatformModeSwitcher.IsVRMode)
        {
            PlacementManager.Instance.EndPlacement();
        }
    }

    // VR 
    public void OnPointerDown(PointerEventData eventData)
    {
        if (PlatformModeSwitcher.IsVRMode)
        {
            if (itemData != null)
            {
                PlacementManager.Instance.SpawnAndGrabInVR(itemData);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void Update()
    {
        // 僅在 VR 模式、射線懸停在該按鈕上、且握下 Grip 鍵時觸發
        if (PlatformModeSwitcher.IsVRMode && isHovered && itemData != null)
        {
            if (PlacementManager.Instance != null && PlacementManager.Instance.IsVRGripPressedThisFrame())
            {
                PlacementManager.Instance.SpawnAndGrabInVR(itemData);
            }
        }
    }

}