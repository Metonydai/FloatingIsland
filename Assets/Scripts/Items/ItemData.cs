using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Sandbox/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;               // UI 顯示的圖標
    public GameObject prefab;         // 正式放置的物件
    public GameObject previewPrefab;  // 拖曳中的半透明/Ghost 預覽物件 (可選)
    public Vector3 placementOffset = Vector3.zero; // 針對不同原點微調高度
}
