using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player1") || other.CompareTag("Player2"))
		{
			PlayerController playerRespawn = other.GetComponent<PlayerController>();
			if (playerRespawn != null) // isLocalPlayerチェックを外す
			{
				playerRespawn.SetRespawnPoint(transform);
			}
		}
	}
}
