using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RoomMemberPanel : MonoBehaviour
{
	[Header("メンバー表示")]
	public Transform memberListParent;  // メンバー行を並べる縦レイアウトのTransform
	public GameObject memberRowPrefab;  // Text x2（名前・状態）を持つPrefab

	private Dictionary<string, GameObject> _memberRows = new Dictionary<string, GameObject>();
	// key: playerId, value: { name, isReady }
	private Dictionary<string, (string name, bool isReady)> _members
		= new Dictionary<string, (string, bool)>();

	/// <summary>メンバー追加 or 更新</summary>
	public void AddOrUpdateMember(string playerId, string playerName, bool isReady = false)
	{
		_members[playerId] = (playerName, isReady);
		RefreshUI();
	}

	/// <summary>準備完了状態の更新</summary>
	public void SetReady(string playerId, bool isReady)
	{
		if (_members.ContainsKey(playerId))
		{
			var m = _members[playerId];
			_members[playerId] = (m.name, isReady);
			RefreshUI();
		}
	}

	/// <summary>メンバー削除</summary>
	public void RemoveMember(string playerId)
	{
		_members.Remove(playerId);
		RefreshUI();
	}

	/// <summary>全クリア</summary>
	public void ClearAll()
	{
		_members.Clear();
		RefreshUI();
	}

	private void RefreshUI()
	{
		// 既存の行を全削除して再生成（シンプル方式）
		foreach (var row in _memberRows.Values)
			Destroy(row);
		_memberRows.Clear();

		foreach (var kv in _members)
		{
			GameObject row = Instantiate(memberRowPrefab, memberListParent);
			Text[] texts = row.GetComponentsInChildren<Text>();
			// texts[0] = 名前, texts[1] = 状態
			if (texts.Length >= 2)
			{
				texts[0].text = kv.Value.name;
				texts[1].text = kv.Value.isReady ? "✔ 準備完了" : "…待機中";
				texts[1].color = kv.Value.isReady ? Color.green : Color.gray;
			}
			_memberRows[kv.Key] = row;
		}
	}
}