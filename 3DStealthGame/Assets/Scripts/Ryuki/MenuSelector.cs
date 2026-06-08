using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.InputSystem.EnhancedTouch;

public class MenuSelector : MonoBehaviour
{
    [SerializeField] private InputField inputField;
    [SerializeField] private GameObject selectionVisual;

    [SerializeField] private GameObject nameInputButton;
    private bool isEditing;

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            Debug.Log(
                "現在選択中: " +
                EventSystem.current.currentSelectedGameObject.name
            );
        }
        else
        {
            Debug.Log("現在選択中: なし");
        }

        if (!isEditing) return;

        // Bボタンで入力終了
        if (Gamepad.current != null &&
            Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            EndEdit();
        }
    }

    private void BlinkArrow()
    {
        selectionVisual.SetActive(!selectionVisual.activeSelf);
    }

    // Aボタンで呼ぶ
    public void StartEdit()
    {
        isEditing = true;

        inputField.interactable = true;
        inputField.ActivateInputField();
        inputField.Select();

        // 点滅開始
        InvokeRepeating(nameof(BlinkArrow), 0.3f, 0.5f);
    }

    private void EndEdit()
    {
        isEditing = false;

        inputField.DeactivateInputField();

        // 点滅停止
        CancelInvoke(nameof(BlinkArrow));
        
        // InputFieldから完全にフォーカスを外す
        inputField.enabled = false;

        EventSystem.current.SetSelectedGameObject(null);

        selectionVisual.SetActive(true);

        StartCoroutine(ReturnSelect());
    }

    private IEnumerator ReturnSelect()
    {
        yield return null;

        inputField.enabled = true;

        EventSystem.current.SetSelectedGameObject(nameInputButton);
    }
}