using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
#if UNITY_EDITOR
using static UnityEditor.Experimental.GraphView.GraphView;
#endif

public class ElementGenerator : MonoBehaviour
{
	#region 宣言
	// リソース格納用
	Material material;                                              // 壁のマテリアル
	GameObject objGoal;                                             // ゴール
	GameObject[] objEnemyList = new GameObject[1];                  // 敵リスト
	GameObject[] objSuperEnemyList = new GameObject[1];             // 強化敵リスト
	public GameObject[] objItemList = new GameObject[1];            // アイテムリスト
	public GameObject[] objSwitchList = new GameObject[1];          // スイッチリスト
	public GameObject[] objPatrolPointList = new GameObject[1];     // 巡回ポイントリスト
	GameObject[] objMapTipList = new GameObject[4];                 // マップチップリスト

	// マップ軽くするためのプレファブ
	[SerializeField] GameObject wallPrefab;

	// ミニマップ生成スクリプト用
	//MapGenerate mapGenerate;
	FixedMap mapGenerate;
	int[,] map;
	// プレイヤー検索用
	int playerX = 0;
	int playerY = 0;
	// 敵検索用
	int enemyX = 0;
	int enemyY = 0;
	// 現在プレイヤーがいるマス
	int currentPlayerX;
	int currentPlayerY;
	// 前回プレイヤーがいたマス
	int oldPlayerX;
	int oldPlayerY;
	// 敵のマス
	List<Vector2Int> oldEnemyPositions = new List<Vector2Int>();
	// 敵の視野UI
	[SerializeField] GameObject enemyViewPrefab;
	List<GameObject> viewList = new List<GameObject>();

	[SerializeField] RectTransform miniMapMaskRect;
	// ミニマップ位置座標
	int mapX = 45;
	int mapY = 145;

	[Header("プレイヤー1のミニマップ")]
	[SerializeField] RectTransform map2DRect_P1;
	[SerializeField] RectTransform miniMapMaskRect_P1;

	[Header("プレイヤー2のミニマップ")]
	[SerializeField] RectTransform map2DRect_P2;
	[SerializeField] RectTransform miniMapMaskRect_P2;

	GameObject[,] objMapExist_P1;
	GameObject[,] objMapExist_P2;

	Transform remotePlayer;   // 相手のTransform
	int currentRemoteX, currentRemoteY;
	int oldRemoteX, oldRemoteY;

	// パス読み込み用
	GameObject objMap2D;                                            // Map2D
	GameObject objPlayer;                                           // プレイヤー
	GameObject[] objEnemys;                                         // 敵
	[SerializeField] Transform player;
	[SerializeField] RectTransform map2DRect;
	float cellSize = 5f;
	[SerializeField] float miniMapScale = 1.0f;

	// 生成したマップチップ
	GameObject[,] objMapExist;                                      // フィールド用
	#endregion

	#region 初期化処理
	void Awake()
	{
		// リソース読み込み
		ReadResources();

		// 壁を生成
		GenerateWall();

		// 敵・アイテム・ゴール・プレイヤー初期配置決め
		GenerateObjectsCSV();

		// 二次元マップ生成
		//GenerateMap2D(map);

		// WebSocketClientからプレイヤーを取得
		var wsClient = FindObjectOfType<WebSocketClient>();
		if (wsClient != null && wsClient.myPlayer != null)
		{
			player = wsClient.myPlayer.transform;
		}

		GenerateMap2D(map, map2DRect_P1, out objMapExist_P1);  // P1用
		GenerateMap2D(map, map2DRect_P2, out objMapExist_P2);  // P2用
	}
	#endregion

	/// <summary>
	/// リソース読み込み
	/// </summary>
	void ReadResources()
	{
		// 壁のマテリアル
		material = Resources.Load<Material>("Prefabs/Ryuki/Wall");

		// ゴール
		objGoal = (GameObject)Resources.Load("Prefabs/Ryuki/Goal");

		// 敵リスト
		objEnemyList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Enemy");

		// 強化敵リスト
		objSuperEnemyList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/SuperEnemy");

		// アイテムリスト
		objItemList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Item");

		// スイッチ
		objSwitchList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Switch");

		// 巡回ポイント
		objPatrolPointList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/PatrolPoint");

		// 2Dのマップチップ読み込み
		objMapTipList[0] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_0");
		objMapTipList[1] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_1");
		objMapTipList[2] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_2");
		objMapTipList[3] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_3");
	}

	/// <summary>
	/// 壁生成
	/// </summary>
	void GenerateWall()
	{
		// ダンジョンマップ
		mapGenerate = GetComponent<FixedMap>();
		// 2Dマップ生成
		map = mapGenerate.Generate();

		//CreateRoomIdMap();

		// 生成する壁の親となるGameObject
		GameObject objWall = GameObject.Find("Wall");

		int width = map.GetLength(0);
		int height = map.GetLength(1);

		//自動生成したマップにCubeを配置
		for (int y = 0; y < height; y++)
		{
			int startX = -1;

			for (int x = 0; x < width; x++)
			{
				if (map[x, y] == 0)
				{
					if (startX == -1) startX = x;
				}
				else
				{
					if (startX != -1)
					{
						CreateWallBlock(startX, x - 1, y, objWall);
						startX = -1;
					}
				}
			}
			if (startX != -1)
			{
				CreateWallBlock(startX, width - 1, y, objWall);
			}
		}
	}

	void CreateWallBlock(int startX, int endX, int y, GameObject parent)
	{
		int length = endX - startX + 1;

		GameObject cube = Instantiate(wallPrefab);

		cube.transform.parent = parent.transform;
		cube.transform.localScale = new Vector3(length, 4f, 1);

		float centerX = startX + (length / 2f) - 0.5f;
		cube.transform.position = new Vector3(centerX, 2f, y);

		var col = cube.GetComponent<Collider>();
		if (col != null) col.enabled = false;
	}

	/// <summary>
	/// 二次元マップ生成
	/// </summary>
	/// <param name="map"></param>
	void GenerateMap2D(int[,] map, RectTransform targetRect, out GameObject[,] mapExist)
	{
		mapExist = new GameObject[map.GetLength(0), map.GetLength(1)];

		objMap2D = GameObject.Find("Map2D").gameObject;
		objMapExist = new GameObject[map.GetLength(0), map.GetLength(1)];

		// 位置検索
		for (int x = 0; x < map.GetLength(0); x++)
		{
			for (int y = 0; y < map.GetLength(1); y++)
			{
				if (map[x, y] == 10) // プレイヤー位置検索
				{
					playerX = x;
					playerY = y;
				}
				if (map[x, y] == 3) // 敵位置検索
				{
					enemyX = x;
					enemyY = y;
				}
			}
		}

		// map[x,y]のパラメタ：0:壁、1:部屋、2:通路、10:プレイヤーが居る部屋
		for (int i = 0; i < map.GetLength(0); i++)
		{
			for (int j = 0; j < map.GetLength(1); j++)
			{
				int index = 0;

				if (map[i, j] == 0) index = 0;      // 壁
				else if (map[i, j] == 1) index = 1; // 部屋
				else if (map[i, j] == 2) index = 2; // 通路
				else if (map[i, j] == 3) index = 3; // 敵
													//else if (map[i, j] == 10) index = 3; // スタート
				objMapExist[i, j] = Instantiate(objMapTipList[index]);

				// Map2D直下に階層を移動
				objMapExist[i, j].transform.SetParent(objMap2D.transform, false);
				objMapExist[i, j].transform.localScale = new Vector3(1, 1, 1);

				// マップの位置調整
				//Vector2 vector2 = new Vector2(5f * i, 5f * j);
				/*if (objPlayer.tag == "Player1")
				{
					// 最初に入ったプレイヤー
					Vector2 vector2 = new Vector2(cellSize * i - mapX, cellSize * j - mapY);
					objMapExist[i, j].GetComponent<RectTransform>().anchoredPosition = vector2;
				}
				else
				{
					// 後から入ったプレイヤー
					Vector2 vector2 = new Vector2(cellSize * i - mapX - 100, cellSize * j - mapY);
					objMapExist[i, j].GetComponent<RectTransform>().anchoredPosition = vector2;
				}*/

				Vector2 vector2 = new Vector2(cellSize * i - mapX, cellSize * j - mapY);
				objMapExist[i, j].GetComponent<RectTransform>().anchoredPosition = vector2;


				// 初期状態では非表示
				//Color color = objMapExist[i, j].GetComponent<Image>().color;
				//color.a = 0;
				//objMapExist[i, j].GetComponent<Image>().color = color;


				// 2Dマップ生成のデバッグ用
				if ((map[i, j] == 1)) // 部屋
				{
					objMapExist[i, j].GetComponent<Image>().color = new Color(0, 0.8f, 1, 0.5f); // 部屋の色
				}
				else if ((map[i, j] == 2)) // 通路
				{
					objMapExist[i, j].GetComponent<Image>().color = new Color(0, 1, 0, 0.5f);
				}
				else if ((map[i, j] == 3)) // 敵
				{
					objMapExist[i, j].GetComponent<Image>().color = new Color(1, 0, 0, 0.5f);
				}
				else if ((map[i, j] == 10)) // スタート位置
				{
					objMapExist[i, j].GetComponent<Image>().color = new Color(1, 1, 0, 1);
				}
			}
		}
		CenterMiniMap(playerX, playerY);

		currentPlayerX = playerX;
		currentPlayerY = playerY;

		oldPlayerX = playerX;
		oldPlayerY = playerY;

	}

	void CenterMiniMap(int mapX, int mapY)
	{
		// Mask中央
		float centerX = miniMapMaskRect.rect.width * 0.5f;
		float centerY = miniMapMaskRect.rect.height * 0.5f;

		// プレイヤーマスの中心座標
		float playerCellX = (mapX * cellSize) + (cellSize * 0.5f);
		float playerCellY = (mapY * cellSize) + (cellSize * 0.5f);

		// プレイヤーマス中心をMask中央へ
		float posX = centerX - playerCellX;
		float posY = centerY - playerCellY;

		map2DRect.anchoredPosition = new Vector2(posX, posY);
	}

	/// <summary>
	/// ただしAwake時点ではmyPlayerがまだ生成されていない（init受信後に生成される）ので、コールバックで後から渡す方式にします。
	/// </summary>
	/// <param name="t"></param>
	public void SetPlayerTransform(Transform t)
	{
		player = t;
		// ミニマップの初期中心も更新
		currentPlayerX = Mathf.RoundToInt(t.position.x);
		currentPlayerY = Mathf.RoundToInt(t.position.z);
		oldPlayerX = currentPlayerX;
		oldPlayerY = currentPlayerY;
		CenterMiniMap(currentPlayerX, currentPlayerY);
	}

	public void SetRemotePlayerTransform(Transform t)
	{
		remotePlayer = t;
	}

	void UpdatePlayerOnMap(
	Transform target,
	ref int curX, ref int curY,
	ref int oldX, ref int oldY,
	GameObject[,] mapExist,
	RectTransform targetRect,
	RectTransform maskRect)
	{
		if (mapExist == null || target == null) return;

		curX = Mathf.RoundToInt(target.position.x);
		curY = Mathf.RoundToInt(target.position.z);

		if (oldX != curX || oldY != curY)
		{
			// 前のマスを地形色に戻す
			if (IsInsideMap(oldX, oldY, mapExist))
			{
				Image oldImg = mapExist[oldX, oldY].GetComponent<Image>();
				if (map[oldX, oldY] == 1) oldImg.color = new Color(0, 0.8f, 1, 0.5f);
				else if (map[oldX, oldY] == 2) oldImg.color = new Color(0, 1, 0, 0.5f);
				else oldImg.color = new Color(0, 0.8f, 1, 0.5f);
			}

			// 現在のマスをプレイヤー色に
			if (IsInsideMap(curX, curY, mapExist))
			{
				mapExist[curX, curY].GetComponent<Image>().color = new Color(1, 1, 0, 1);
			}

			oldX = curX;
			oldY = curY;
		}

		// ミニマップを中心に移動
		float centerX = maskRect.rect.width * 0.5f;
		float centerY = maskRect.rect.height * 0.5f;
		float playerCellX = (curX * cellSize) + (cellSize * 0.5f);
		float playerCellY = (curY * cellSize) + (cellSize * 0.5f);
		targetRect.anchoredPosition = new Vector2(centerX - playerCellX, centerY - playerCellY);
	}
	#region Update処理
	void Update()
	{
		// ミニマップに反映
		UpdateMiniMap();
	}

	/// <summary>
	/// プレイヤーと敵の現在位置をミニマップに反映
	/// </summary>
	void UpdateMiniMap()
	{
		///////////////////////
		// プレイヤー
		///////////////////////

		Vector3 playerPos = player.position;

		// ワールド座標 → マップ座標変換
		currentPlayerX = Mathf.RoundToInt(playerPos.x);
		currentPlayerY = Mathf.RoundToInt(playerPos.z);


		// 前回位置の色を戻す
		if (oldPlayerX != currentPlayerX || oldPlayerY != currentPlayerY)
		{
			if (IsInsideMap(oldPlayerX, oldPlayerY, objMapExist))
			{
				Image oldImage = objMapExist[oldPlayerX, oldPlayerY].GetComponent<Image>();

				// 元の地形色に戻す
				if (map[oldPlayerX, oldPlayerY] == 1)
				{
					oldImage.color = new Color(0, 0.8f, 1, 0.5f); // 部屋の色
				}
				else if (map[oldPlayerX, oldPlayerY] == 2)
				{
					oldImage.color = new Color(0, 1, 0, 0.5f);
				}
				else if (map[oldPlayerX, oldPlayerY] == 3)
				{
					oldImage.color = new Color(0, 0.8f, 1, 0.5f);
				}
				else if (map[oldPlayerX, oldPlayerY] == 10)
				{
					oldImage.color = new Color(0, 0.8f, 1, 0.5f);
				}
			}


			// 現在位置をプレイヤー色に
			if (IsInsideMap(currentPlayerX, currentPlayerY, objMapExist))
			{
				print("プレイヤー色変える");

				Image currentImage = objMapExist[currentPlayerX, currentPlayerY].GetComponent<Image>();

				// プレイヤー位置
				currentImage.color = new Color(1, 1, 0, 1);
			}
			oldPlayerX = currentPlayerX;
			oldPlayerY = currentPlayerY;

		}

		// マップを逆方向へ動かす
		CenterMiniMap(currentPlayerX, currentPlayerY);

		// P1（自分）
		if (player != null)
			UpdatePlayerOnMap(player, ref currentPlayerX, ref currentPlayerY,
							  ref oldPlayerX, ref oldPlayerY,
							  objMapExist_P1, map2DRect_P1, miniMapMaskRect_P1);

		// P2（相手）
		if (remotePlayer != null)
			UpdatePlayerOnMap(remotePlayer, ref currentRemoteX, ref currentRemoteY,
							  ref oldRemoteX, ref oldRemoteY,
							  objMapExist_P2, map2DRect_P2, miniMapMaskRect_P2);

		///////////////////////
		// 敵
		///////////////////////

		// 前回の敵位置を元に戻す
		foreach (Vector2Int pos in oldEnemyPositions)
		{
			if (IsInsideMap(pos.x, pos.y, objMapExist))
			{
				Image img = objMapExist[pos.x, pos.y].GetComponent<Image>();

				if (map[pos.x, pos.y] == 1)
				{
					img.color = new Color(0, 0.8f, 1, 0.5f);
				}
				else if (map[pos.x, pos.y] == 2)
				{
					img.color = new Color(0, 1, 0, 0.5f);
				}
			}
		}
		// 今回の敵位置保存用
		oldEnemyPositions.Clear();

		// 全敵更新
		foreach (GameObject enemyObj in objEnemys)
		{
			Vector3 enemyPos = enemyObj.transform.position;

			int enemyX = Mathf.RoundToInt(enemyPos.x);

			int enemyY = Mathf.RoundToInt(enemyPos.z);

			if (IsInsideMap(enemyX, enemyY, objMapExist))
			{
				Image img = objMapExist[enemyX, enemyY].GetComponent<Image>();

				// 敵色
				img.color = new Color(1, 0, 0, 0.5f);

				// 保存
				oldEnemyPositions.Add(new Vector2Int(enemyX, enemyY));
			}
		}

		for (int i = 0; i < objEnemys.Length; i++)
		{
			GameObject enemyObj = objEnemys[i];

			Vector3 enemyPos = enemyObj.transform.position;

			int enemyX = Mathf.RoundToInt(enemyPos.x);

			int enemyY = Mathf.RoundToInt(enemyPos.z);

			// UI位置
			RectTransform rt = viewList[i].GetComponent<RectTransform>();

			rt.anchoredPosition = new Vector2(enemyX * cellSize - mapX, enemyY * cellSize - mapY);

			// 向き
			float angle = enemyObj.transform.eulerAngles.y;

			rt.rotation = Quaternion.Euler(0, 0, -angle);
		}
	}
	#endregion

	/// <summary>
	/// 座標がマップ範囲内かどうか確認
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	bool IsInsideMap(int x, int y, GameObject[,] mapExist)
	{
		return x >= 0 &&
			   y >= 0 &&
			   x < mapExist.GetLength(0) &&
			   y < mapExist.GetLength(1);    
	}



	/// <summary>
	/// CSVに敵やアイテムなども調整できるように
	/// </summary>
	void GenerateObjectsCSV()
	{
		for (int x = 0; x < map.GetLength(0); x++)
		{
			for (int y = 0; y < map.GetLength(1); y++)
			{
				Vector3 pos = new Vector3(x, 0, y);

				switch (map[x, y])
				{
					case 3: // 敵
						Instantiate(objEnemyList[0], pos, Quaternion.identity);
						GameObject view = Instantiate(enemyViewPrefab, map2DRect);
						viewList.Add(view);
						objEnemys = GameObject.FindGameObjectsWithTag("Enemy");
						map[x, y] = 1; // 床に戻す
						break;

					case 4: // 強化敵
						Instantiate(objSuperEnemyList[0], pos, Quaternion.identity);
						map[x, y] = 1;
						break;

					case 5: // アイテム
						Instantiate(objItemList[0], pos, Quaternion.identity);
						map[x, y] = 1;
						break;

					case 6: // ゴール
						Instantiate(objGoal, pos, Quaternion.identity);
						map[x, y] = 1;
						break;
					case 7: // スイッチ
						Instantiate(objSwitchList[0], pos, Quaternion.identity);
						map[x, y] = 1;
						break;

					/*case 8: // プレイヤー
						objPlayer = GameObject.Find("Player");
						objPlayer.transform.position = pos;
						map[x, y] = 1; // プレイヤー位置マップ表示
						break;*/
					case 8: // プレイヤー1
						var wsClient = FindObjectOfType<WebSocketClient>();
						if (wsClient != null)
							wsClient.SetSpawnPosition(1, pos);
						map[x, y] = 1;
						break;
					case 9: // 敵の巡回ポイント
						Instantiate(objPatrolPointList[0], pos, Quaternion.identity);
						map[x, y] = 1;
						break;
					case 10: // プレイヤー2
						var wsClient2 = FindObjectOfType<WebSocketClient>();
						if (wsClient2 != null)
							wsClient2.SetSpawnPosition(2, pos);
						map[x, y] = 1;
						break;
				}
			}
		}
	}
}