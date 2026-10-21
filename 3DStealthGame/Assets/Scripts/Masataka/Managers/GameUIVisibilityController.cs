using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UIの表示・非表示を切り替える。
/// キーボード: H / ゲームパッド: X・□
/// </summary>
public class GameUIVisibilityController : MonoBehaviour
{
	[SerializeField] private GameObject uiRoot;
	[SerializeField] private bool initiallyVisible = true;

	private bool _isVisible;

	private void Start()
	{
		_isVisible = initiallyVisible;
		ApplyVisibility();
	}

	private void Update()
	{
		if (IsTogglePressed())
		{
			_isVisible = !_isVisible;
			ApplyVisibility();
		}
	}

	private bool IsTogglePressed()
	{
		if (Keyboard.current != null &&
			Keyboard.current.hKey.wasPressedThisFrame)
		{
			return true;
		}

		if (CoopGameSession.Instance == null &&
			Gamepad.current != null &&
			Gamepad.current.buttonWest.wasPressedThisFrame)
		{
			return true;
		}

		return false;
	}

	private void ApplyVisibility()
	{
		if (uiRoot != null)
		{
			uiRoot.SetActive(_isVisible);
		}
	}
}