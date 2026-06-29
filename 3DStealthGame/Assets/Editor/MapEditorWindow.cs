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
        StrongEnemy = 4,     // 強化敵
        Item = 5,            // アイテム
        Goal = 6,            // ゴール
        Switch = 7,          // スイッチ
        Respawn = 8,         // リスポーン
        PatrolPoint = 9,     // 巡回ポイント
        Player1 = 10,        // プレイヤー1
        Player2 = 11,        // プレイヤー2
        InvisibleWall = 12,  // 透明壁
        powerItemBlue = 13,  // 青アイテム
        powerItemGreen = 14, // 緑アイテム
        switchBlue = 15,     // 青スイッチ
        switchGreen = 16     // 緑スイッチ
    }

    #endregion

    #region マップデータ

    int rows = 100;                                    // 縦100マス
    int cols = 50;                                     // 横50マス
    string[,] mapData;                                 // マップデータを保持する2次元配列
    string csvFilePath = "Assets/Resources/map.csv";   // CSVファイルの保存パス
    #endregion

    #region エディタUI関連

    MapObjectType selectedType = MapObjectType.Floor;  // 初期のマップタイル
    int selectedGimmickID = -1;                        // 現在選択されている配置オブジェクトのID
    Vector2 scrollPosition;                            // スクロール位置管理
    const float cellSize = 22f;                        // 正方形マスサイズ定義

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

    void OnGUI()
    {
        GUILayout.Label("ステージ生成", EditorStyles.boldLabel);

        // 設定エリア
        EditorGUILayout.BeginVertical("box");
        csvFilePath = EditorGUILayout.TextField("CSV保存パス", csvFilePath);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 選択中のオブジェクトに応じた色を取得
        Color previewColor = GetColorForType(((int)selectedType).ToString());
        GUIStyle paletteLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        float brightness = (previewColor.r + previewColor.g + previewColor.b) / 3f;
        paletteLabelStyle.normal.textColor = (brightness > 0.5f && previewColor.a > 0.4f) ? Color.black : Color.white;
 
        // パレット・ID設定エリア
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("配置するオブジェクト、IDを選択", EditorStyles.boldLabel);

        Color originalGUIColor = GUI.backgroundColor;
        GUI.backgroundColor = previewColor;

        // オブジェクトタイプをドロップダウンで選択
        selectedType = (MapObjectType)EditorGUILayout.EnumPopup("配置するオブジェクト", selectedType);
        
        GUI.backgroundColor = originalGUIColor;

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

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // テキスト表示用の中央揃えスタイルを作成
        GUIStyle centerLabelStyle = new GUIStyle(EditorStyles.miniLabel);
        centerLabelStyle.alignment = TextAnchor.MiddleCenter;
        centerLabelStyle.fontStyle = FontStyle.Bold;
        centerLabelStyle.normal.textColor = Color.black;

        for (int r = 0; r < rows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols; c++)
            {
                Event currentEvent = Event.current;

                string cellValue = mapData[r, c];
                if (string.IsNullOrEmpty(cellValue)) cellValue = "1";

                string[] data = cellValue.Split('_');
                string typeStr = data[0];
                bool hasID = data.Length > 1;

                // IDごとにボタンの色を変えて視覚的にわかりやすくする
                Color originalColor = GUI.backgroundColor;
                // 判別用のカラー変数を定義
                Color chipColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                if (typeStr == "0") chipColor = new Color(0.1f, 0.1f, 0.1f, 1.0f); // 壁：ダークグレー
                else if (typeStr == "1") chipColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); // 床：白
                else if (typeStr == "2") chipColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 通路：ライトグレー
                else if (typeStr == "3") chipColor = new Color(1.0f, 0.5f, 0.0f, 1.0f); // 敵：オレンジ
                else if (typeStr == "4") chipColor = new Color(1.0f, 0.0f, 0.0f, 1.0f); // 敵：赤
                else if (typeStr == "5") chipColor = new Color(1.0f, 0.0f, 1.0f, 1.0f); // アイテム：ピンク
                else if (typeStr == "6") chipColor = new Color(1.0f, 1.0f, 0.0f, 1.0f); // ゴール：黄
                else if (typeStr == "7") chipColor = new Color(0.0f, 1.0f, 0.0f, 0.5f); // スイッチ：緑
                else if (typeStr == "8") chipColor = new Color(0.0f, 1.0f, 1.0f, 1.0f); // リスポーン：水色
                else if (typeStr == "9") chipColor = new Color(1.0f, 0.1f, 0.6f, 0.5f); // 追従ポイント：明るいピンク
                else if (typeStr == "10" || typeStr == "11") chipColor = new Color(1.0f, 1.0f, 0.6f, 1.0f); // プレイヤー：薄い黄
                else if (typeStr == "12") chipColor = new Color(0.5f, 0.25f, 0.0f, 1.0f); // 透明壁：茶色
                else if (typeStr == "13") chipColor = new Color(0.0f, 0.0f, 1.0f, 1.0f); // 青用アイテム：青
                else if (typeStr == "14") chipColor = new Color(0.4f, 1.0f, 0.4f, 1.0f); // 緑用アイテム：ライム緑
                else if (typeStr == "15") chipColor = new Color(0.0f, 0.0f, 1.0f, 1.0f); // 青用スイッチ：青
                else if (typeStr == "16") chipColor = new Color(0.4f, 1.0f, 0.4f, 1.0f); // 緑用スイッチ：ライム緑
                else if (int.TryParse(typeStr, out int tNum) && tNum > 1) chipColor = new Color(1.0f, 0.0f, 1.0f, 1.0f); // その他：紫
                
                Rect cellRect = GUILayoutUtility.GetRect(cellSize, cellSize, GUILayout.Width(cellSize), GUILayout.Height(cellSize));
                Rect drawRect = new Rect(cellRect.x + 0.5f, cellRect.y + 0.5f, cellSize - 1f, cellSize - 1f);
                EditorGUI.DrawRect(drawRect, chipColor);

                if (hasID)
                {
                    Handles.DrawSolidRectangleWithOutline(drawRect, Color.clear, Color.yellow);
                }

                if (typeStr == "10")
                {
                    GUI.Label(drawRect, "1", centerLabelStyle);
                }
                else if (typeStr == "11")
                {
                    GUI.Label(drawRect, "2", centerLabelStyle);
                }
                else if (typeStr == "13")
                {
                    GUI.Label(drawRect, "I", centerLabelStyle);
                }
                else if (typeStr == "14")
                {
                    GUI.Label(drawRect, "I", centerLabelStyle);
                }
                else if (typeStr == "15")
                {
                    GUI.Label(drawRect, "S", centerLabelStyle);
                }
                else if (typeStr == "16")
                {
                    GUI.Label(drawRect, "S", centerLabelStyle);
                }
                else if (data.Length > 1)
                {
                    GUI.Label(drawRect, data[1], EditorStyles.miniLabel);
                }

                // マウスドラッグの判定
                if (currentEvent != null && cellRect.Contains(currentEvent.mousePosition) &&
                    (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag))
                {
                    if (currentEvent.button == 0) // 左クリック・左ドラッグ
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
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private Color GetColorForType(string typeStr)
    {
        if (typeStr == "0") return new Color(0.1f, 0.1f, 0.1f, 1.0f); // 壁：ダークグレー
        if (typeStr == "1") return new Color(1.0f, 1.0f, 1.0f, 1.0f); // 床：白
        if (typeStr == "2") return new Color(0.7f, 0.7f, 0.7f, 1.0f); // 通路：ライトグレー
        if (typeStr == "3") return new Color(1.0f, 0.5f, 0.0f, 1.0f); // 敵：オレンジ
        if (typeStr == "4") return new Color(1.0f, 0.0f, 0.0f, 1.0f); // 敵：赤
        if (typeStr == "5") return new Color(1.0f, 0.0f, 1.0f, 1.0f); // アイテム：ピンク
        if (typeStr == "6") return new Color(1.0f, 1.0f, 0.0f, 1.0f); // ゴール：黄
        if (typeStr == "7") return new Color(0.0f, 1.0f, 0.0f, 0.5f); // スイッチ：緑
        if (typeStr == "8") return new Color(0.0f, 1.0f, 1.0f, 1.0f); // リスポーン：水色
        if (typeStr == "9") return new Color(1.0f, 0.1f, 0.6f, 0.5f); // 追従ポイント：明るいピンク
        if (typeStr == "10" || typeStr == "11") return new Color(1.0f, 1.0f, 0.6f, 1.0f); // プレイヤー：薄い黄
        if (typeStr == "12") return new Color(0.5f, 0.25f, 0.0f, 1.0f); // 透明壁：茶色
        if (typeStr == "13") return new Color(0.0f, 0.0f, 1.0f, 1.0f); // 青用アイテム：青
        if (typeStr == "14") return new Color(0.4f, 1.0f, 0.4f, 1.0f); // 緑用アイテム：ライム緑
        if (typeStr == "15") return new Color(0.0f, 0.0f, 1.0f, 1.0f); // 青用スイッチ：青
        if (typeStr == "16") return new Color(0.4f, 1.0f, 0.4f, 1.0f); // 緑用スイッチ：ライム緑

        if (int.TryParse(typeStr, out int tNum) && tNum > 1) return new Color(1.0f, 0.0f, 1.0f, 1.0f); // その他：紫
        return Color.white;
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
