using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedMap : MonoBehaviour
{
    [Header("CSVファイル")]
    public string fileName = "map"; // Resources/map.csv

    // 読み込んだマップを保存する二次元配列
    private int[,] map;

    /// <summary>
    /// int型の二次元配列を返す関数
    /// </summary>
    /// <returns></returns>
    public int[,] Generate()
    {
        // CSVファイルを読み込む
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);

        if (csvFile == null)
        {
            Debug.LogError("CSVファイルが見つからない: " + fileName);
            return null;
        }

        // 行ごとに改行
        string[] lines = csvFile.text.Split('\n');

        // マップサイズ決定
        int height = lines.Length;
        int width = lines[0].Split(',').Length;

        // 配列の作成
        map = new int[width, height];

        // CSVを1マスずつ読む
        for (int y = 0; y < height; y++)
        {
            // 改行や空白を削除
            string line = lines[y].Trim();
            // 空白をスキップ
            if (string.IsNullOrEmpty(line)) continue;
            // カンマで分割
            string[] values = line.Split(',');

            // 数値に変換
            for (int x = 0; x < width; x++)
            {
                int value = 0;

                if (x < values.Length)
                {
                    int.TryParse(values[x], out value);
                }

                // Unityの座標にあわせるため上下を反転
                map[x, height - 1 - y] = value;
            }
        }

        return map;
    }
}
