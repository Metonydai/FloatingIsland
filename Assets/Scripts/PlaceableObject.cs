using Unity.VisualScripting;
using UnityEngine;

public class PlaceableObject : MonoBehaviour
{
    [Header("Menu Position Offset")]
    public Vector3 menuOffset = new Vector3(0, 1.2f, 0);

    public void DeleteSelf()
    {
        // 之後可以在這裡加粒子消散或動畫
        Destroy(gameObject);
    }
}