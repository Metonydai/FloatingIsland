using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDropdownController : MonoBehaviour
{
    [Header("UI References")]
    public Button headerButton;

    public GameObject contentList;

    public TextMeshProUGUI headerText;

    [Header("Settings")]
    public string menuTitle = "Item Lists";
    public bool isExpanded = false; // 預設是否展開

    private void Awake()
    {
        if (headerButton != null)
        {
            headerButton.onClick.AddListener(ToggleDropdown);
        }

        // 依照預設狀態初始化
        SetDropdownState(isExpanded);
    }

    /// 切換開關狀態
    public void ToggleDropdown()
    {
        SetDropdownState(!isExpanded);
    }

    /// 設定展開或收合
    public void SetDropdownState(bool expand)
    {
        isExpanded = expand;

        if (contentList != null)
        {
            contentList.SetActive(isExpanded);
        }

        if (headerText != null)
        {
            // 展開時箭頭朝上 ▲，收合時箭頭朝下 ▼
            string arrow = isExpanded ? "▲" : "▼";
            headerText.text = $"{menuTitle} {arrow}";
        }
    }
}