using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToTitle : MonoBehaviour
{
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.A))
		{
			SceneManager.LoadScene("Title");
		}
	}
}