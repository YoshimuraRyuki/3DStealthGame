using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedMap : MonoBehaviour
{
    [Header("CSVファイル（Resourcesフォルダに入れる）")]
    public string fileName = "map"; // Resources/map.csv

    private int[,] map;

    public int[,] Generate()
    {
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);

        if (csvFile == null)
        {
            Debug.LogError("CSVファイルが見つからない: " + fileName);
            return null;
        }

        string[] lines = csvFile.text.Split('\n');

        int height = lines.Length;
        int width = lines[0].Split(',').Length;

        map = new int[width, height];

        for (int y = 0; y < height; y++)
        {
            string line = lines[y].Trim();

            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');

            for (int x = 0; x < width; x++)
            {
                int value = 0;

                if (x < values.Length)
                {
                    int.TryParse(values[x], out value);
                }

                // 上下反転（Unityの座標に合わせる）
                map[x, height - 1 - y] = value;
            }
        }

        return map;
    }
}
