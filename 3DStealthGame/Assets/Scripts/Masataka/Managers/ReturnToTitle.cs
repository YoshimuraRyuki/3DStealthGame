using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// リザルト画面からタイトルへ戻る。
/// Aボタン、またはAキーで操作する。
/// </summary>
public class ReturnToTitle : MonoBehaviour
{
	#region 内部状態

	private bool _isReturning = false;

	#endregion

	#region Unityイベント

	private void Update()
	{
		if (_isReturning) return;

		bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
		bool key = Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame;

		if (pad || key)
		{
			ReturnTitle();
		}
	}

	#endregion

	#region タイトル復帰

	private async void ReturnTitle()
	{
		if (_isReturning) return;
		_isReturning = true;

		WebSocketClient wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient != null)
		{
			await wsClient.DisconnectAndReset();
		}

		ResultData.Reset();

		SceneManager.LoadScene("Title");
	}

	#endregion
}