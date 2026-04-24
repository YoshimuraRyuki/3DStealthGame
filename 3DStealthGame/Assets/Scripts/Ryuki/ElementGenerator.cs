using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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

    // 2Dマップ生成スクリプト用
    //MapGenerate mapGenerate;
    FixedMap mapGenerate;
    int[,] map;

    // パス読み込み用
    GameObject objMap2D;                                            // Map2D
    GameObject objPlayer;                                           // プレイヤー

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
        GenerateMap2D(map);
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
        GetComponent<NavMeshSurface>().BuildNavMesh();
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
    void GenerateMap2D(int[,] map)
    {
        objMap2D = GameObject.Find("Map2D").gameObject;
        objMapExist = new GameObject[map.GetLength(0), map.GetLength(1)];

        // map[x,y]のパラメタ：0:壁、1:部屋、2:通路、10:プレイヤーが居る部屋
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                int index = 0;

                if (map[i, j] == 0) index = 0;      // 壁
                else if (map[i, j] == 1) index = 1; // 部屋
                else if (map[i, j] == 2) index = 2; // 通路
                else if (map[i, j] == 10) index = 3; // スタート

                objMapExist[i, j] = Instantiate(objMapTipList[index]);

                // Map2D直下に階層を移動
                //objMapExist[i, j].transform.parent = objMap2D.transform;
                objMapExist[i, j].transform.SetParent(objMap2D.transform, false);
                objMapExist[i, j].transform.localScale = new Vector3(1, 1, 1);

                // マップの位置調整
                Vector2 vector2 = new Vector2(2.5f + 5f * i, 2.5f + 5f * j);
                objMapExist[i, j].GetComponent<RectTransform>().anchoredPosition = vector2;

                // 初期状態では非表示
                Color color = objMapExist[i, j].GetComponent<Image>().color;
                color.a = 0;
                objMapExist[i, j].GetComponent<Image>().color = color;


                if ((map[i, j] == 1) || (map[i, j] == 2))
                {
                    // 2Dマップ生成のデバッグ用
                    if ((map[i, j] == 1))
                    {
                        objMapExist[i, j].GetComponent<Image>().color = new Color(1, 0, 0, 0.5f);
                    }
                    else if ((map[i, j] == 2))
                    {
                        objMapExist[i, j].GetComponent<Image>().color = new Color(0, 1, 0, 0.5f);
                    }
                }

            }
        }
    }

    /// <summary>
    /// CSV1に敵やアイテムなども調整できるように
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
                        map[x, y] = 1; // 床に戻す
                        break;

                    case 4:
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

                    case 8: // プレイヤー
                        objPlayer = GameObject.Find("Player");
                        objPlayer.transform.position = pos;
                        map[x, y] = 10; // プレイヤー位置マップ表示
                        break;

                    case 9: // 敵の巡回ポイント
                        Instantiate(objPatrolPointList[0], pos, Quaternion.identity);
                        map[x, y] = 1;
                        break;
                }
            }
        }
    }
}