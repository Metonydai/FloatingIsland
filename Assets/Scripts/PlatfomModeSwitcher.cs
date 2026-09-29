using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;

public class PlatformModeSwitcher : MonoBehaviour
{
    [Header("Rigs")]
    public GameObject desktopRig;
    public GameObject xrRig;

    [Header("UI Canvases")]
    public Canvas desktopCanvas;    // 原本的 Screen Space - Overlay Canvas
    public Canvas vrCanvas;

    public static bool IsVRMode { get; private set; }

    private IEnumerator Start()
    {
        // 稍微等待 1~2 幀，確保 XR Subsystem 與 OpenXR 完成連線初始化
        yield return new WaitForSeconds(0.2f);

        IsVRMode = CheckVRPresence();
        ApplyPlatformMode(IsVRMode);
    }

    private bool CheckVRPresence()
    {
        // 1. 檢查 XR General Settings 是否有運作中的 Loader
        var xrManager = XRGeneralSettings.Instance?.Manager;
        if (xrManager != null && xrManager.activeLoader != null)
        {
            return true;
        }

        // 2. 備用檢查：檢查頭戴裝置 InputDevice
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);
        if (inputDevices.Count > 0)
        {
            return true;
        }

        // 3. 舊版/通用 XR 旗標
        return XRSettings.isDeviceActive;
    }

    public void ApplyPlatformMode(bool vrEnabled)
    {
        IsVRMode = vrEnabled;

        if (desktopRig != null) desktopRig.SetActive(!vrEnabled);
        if (desktopCanvas != null) desktopCanvas.gameObject.SetActive(!vrEnabled);

        if (xrRig != null) xrRig.SetActive(vrEnabled);
        if (vrCanvas != null) vrCanvas.gameObject.SetActive(vrEnabled);

        Debug.Log($"[PlatformModeSwitcher] Active Mode: {(vrEnabled ? "VR Mode" : "Desktop Mode")}");
    }
}