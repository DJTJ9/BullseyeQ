using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Settings panel: reduce-motion toggle (persisted in PlayerPrefs, applied as a root class)
/// and the reset-stats confirmation modal.
/// </summary>
public class SettingsController : MonoBehaviour
{
    private const string ReduceMotionKey = "bq_reduce_motion";

    private VisualElement _modalOverlay;
    private Action        _pendingAction;

    void OnEnable()
    {
        var root    = GetComponent<UIDocument>().rootVisualElement;
        var appRoot = root.Q<VisualElement>("root");

        _modalOverlay = root.Q<VisualElement>("modal-overlay");

        bool reduced = PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
        if (appRoot != null) UiFx.SetReducedMotion(appRoot, reduced);

        var toggle = root.Q<Toggle>("settings-toggle-motion");
        if (toggle != null)
        {
            toggle.SetValueWithoutNotify(reduced);
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (appRoot != null) UiFx.SetReducedMotion(appRoot, evt.newValue);
                PlayerPrefs.SetInt(ReduceMotionKey, evt.newValue ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        root.Q<Button>("settings-btn-reset-stats")?.RegisterCallback<ClickEvent>(_ =>
            ShowModal(() =>
            {
                DataManager.Instance.ResetAllStats();
                HideModal();
            }));

        root.Q<Button>("modal-btn-yes")?.RegisterCallback<ClickEvent>(_ => ConfirmModal());
        root.Q<Button>("modal-btn-cancel")?.RegisterCallback<ClickEvent>(_ => HideModal());
    }

    private void ShowModal(Action onConfirm)
    {
        _pendingAction = onConfirm;
        if (_modalOverlay != null) UiFx.Show(_modalOverlay);
    }

    private void ConfirmModal()
    {
        _pendingAction?.Invoke();
        _pendingAction = null;
    }

    private void HideModal()
    {
        _pendingAction = null;
        if (_modalOverlay != null) UiFx.Hide(_modalOverlay);
    }
}
