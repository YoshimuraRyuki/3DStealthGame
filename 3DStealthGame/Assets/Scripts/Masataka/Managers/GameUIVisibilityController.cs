using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GameScene上のUI表示/非表示を切り替える。
/// キーボード: H
/// ゲームパッド：Ⓧ
/// </summary>
public class GameUIVisibilityController : MonoBehaviour
{
	[SerializeField] private GameObject uiRoot;

	private bool _isVisible = true;

	private void Update()
	{
		if (IsTogglePressed())
		{
			ToggleUI();
		}
	}

	private bool IsTogglePressed()
	{
		// キーボード H
		if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
		{
			return true;
		}

		// ゲームパッド 左フェイスボタン
		// Xbox: Xボタン / PlayStation: □ボタン
		if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
		{
			return true;
		}

		return false;
	}

	private void ToggleUI()
	{
		_isVisible = !_isVisible;

		if (uiRoot != null)
		{
			uiRoot.SetActive(_isVisible);
		}
	}
}