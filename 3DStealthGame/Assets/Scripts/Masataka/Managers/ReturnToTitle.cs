using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルトシーンからタイトルに戻るクラス。
/// XBOXコントローラーのAボタン、またはキーボードのAキーで遷移する。
/// </summary>
public class ReturnToTitle : MonoBehaviour
{
	#region Unityイベント

	void Update()
	{
		// AボタンまたはAキーでタイトルシーンに遷移
		if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.A))
		{
			SceneManager.LoadScene("Title");
		}
	}

	#endregion
}