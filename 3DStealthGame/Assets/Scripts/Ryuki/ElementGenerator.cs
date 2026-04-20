using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ElementGenerator : MonoBehaviour
{
    #region 宣言
    //リソース格納用
    Material material;                                              //壁のマテリアル
    GameObject objGoal;                                             //ゴール
    GameObject[] objEnemyList = new GameObject[1];                  //敵リスト
    GameObject[] objSuperEnemyList = new GameObject[1];             //強化敵リスト
    public GameObject[] objItemList = new GameObject[1];            //アイテムリスト
    GameObject[] objMapTipList = new GameObject[4];                 //マップチップリスト

    // 調整用パラメータ
    [SerializeField] int LimitEnemyInitMin;                         //初期生成の敵の下限数
    [SerializeField] int LimitEnemyInitMax;                         //初期生成の敵の上限数
    [SerializeField] int LimitSuperEnemyInitMin;                    //初期生成の強化敵の下限数
    [SerializeField] int LimitSuperEnemyInitMax;                    //初期生成の強化敵の上限数
    [SerializeField] int LimitItemInitMin;                          //初期生成のアイテムの下限数
    [SerializeField] int LimitItemInitMax;                          //初期生成のアイテムの上限数

    // マップ軽くするためのプレファブ
    [SerializeField] GameObject wallPrefab;

    //2Dマップ生成スクリプト用
    MapGenerate mapGenerate;
    int[,] map;

    // ランダム生成用
    int[,] roomIdMap;                                               //ルームID                        
    HashSet<int> usedRooms = new HashSet<int>();

    //パス読み込み用
    GameObject objMap2D;                                            //Map2D
    GameObject objPlayer;                                           //プレイヤー

    //生成したマップチップ
    GameObject[,] objMapExist;                                      //フィールド用
    #endregion

    #region 初期化処理
    void Awake()
    {
        //リソース読み込み
        ReadResources();

        //壁を生成
        GenerateWall();

        //二次元マップ生成
        GenerateMap2D(map);
    }

    void Start()
    {
        // 部屋IDリセット
        usedRooms.Clear();

        //プレイヤーを部屋に配置(予め生成しておく)
        objPlayer = GameObject.Find("Player");
        GenerateObj(map, objPlayer);

        //ゴールを部屋に配置
        GenerateObj(map, objGoal);

        //敵を部屋に配置(初期出現数はランダム)
        int enemyNum = Random.Range(LimitEnemyInitMin, LimitEnemyInitMax + 1);
        for (int i = 0; i < enemyNum; i++)
        {
            int enemyIdx = Random.Range(0, objEnemyList.Length);
            GenerateObj(map, objEnemyList[enemyIdx]);
        }
        
        //アイテムを部屋に配置(初期出現数はランダム)
        int itemNum = Random.Range(LimitItemInitMin, LimitItemInitMax + 1);
        for (int i = 0; i < itemNum; i++)
        {
            int itemIdx = Random.Range(0, objItemList.Length);
            GenerateObj(map, objItemList[itemIdx]);
        }
        //敵を部屋に配置(初期出現数はランダム)
        int enemySuperNum = Random.Range(LimitSuperEnemyInitMin, LimitSuperEnemyInitMax + 1);
        for (int i = 0; i < enemySuperNum; i++)
        {
            int enemyIdx = Random.Range(0, objSuperEnemyList.Length);
            GenerateObj(map, objSuperEnemyList[enemyIdx]);
        }
    }
    #endregion

    /// <summary>
    /// リソース読み込み
    /// </summary>
    void ReadResources()
    {
        //壁のマテリアル
        material = Resources.Load<Material>("Prefabs/Ryuki/Wall");

        //ゴール
        objGoal = (GameObject)Resources.Load("Prefabs/Ryuki/Goal");
        
        //敵リスト
        objEnemyList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Enemy");

        //強化敵リスト
        objSuperEnemyList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/SuperEnemy");

        //アイテムリスト
        objItemList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Item");

        //2Dのマップチップ読み込み
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
        //ダンジョンマップ
        mapGenerate = GetComponent<MapGenerate>();

        //壁の高さ
        float blockHeight = 4f;

        //2Dマップ生成
        map = mapGenerate.Generate();

        CreateRoomIdMap();

        //生成する壁の親となるGameObject
        GameObject objWall = GameObject.Find("Wall");

        //自動生成したマップにCubeを配置
        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                //壁をCubeで作成
                //if (map[i, j] == 0)
                //{
                //    for (int k = 0; k < blockHeight; k++)
                //    {
                //        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                //        //Wall直下に階層を移動
                //        cube.transform.parent = objWall.transform;
                //        cube.GetComponent<Renderer>().material = material;
                //        cube.transform.localScale = new Vector3(1, 1, 1);
                //        cube.transform.position = new Vector3(i, k + 0.5f, j);
                //    }
                //}
                if (map[i, j] == 0)
                {
                    //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    GameObject cube = Instantiate(wallPrefab);
                    Destroy(cube.GetComponent<Collider>());

                    cube.transform.parent = objWall.transform;
                    cube.GetComponent<Renderer>().material = material;

                    cube.transform.localScale = new Vector3(1, blockHeight, 1);
                    cube.transform.position = new Vector3(i, blockHeight / 2f, j);
                }
            }
        }
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    /// <summary>
    /// 部屋ごとにIDを割り振る
    /// </summary>
    void CreateRoomIdMap()
    {
        int currentId = 0;
        roomIdMap = new int[map.GetLength(0), map.GetLength(1)];

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y] == 1 && roomIdMap[x, y] == 0)
                {
                    currentId++;
                    MapSort(x, y, currentId);
                }
            }
        }
    }

    /// <summary>
    /// 二次元マップ生成
    /// </summary>
    /// <param name="map"></param>
    void GenerateMap2D(int[,] map)
    {
        objMap2D = GameObject.Find("Map2D").gameObject;
        objMapExist = new GameObject[map.GetLength(0), map.GetLength(1)];

        //map[x,y]のパラメタ：0:壁、1:部屋、2:通路、10:プレイヤーが居る部屋
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                int index = 0;

                if (map[i, j] == 0) index = 0;      // 壁
                else if (map[i, j] == 1) index = 1; // 部屋
                else if (map[i, j] == 2) index = 2; // 通路
                else if (map[i, j] == 10) index = 3; // スタート

                objMapExist[i, j] = Instantiate(objMapTipList[index]);

                //Map2D直下に階層を移動
                //objMapExist[i, j].transform.parent = objMap2D.transform;
                objMapExist[i, j].transform.SetParent(objMap2D.transform, false);
                objMapExist[i, j].transform.localScale = new Vector3(1, 1, 1);

                //マップの位置調整
                Vector2 vector2 = new Vector2(2.5f + 5f * i, 2.5f + 5f * j);
                objMapExist[i, j].GetComponent<RectTransform>().anchoredPosition = vector2;

                //初期状態では非表示
                Color color = objMapExist[i, j].GetComponent<Image>().color;
                color.a = 0;
                objMapExist[i, j].GetComponent<Image>().color = color;


                if ((map[i, j] == 1) || (map[i, j] == 2))
                {
                    //2Dマップ生成のデバッグ用
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
    /// 幅優先探索を使った振り分け処理
    /// </summary>
    void MapSort(int startX, int startY, int id)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(startX, startY));
        roomIdMap[startX, startY] = id;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (q.Count > 0)
        {
            var p = q.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i];
                int ny = p.y + dy[i];

                if (nx >= 0 && ny >= 0 &&
                    nx < map.GetLength(0) && ny < map.GetLength(1))
                {
                    if (map[nx, ny] == 1 && roomIdMap[nx, ny] == 0)
                    {
                        roomIdMap[nx, ny] = id;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }
    }

    /// <summary>
    /// オブジェクト生成 (プレイヤー以外)
    /// </summary>
    /// <param name="map"></param>
    /// <param name="obj"></param>
    void GenerateObj(int[,] map, GameObject obj)
    {
        while (true)
        {
            int mapX = Random.Range(0, map.GetLength(0) - 1);
            int mapY = Random.Range(0, map.GetLength(1) - 1);

            if (map[mapX, mapY] == 1)
            {
                int roomId = roomIdMap[mapX, mapY];

                if (usedRooms.Contains(roomId))
                    continue;

                usedRooms.Add(roomId);

                Instantiate(obj, new Vector3(mapX, 0, mapY), Quaternion.identity);
                break;

                ////プレイヤーは生成済みのため移動だけ
                //if (obj.CompareTag("Player") == true)
                //{
                //    obj.transform.position = new Vector3(mapX, 0, mapY);
                //}
                ////その他は生成と移動
                //else
                //{
                //    GameObject objInstant = Instantiate(obj, new Vector3(mapX, 0, mapY), Quaternion.Euler(0f, 0f, 0f));
                //}
                //break;
            }
        }
    }
}