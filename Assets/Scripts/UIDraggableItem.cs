using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    public Image iconImage;

    private void Start()
    {
        if (itemData != null && iconImage != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
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