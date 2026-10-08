using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using static UnityEditor.Experimental.GraphView.GraphView;
#endif

public class ElementGenerator : MonoBehaviour
{
    #region 定数

    readonly Color ROOM_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.5f);  // 部屋の色
    readonly Color AISLE_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 通路の色
    readonly Color PLAYER1_COLOR = new Color(0, 0.5f, 1, 1);        // 青（ホスト）
    readonly Color PLAYER2_COLOR = new Color(0, 1, 0.3f, 1);        // 緑（ゲスト）
    readonly Color ENEMY_COLOR = new Color(1, 0, 0, 0.5f);          // 敵の色
    readonly Color WALLBLUE_COLOR = new Color(0, 0.5f, 1, 1);       // 青用壁
    readonly Color WALLGREEN_COLOR = new Color(0, 1, 0.3f, 1);      // 緑用壁

    #endregion

    #region リソース格納用

    GameObject goalObjects;                                         // ゴール
    GameObject[] enemiesList = new GameObject[1];                   // 敵リスト
    GameObject[] strongEnemisList = new GameObject[1];              // 強化敵リスト
    GameObject[] itemsList = new GameObject[1];                     // アイテムリスト
    GameObject[] switchesList = new GameObject[1];                  // スイッチリスト
    GameObject[] patrolPointsList = new GameObject[1];              // 巡回ポイントリスト
    GameObject[] mapTilesList = new GameObject[4];                  // マップタイルリスト
    GameObject[] respawnPointsList = new GameObject[1];             // リスポーン地点リスト
    GameObject wallObjects;                                         // マップ作成用キューブ

    // 仮作成用プレイヤースイッチギミックに必要なアイテム
    public GameObject[] powerItemBlue = new GameObject[1];                 // 青用アイテム               
    public GameObject[] powerItemGreen = new GameObject[1];                // 緑用アイテム

    Sprite goalIcon;                                                // ゴールアイコン
    Sprite switchOFFIcon;                                           // スイッチアイコン
    Sprite switchONIcon;                                            // スイッチアイコン
    Sprite itemIcon;                                                // アイテムアイコン
    Sprite greenItemIcon;                                           // 緑用アイテムアイコン
    Sprite blueItemIcon;                                            // 青用アイテムアイコン
    Sprite wallBlueIcon;                                            // 青用壁アイコン
    Sprite wallGreenIcon;                                           // 青用壁アイコン

    #endregion

    #region マップ生成管理

    CsvMapLoader mapGenerate;                                       // ミニマップ生成スクリプト
    string[,] map;                                                  // マップ読み込み
    GameObject objMap2D;                                            // Map2D
    GameObject[,] objMapExist;                                      // 生成したマップチップ

    // CSVデータ数値パラメータ
    public enum MapObjectType
    {
        Enemy = 3,
        StrongEnemy = 4,
        Item = 5,
        Goal = 6,
        Switch = 7,
        Respawn = 8,
        PatrolPoint = 9,
        Player1 = 10,
        Player2 = 11,
        GimickWall = 12,
        powerItemBlue = 13,
        powerItemGreen = 14,
        switchBlue = 15,
        switchGreen = 16,
        GimickWallBlue = 17, 
        GimickWallGreen = 18

    }
    // メンバ変数として追加
    public Dictionary<int, List<GameObject>> gimmickWallDic = new Dictionary<int, List<GameObject>>();
    public Dictionary<int, List<Vector2Int>> gimmickWallPosDic = new Dictionary<int, List<Vector2Int>>();
    [SerializeField] private Material transparentRedMaterial;   // 半透明な壁マテリアル
    [SerializeField] private Material transparentBlueMaterial;  // 半透明な壁マテリアル
    [SerializeField] private Material transparentGreenMaterial; // 半透明な壁マテリアル
    #endregion

    #region プレイヤー位置管理

    int playerX = 0;                                                // プレイヤーX座標
    int playerY = 0;                                                // プレイヤーY座標
    int currentPlayerX;                                             // 現在プレイヤーがいるXマス
    int currentPlayerY;                                             // 現在プレイヤーがいるYマス
    int oldPlayerX;                                                 // 前回プレイヤーがいたXマス
    int oldPlayerY;                                                 // 前回プレイヤーがいたYマス

    #endregion

    #region ミニマップUI管理

    [SerializeField] RectTransform miniMapMaskRect;
    [SerializeField] RectTransform map2DRect;
    int mapX = 45;                                                 // ミニマップ位置座標X
    int mapY = 145;                                                // ミニマップ位置座標Y
    float cellSize = 5f;

    #endregion

    #region 敵視野UI管理

    [Header("敵の視野UI")]
    [SerializeField] GameObject enemyViewPrefab;
    [SerializeField] GameObject enemyStrongViewPrefab;
    [SerializeField] GameObject itemViewPrefab;

    List<GameObject> viewList = new List<GameObject>();
    List<GameObject> viewStrongList = new List<GameObject>();

    #endregion

    #region 澤田作:マルチプレイヤー管理/プレイヤー別ミニマップ管理

    /// <summary>
    /// マルチプレイヤー管理
    /// </summary>
    [Header("Player サーバ関連"), SerializeField]
    Transform player;
    Transform remotePlayer;   // 相手のTransform
    int currentRemoteX, currentRemoteY;
    int oldRemoteX, oldRemoteY;

    /// <summary>
    /// プレイヤー別ミニマップ管理
    /// </summary>
    [Header("プレイヤー1のミニマップ")]
    [SerializeField] RectTransform map2DRect_P1;
    [SerializeField] RectTransform miniMapMaskRect_P1;

    [Header("プレイヤー2のミニマップ")]
    [SerializeField] RectTransform map2DRect_P2;
    [SerializeField] RectTransform miniMapMaskRect_P2;

    GameObject[,] objMapExist_P1;
    GameObject[,] objMapExist_P2;

    #endregion

    #region シーン内オブジェクト管理

    GameObject[] objEnemys;                                               // 敵
    GameObject[] objEnemyStrongs;                                         // 強化敵
    List<GameObject> activeItems = new List<GameObject>();                // アイテム
    List<GameObject> activeGoals = new List<GameObject>();                // ゴール
    List<GameObject> activeSwitches = new List<GameObject>();             // スイッチ
    public List<GameObject> activeGreenItems = new List<GameObject>();    // 緑用アイテム
    public List<GameObject> activeBlueItems = new List<GameObject>();     // 青用アイテム
    public List<GameObject> activeGreenWalls = new List<GameObject>();    // 緑壁
    public List<GameObject> activeBlueWalls = new List<GameObject>();     // 青壁

    #endregion

    #region ミニマップ更新管理

    List<Vector2Int> oldEnemyPositions = new List<Vector2Int>();                                         // 敵のマス                                                                     
    Dictionary<GameObject, Vector2Int> oldGreenItemPositions = new Dictionary<GameObject, Vector2Int>(); // 緑アイテムのマス
    Dictionary<GameObject, Vector2Int> oldBlueItemPositions = new Dictionary<GameObject, Vector2Int>(); // 緑アイテムのマス
    #endregion

    #region デバック設定

    [Header("壁のコライダーを有効化")]
    [SerializeField] bool enableWallCollider = true;                // Debug用

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

        // WebSocketClientからプレイヤーを取得
        var wsClient = FindObjectOfType<WebSocketClient>();
        if (wsClient != null && wsClient.myPlayer != null)
        {
            player = wsClient.myPlayer.transform;
        }

        //GenerateMap2D(map, map2DRect_P1, out objMapExist_P1);  // P1用
        //GenerateMap2D(map, map2DRect_P2, out objMapExist_P2);  // P2用
    }

    /// <summary>
    /// リソース読み込み
    /// </summary>
    void ReadResources()
    {
        // マップ作成用キューブ
        wallObjects = (GameObject)Resources.Load("Prefabs/Ryuki/WallPrefab");

        // ゴール
        goalObjects = (GameObject)Resources.Load("Prefabs/Ryuki/Goal");
        goalIcon = Resources.Load<Sprite>("Images/Ryuki/IconGoal");

        // 敵リスト
        enemiesList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Enemy");

        // 強化敵リスト
        strongEnemisList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/SuperEnemy");

        // アイテムリスト
        itemsList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Item");
        itemIcon = Resources.Load<Sprite>("Images/Ryuki/IconItem");
        greenItemIcon = Resources.Load<Sprite>("Images/Ryuki/GreenItem");
        blueItemIcon = Resources.Load<Sprite>("Images/Ryuki/BlueItem");
        wallBlueIcon = Resources.Load<Sprite>("Images/Ryuki/青壁");
        wallGreenIcon = Resources.Load<Sprite>("Images/Ryuki/緑壁");
        
        // スイッチ
        switchesList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Switch");
        switchOFFIcon = Resources.Load<Sprite>("Images/Ryuki/SwitchOFF");
        switchONIcon = Resources.Load<Sprite>("Images/Ryuki/SwitchON");

        // 巡回ポイント
        patrolPointsList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/PatrolPoint");

        // 2Dのマップチップ読み込み
        mapTilesList[0] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_0");
        mapTilesList[1] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_1");
        mapTilesList[2] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_2");
        mapTilesList[3] = Resources.Load<GameObject>("Prefabs/Ryuki/Map2D/MapUI_3");

        // リスポーン地点
        respawnPointsList[0] = (GameObject)Resources.Load("Prefabs/Ryuki/Respawn");

        // 仮作成
        powerItemBlue[0] = Resources.Load<GameObject>("Prefabs/Masataka/Thunder_Blue");
        powerItemGreen[0] = Resources.Load<GameObject>("Prefabs/Masataka/Thunder_Green");
    }

    /// <summary>
    /// 壁生成
    /// </summary>
    void GenerateWall()
    {
        // CSVマップデータ取得
        mapGenerate = GetComponent<CsvMapLoader>();
        // 2Dマップ読み込み
        map = mapGenerate.Generate();

        // 生成する壁の親となるGameObject
        GameObject objWall = GameObject.Find("Wall");

        // マップサイズ取得
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // 古いデータが残らないように初期化
        gimmickWallDic.Clear();

        // CSVデータを横一列で見る
        for (int y = 0; y < height; y++)
        {
            int startX = -1; // 開始位置
            string currentWallType = "";
            int currentWallID = -1;

            for (int x = 0; x < width; x++)
            {
                string cell = map[x, y];

                // 空白セルの場合はスキップ
                if (string.IsNullOrEmpty(cell))
                {
                    // 壁が途切れたら生成
                    if (startX != -1)
                    {
                        CreateWallBlock(startX, x - 1, y, objWall, currentWallType, currentWallID);
                        startX = -1;
                        currentWallType = "";
                        currentWallID = -1;
                    }
                    continue;
                }
                // セルのデータを分割
                string[] data = cell.Split('_');
                string type = data[0];
                int id = data.Length > 1 ? int.Parse(data[1]) : -1;

                bool isWall = (type == "0" || type == "12" || type == "17" || type == "18");

                if (isWall)
                {
                    if (startX == -1)
                    {
                        startX = x;
                        currentWallType = type;
                        currentWallID = id;
                    }
                    // 壁の種類かIDが変わったら、それまでの壁を生成して区切る
                    else if (currentWallType != type || currentWallID != id)
                    {
                        CreateWallBlock(startX, x - 1, y, objWall, currentWallType, currentWallID);
                        startX = x;
                        currentWallType = type;
                        currentWallID = id;
                    }
                }
                else // 壁以外のマス（敵やアイテムなど）
                {
                    if (startX != -1)
                    {
                        CreateWallBlock(startX, x - 1, y, objWall, currentWallType, currentWallID);
                        startX = -1;
                        currentWallType = "";
                        currentWallID = -1;
                    }
                }
            }
            if (startX != -1)
            {
                CreateWallBlock(startX, width - 1, y, objWall, currentWallType, currentWallID);
            }
        }
    }

    /// <summary>
    /// 連立するキューブを一つのオブジェクトとして生成させる処理
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="endX"></param>
    /// <param name="y"></param>
    /// <param name="parent"></param>
    void CreateWallBlock(int startX, int endX, int y, GameObject parent, string wallType, int wallID)
    {
        // 青壁・緑壁だけ1マスずつ生成
        if (wallType == "17" || wallType == "18")
        {
            for (int x = startX; x <= endX; x++)
            {
                GameObject cube = Instantiate(wallObjects);

                cube.transform.parent = parent.transform;
                cube.transform.localScale = new Vector3(3f, 4f, 1);
                cube.transform.position = new Vector3(x, 2f, y);

                MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (wallType == "17")
                    {
                        renderer.material = transparentBlueMaterial;
                        activeBlueWalls.Add(cube);
                    }
                    else if (wallType == "18")
                    {
                        renderer.material = transparentGreenMaterial;
                        activeGreenWalls.Add(cube);

                    }
                }

                cube.AddComponent<WallCollision>();

                if (wallID != -1)
                {
                    if (!gimmickWallDic.ContainsKey(wallID))
                        gimmickWallDic[wallID] = new List<GameObject>();

                    if (!gimmickWallPosDic.ContainsKey(wallID))
                        gimmickWallPosDic[wallID] = new List<Vector2Int>();

                    gimmickWallDic[wallID].Add(cube);
                    gimmickWallPosDic[wallID].Add(new Vector2Int(x, y));
                }

                var col = cube.GetComponent<Collider>();
                if (col != null)
                    col.enabled = enableWallCollider;
            }

            return;
        }

        int length = endX - startX + 1;

        GameObject cube2 = Instantiate(wallObjects);

        cube2.transform.parent = parent.transform;
        cube2.transform.localScale = new Vector3(length, 4f, 1);

        float centerX = startX + (length / 2f) - 0.5f;
        cube2.transform.position = new Vector3(centerX, 2f, y);

        if (wallType == "12")
        {
            MeshRenderer renderer = cube2.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
               renderer.material = transparentRedMaterial;
            }
            // 透明壁の通知用スクリプト
            cube2.AddComponent<WallCollision>();

            // IDが設定されている場合のみ辞書に登録
            if (wallID != -1)
            {
                if (!gimmickWallDic.ContainsKey(wallID))
                {
                    gimmickWallDic[wallID] = new List<GameObject>();
                }

                if (!gimmickWallPosDic.ContainsKey(wallID))
                {
                    gimmickWallPosDic[wallID] = new List<Vector2Int>();
                }

                gimmickWallDic[wallID].Add(cube2);

                for (int x = startX; x <= endX; x++)
                {
                    gimmickWallPosDic[wallID].Add(new Vector2Int(x, y));
                }
            }
        }

        // デバック用：コライダーON/OFF切り替え
        var col2 = cube2.GetComponent<Collider>();
        if (col2 != null)
        {
            col2.enabled = enableWallCollider;
        }
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

                string cell = map[x, y];

                if (string.IsNullOrEmpty(cell))
                {
                    continue;
                }

                string[] data = cell.Split('_');

                //int type = int.Parse(data[0]);
                MapObjectType type = (MapObjectType)int.Parse(data[0]);
                int id = data.Length > 1 ? int.Parse(data[1]) : -1;

                switch (type)
                {
                    case MapObjectType.Enemy: // 敵
                        GameObject normalEnemyObj = Instantiate(enemiesList[0], pos, Quaternion.identity);
                        GameObject view = Instantiate(enemyViewPrefab, map2DRect);
                        viewList.Add(view);
                        objEnemys = GameObject.FindGameObjectsWithTag("Enemy");
                        EnemyManager normalEm = normalEnemyObj.GetComponent<EnemyManager>();
                        if (normalEm != null) normalEm.enemyID = id;
                        map[x, y] = "1";
                        break;		

					case MapObjectType.Player1: // プレイヤー1
                        var wsClient = FindObjectOfType<WebSocketClient>();
                        if (wsClient != null) wsClient.SetSpawnPosition(1, pos);
                        map[x, y] = "1";
                        break;

                    case MapObjectType.PatrolPoint: // 敵の巡回ポイント
                        Instantiate(patrolPointsList[0], pos, Quaternion.identity);
                        map[x, y] = "1";
                        break;

                    case MapObjectType.Player2: // プレイヤー2
                        var wsClient2 = FindObjectOfType<WebSocketClient>();
                        if (wsClient2 != null)
                            wsClient2.SetSpawnPosition(2, pos);
                        map[x, y] = "1";
                        break;

                    case MapObjectType.Respawn: // リスポーン
                        Instantiate(respawnPointsList[0], pos, Quaternion.identity);
                        map[x, y] = "1";
                        break;				
				}
            }
        }
    }

    #region 澤田作：サーバ関連処理

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
    }

    public void SetRemotePlayerTransform(Transform t)
    {
        remotePlayer = t;
    }

    #endregion

    #endregion
}