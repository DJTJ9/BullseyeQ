using System;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsController : MonoBehaviour
{
    private VisualElement _modalOverlay;
    private Action        _pendingAction;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _modalOverlay = root.Q<VisualElement>("modal-overlay");

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
        if (_modalOverlay != null) _modalOverlay.style.display = DisplayStyle.Flex;
    }

    private void ConfirmModal()
    {
        _pendingAction?.Invoke();
        _pendingAction = null;
    }

    private void HideModal()
    {
        _pendingAction = null;
        if (_modalOverlay != null) _modalOverlay.style.display = DisplayStyle.None;
    }
}
