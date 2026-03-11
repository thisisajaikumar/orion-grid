using System;
using System.IO;
using UnityEngine;


[Serializable]
public class SaveData
{
    public int   currentLevel;
    public int[] bestScores = new int[5];
    public bool  hasActiveSession;
    public float sessionTime;
    public int   sessionScore;
    public int   sessionCombo;
    public int   sessionMatchesFound;
    public int[] matchedPairIndices;
}

public static class SaveSystem
{
    static readonly string FilePath =
        Path.Combine(Application.persistentDataPath, "progress.json");

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Write failed – {ex.Message}");
        }
    }

    public static SaveData Load()
    {
        if (!File.Exists(FilePath))
            return new SaveData();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SaveSystem] Corrupt save, resetting. {ex.Message}");
            return new SaveData();
        }
    }

    public static void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}
