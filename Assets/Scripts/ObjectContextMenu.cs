using UnityEngine;
using UnityEngine.UI;

public class ObjectContextMenu : MonoBehaviour
{
    public static ObjectContextMenu Instance { get; private set; }

    [Header("UI Reference")]
    public Button deleteButton;

    private PlaceableObject currentTarget;

    private void Awake()
    {
        Instance = this;
        
        // 綁定刪除按鈕事件
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        // 初始隱藏
        Hide();
    }

    private void Update()
    {
        // 面向相機 (Billboard)
        if (gameObject.activeSelf && Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - Camera.main.transform.position
            );
        }
    }

    // 顯示並移動到目標物件頭頂
    public void Show(PlaceableObject target)
    {
        currentTarget = target;
        transform.position = target.transform.position + target.menuOffset;
        gameObject.SetActive(true);
    }

    // 隱藏選單
    public void Hide()
    {
        currentTarget = null;
        gameObject.SetActive(false);
    }

    private void OnDeleteClicked()
    {
        if (currentTarget != null)
        {
            currentTarget.DeleteSelf();
        }
        Hide();
    }
}
