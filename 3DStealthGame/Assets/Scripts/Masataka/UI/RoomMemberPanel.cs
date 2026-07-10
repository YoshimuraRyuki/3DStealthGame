using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ルーム内のメンバー表示を更新する。
/// 入室、退出、準備状態の変更をUIに反映する。
/// </summary>
public class RoomMemberPanel : MonoBehaviour
{
	#region インスペクター設定

	[Header("メンバー表示")]
	public Transform memberListParent;
	public GameObject memberRowPrefab;

	#endregion

	#region 内部状態

	private readonly Dictionary<string, GameObject> _memberRows = new Dictionary<string, GameObject>();
	private readonly Dictionary<string, (string name, bool isReady)> _members
		= new Dictionary<string, (string, bool)>();

	#endregion

	#region 公開メソッド

	public void AddOrUpdateMember(string playerId, string playerName, bool isReady = false)
	{
		if (string.IsNullOrEmpty(playerId)) return;

		_members[playerId] = (playerName, isReady);
		RefreshUI();
	}

	public void SetReady(string playerId, bool isReady)
	{
		if (string.IsNullOrEmpty(playerId)) return;
		if (!_members.ContainsKey(playerId)) return;

		var member = _members[playerId];
		_members[playerId] = (member.name, isReady);

		RefreshUI();
	}

	public void RemoveMember(string playerId)
	{
		if (string.IsNullOrEmpty(playerId)) return;

		_members.Remove(playerId);
		RefreshUI();
	}

	public void ClearAll()
	{
		_members.Clear();
		RefreshUI();
	}

	#endregion

	#region UI更新

	private void RefreshUI()
	{
		ClearRows();

		if (memberListParent == null || memberRowPrefab == null) return;

		foreach (var member in _members)
		{
			GameObject row = Instantiate(memberRowPrefab, memberListParent);
			Text[] texts = row.GetComponentsInChildren<Text>();

			if (texts.Length >= 2)
			{
				texts[0].text = member.Value.name;
				texts[1].text = member.Value.isReady ? "✔ 準備完了" : "…待機中";
				texts[1].color = member.Value.isReady ? Color.green : Color.gray;
			}

			_memberRows[member.Key] = row;
		}
	}

	private void ClearRows()
	{
		foreach (var row in _memberRows.Values)
		{
			if (row != null)
			{
				Destroy(row);
			}
		}

		_memberRows.Clear();
	}

	#endregion
}