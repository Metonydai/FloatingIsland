using UnityEngine;

public class GuideLineFlow : MonoBehaviour
{
    public float scrollSpeed = 2.0f; // 箭頭流動速度

    private LineRenderer lr;
    private Material mat;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        mat = lr.material;
    }

    private void Update()
    {
        if (lr.enabled && mat != null)
        {
            float offset = Time.time * scrollSpeed;
            mat.SetTextureOffset("_BaseMap", new Vector2(-offset, 0f));
        }
    }
}