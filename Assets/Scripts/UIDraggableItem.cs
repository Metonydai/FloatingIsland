using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    private Image cachedIcon;

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
        if (itemData != null)
        {
            PlacementManager.Instance.StartPlacement(itemData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖曳事件維持活躍，射線邏輯已在 PlacementManager 內更新
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PlacementManager.Instance.EndPlacement();
    }
}