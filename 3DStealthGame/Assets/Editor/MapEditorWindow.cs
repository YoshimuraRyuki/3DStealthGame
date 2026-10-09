using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class MapEditorWindow : EditorWindow
{
    #region オブジェクト管理

    public enum MapObjectType
    {
        NormalWall = 0,      // 通常の壁
        Floor = 1,           // 床・空白
        Road = 2,            // 通路
        Enemy = 3,           // 敵
        Goal = 4,            // ゴール
        PatrolPoint = 5,     // 巡回ポイント
        Player1 = 6,         // プレイヤー1
        Player2 = 7,         // プレイヤー2
        Core = 8,            // コア
    }

    #endregion

    #region マップデータ

    int rows = 100;                                     // 縦100マス
    int cols = 50;                                      // 横50マス
    string[,] mapData;                                  // マップデータを保持する2次元配列
    string csvFilePath = "Assets/Resources/map.csv";   // CSVファイルの保存パス
    #endregion

    #region エディタUI関連

    MapObjectType selectedType = MapObjectType.Floor;  // 初期のマップタイル
    int selectedGimmickID = -1;                        // 現在選択されている配置オブジェクトのID
    Vector2 scrollPosition;                            // スクロール位置管理
    const float cellSize = 22f;                        // 正方形マスサイズ定義

    // スタイルのキャッシュ用
    private GUIStyle centerLabelStyleWhite;
    private GUIStyle centerLabelStyleBlack;
    private bool isDragging = false;

    #endregion

    [MenuItem("Tools/ステージ作成エディタ")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("ステージエディタ");
    }

    void OnEnable()
    {
        ResetMap();
    }

    void ResetMap()
    {
        mapData = new string[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                mapData[r, c] = "0"; // 最初は壁(0)で埋める
            }
        }
    }

    private void InitStyles()
    {
        if (centerLabelStyleWhite == null)
        {
            centerLabelStyleWhite = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            centerLabelStyleWhite.normal.textColor = Color.white;
        }

        if (centerLabelStyleBlack == null)
        {
            centerLabelStyleBlack = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            centerLabelStyleBlack.normal.textColor = Color.black;
        }
    }

    void OnGUI()
    {
        InitStyles();

        GUILayout.Label("ステージ生成", EditorStyles.boldLabel);

        // 設定エリア
        EditorGUILayout.BeginVertical("box");
        csvFilePath = EditorGUILayout.TextField("CSV保存パス", csvFilePath);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 選択中のオブジェクトに応じた実際のカラーを取得
        Color previewColor = GetColorForType(((int)selectedType).ToString());

        // パレット・ID設定エリア
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("配置するオブジェクト、IDを選択", EditorStyles.boldLabel);

        // --- オブジェクト選択 ＋ プレビュー表示エリア ---
        EditorGUILayout.BeginHorizontal();

        // オブジェクトタイプをドロップダウンで選択
        selectedType = (MapObjectType)EditorGUILayout.EnumPopup("配置するオブジェクト", selectedType);

        // 実際の配置色と同じプレビュー四角を描画
        Rect colorBoxRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUI.DrawRect(colorBoxRect, previewColor);
        Handles.DrawSolidRectangleWithOutline(colorBoxRect, Color.clear, Color.black); // 黒枠線

        EditorGUILayout.EndHorizontal();

        // ギミックIDの入力枠
        EditorGUILayout.BeginHorizontal();
        selectedGimmickID = EditorGUILayout.IntField("紐付けギミックID (-1で無し)", selectedGimmickID);
        if (GUILayout.Button("IDクリア (-1)", GUILayout.Width(100))) selectedGimmickID = -1;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // 操作ボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CSVから読み込み", GUILayout.Height(25))) { LoadFromCSV(); }
        if (GUILayout.Button("CSVへ保存", GUILayout.Height(25))) { SaveToCSV(); }
        if (GUILayout.Button("クリア", GUILayout.Height(25))) { if (EditorUtility.DisplayDialog("確認", "マップを初期化しますか？", "はい", "いいえ")) ResetMap(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // マップ編集グリッドエリア
        GUILayout.Label("マップグリッド(クリックまたはドラッグして配置)", EditorStyles.boldLabel);

        Event currentEvent = Event.current;

        // ドラッグ状態の管理
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            isDragging = true;
        }
        else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
        {
            isDragging = false;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width - 20), GUILayout.Height(position.height - 190));

        // グリッド全体の領域サイズを計算して確保
        float totalWidth = cols * cellSize;
        float totalHeight = rows * cellSize;
        Rect gridAreaRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

        // スクロール視界領域（カリング計算用）
        Rect viewRect = new Rect(scrollPosition.x, scrollPosition.y, position.width, position.height);

        // マウス位置からカーソル下の行・列を直接算出
        Vector2 mousePos = currentEvent.mousePosition;
        int hoverCol = Mathf.FloorToInt((mousePos.x - gridAreaRect.x) / cellSize);
        int hoverRow = Mathf.FloorToInt((mousePos.y - gridAreaRect.y) / cellSize);

        // ドラッグ中またはクリック時に配置を実行
        if (isDragging && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseMove))
        {
            if (hoverRow >= 0 && hoverRow < rows && hoverCol >= 0 && hoverCol < cols)
            {
                SetCellData(hoverRow, hoverCol);
            }
        }

        // --- 画面に見えている領域だけを描画（軽量化） ---
        int startRow = Mathf.Max(0, Mathf.FloorToInt(viewRect.y / cellSize));
        int endRow = Mathf.Min(rows, Mathf.CeilToInt((viewRect.y + viewRect.height) / cellSize));
        int startCol = Mathf.Max(0, Mathf.FloorToInt(viewRect.x / cellSize));
        int endCol = Mathf.Min(cols, Mathf.CeilToInt((viewRect.x + viewRect.width) / cellSize));

        for (int r = startRow; r < endRow; r++)
        {
            for (int c = startCol; c < endCol; c++)
            {
                string cellValue = mapData[r, c];
                if (string.IsNullOrEmpty(cellValue)) cellValue = "1";

                string[] data = cellValue.Split('_');
                string typeStr = data[0];
                bool hasID = data.Length > 1;

                Color chipColor = GetColorForType(typeStr);

                Rect drawRect = new Rect(gridAreaRect.x + c * cellSize + 0.5f, gridAreaRect.y + r * cellSize + 0.5f, cellSize - 1f, cellSize - 1f);

                // セルの背景描画
                EditorGUI.DrawRect(drawRect, chipColor);

                if (hasID)
                {
                    Handles.DrawSolidRectangleWithOutline(drawRect, Color.clear, Color.yellow);
                }

                // 文字の描画（キャッスしたスタイルを適用）
                float brightness = (chipColor.r + chipColor.g + chipColor.b) / 3f;
                GUIStyle targetStyle = (brightness > 0.5f && chipColor.a > 0.4f) ? centerLabelStyleBlack : centerLabelStyleWhite;

                if (typeStr == "6")
                {
                    GUI.Label(drawRect, "1", targetStyle);
                }
                else if (typeStr == "7")
                {
                    GUI.Label(drawRect, "2", targetStyle);
                }
                else if (data.Length > 1)
                {
                    GUI.Label(drawRect, data[1], targetStyle);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void SetCellData(int r, int c)
    {
        string writeValue = ((int)selectedType).ToString();
        if (selectedGimmickID != -1)
        {
            writeValue += "_" + selectedGimmickID;
        }

        if (mapData[r, c] != writeValue)
        {
            mapData[r, c] = writeValue;
            Repaint();
        }
    }

    private Color GetColorForType(string typeStr)
    {
        switch (typeStr)
        {
            case "0": return new Color(0.15f, 0.15f, 0.15f, 1.0f); // 0: 壁 (ダークグレー)
            case "1": return new Color(1.0f, 1.0f, 1.0f, 1.0f);     // 1: 床 (白)
            case "2": return new Color(0.7f, 0.7f, 0.7f, 1.0f);     // 2: 通路 (ライトグレー)
            case "3": return new Color(1.0f, 0.5f, 0.0f, 1.0f);     // 3: 敵 (オレンジ)
            case "4": return new Color(1.0f, 0.2f, 0.2f, 1.0f);     // 4: ゴール (赤)
            case "5": return new Color(0.0f, 0.8f, 1.0f, 1.0f);     // 5: 巡回ポイント (シアン)
            case "6": return new Color(0.2f, 0.6f, 1.0f, 1.0f);     // 6: プレイヤー1 (青)
            case "7": return new Color(0.2f, 0.9f, 0.3f, 1.0f);     // 7: プレイヤー2 (緑)
            case "8": return new Color(0.9f, 0.8f, 0.2f, 1.0f);     // 8: コア (黄色)
            default: return Color.white;
        }
    }

    void SaveToCSV()
    {
        StringBuilder sb = new StringBuilder();
        for (int r = 0; r < rows; r++)
        {
            string[] rowData = new string[cols];
            for (int c = 0; c < cols; c++)
            {
                rowData[c] = mapData[r, c];
            }
            sb.AppendLine(string.Join(",", rowData));
        }

        File.WriteAllText(csvFilePath, sb.ToString());
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("成功", "CSVファイルに保存しました。\n" + csvFilePath, "OK");
    }

    void LoadFromCSV()
    {
        if (!File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("エラー", "CSVファイルが見つかりません。", "OK");
            return;
        }

        string[] lines = File.ReadAllLines(csvFilePath);
        rows = lines.Length;
        cols = lines[0].Split(',').Length;
        mapData = new string[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            string[] values = lines[r].Split(',');
            for (int c = 0; c < cols; c++)
            {
                mapData[r, c] = values[c];
            }
        }
        Repaint();
    }
}