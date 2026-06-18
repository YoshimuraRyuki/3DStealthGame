using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// リザルト画面からタイトルへ戻るクラス。
/// コントローラーのAボタン、またはキーボードのAキーで遷移する。
/// </summary>
public class ReturnToTitle : MonoBehaviour
{
	#region Unityイベント

	void Update()
	{
		bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
		bool key = Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame;

		if (pad || key)
		{
			SceneManager.LoadScene("Title");
		}
	}

	#endregion
}