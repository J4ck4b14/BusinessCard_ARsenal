using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ArsenalSaveSystem
{
    private const string FileName = "arsenal_save.json";

    private const string CurrentSaveFileName = "arsenal_current.json";

    private static string CurrentSaveFilePath =>
        Path.Combine(SaveDirectory, CurrentSaveFileName);

    // Timestamped multi-save pattern
    private const string TimeStampedPrefix = "arsenal_";
    private const string TimeStampedExtension = ".json";
    private const string TimeStampedFormat = "yyyyMMdd_HHmmss_fff";

    private static string SaveDirectory => Application.persistentDataPath;
    private static string SaveFilePath => Path.Combine(SaveDirectory, FileName);

    /*
     *  Legacy single-save pattern (commented out for reference)
     * 
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static ArsenalSaveData Load()
    {
        if (!File.Exists(SaveFilePath))
            return new ArsenalSaveData();

        string json = File.ReadAllText(SaveFilePath);
        return JsonUtility.FromJson<ArsenalSaveData>(json) ?? new ArsenalSaveData();
    }

    public static void Save(ArsenalSaveData data)
    {
        if(data == null)
            return;

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(CurrentSaveFilePath, json);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
            File.Delete(SaveFilePath);
    }
    */

    public sealed class SaveListEntry
    {
        public string fileName;
        public string timestamp;
        public string displayName;
    }

    public static string SaveNew(ArsenalSaveData data)
    {
        if (data == null)
            return null;

        Directory.CreateDirectory(SaveDirectory);

        DateTime now = DateTime.Now;
        string stamp = now.ToString(TimeStampedFormat, CultureInfo.InvariantCulture);
        string fileName = $"{TimeStampedPrefix}{stamp}{TimeStampedExtension}";
        string path = Path.Combine(SaveDirectory, fileName);

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(path, json);

        return fileName;
    }

    public static void SaveCurrent(ArsenalSaveData data)
    {
        if (data == null)
            return;

        Directory.CreateDirectory(SaveDirectory);

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(CurrentSaveFilePath, json);
    }

    public static ArsenalSaveData LoadCurrent()
    {
        if (!File.Exists(CurrentSaveFilePath))
            return new ArsenalSaveData();

        string json = File.ReadAllText(CurrentSaveFilePath);
        return JsonUtility.FromJson<ArsenalSaveData>(json) ?? new ArsenalSaveData();
    }

    public static void DeleteCurrent()
    {
        if (File.Exists(CurrentSaveFilePath))
            File.Delete(CurrentSaveFilePath);
    }

    public static ArsenalSaveData Load(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return new ArsenalSaveData();

        string safeFileName = Path.GetFileName(fileName); // Prevent directory traversal
        string path = Path.Combine(SaveDirectory, safeFileName);

        if (!File.Exists(path))
            return new ArsenalSaveData();

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<ArsenalSaveData>(json) ?? new ArsenalSaveData();
    }

    public static ArsenalSaveData LoadLatest()
    {
        SaveListEntry latest = ListSaves().OrderByDescending(e => e.fileName, StringComparer.Ordinal).FirstOrDefault();
        return latest == null ? new ArsenalSaveData() : Load(latest.fileName);

    }

    public static List<SaveListEntry> ListSaves()
    {
        List<SaveListEntry> results = new List<SaveListEntry>();

        if (!Directory.Exists(SaveDirectory))
            return results;

        string[] files = Directory.GetFiles(SaveDirectory, $"{TimeStampedPrefix}*{TimeStampedExtension}");

        foreach (string path in files)
        {
            string fileName = Path.GetFileName(path);

            DateTime savedAt = TryParseTimestampFromFileName(fileName, out DateTime parsed)
                ? parsed : File.GetLastWriteTime(path);

            ArsenalSaveData data = TryLoadArsenalSaveData(path);

            string playerName = string.IsNullOrWhiteSpace(data?.playerName) ? "Player" : data.playerName;
            (int level, string rank) = GetPlayerLvlRank(data);

            string display = string.Format(
                CultureInfo.InvariantCulture,
                "{0, -10} Lvl {1, -3} {2, -12} {3:dd/MM/yy HH:mm:ss}",
                Truncate(playerName, 10),
                level,
                Truncate(rank, 12),
                savedAt);

            results.Add(new SaveListEntry
            {
                fileName = fileName,
                timestamp = savedAt.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture),
                displayName = display
            });
        }

        results.Sort((a, b) => string.CompareOrdinal(b.fileName, a.fileName)); // Newest first
        return results;
    }

    private static ArsenalSaveData TryLoadArsenalSaveData(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<ArsenalSaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseTimestampFromFileName(string fileName, out DateTime savedAtLocal)
    {
        savedAtLocal = default;

        if (string.IsNullOrEmpty(fileName))
            return false;

        if (!fileName.StartsWith(TimeStampedPrefix, StringComparison.Ordinal))
            return false;

        if (!fileName.EndsWith(TimeStampedExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        string core = fileName.Substring(

            TimeStampedPrefix.Length,
            fileName.Length - TimeStampedPrefix.Length - TimeStampedExtension.Length);

        return DateTime.TryParseExact(
            core,
            TimeStampedFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out savedAtLocal);
    }

    private static (int level, string rank) GetPlayerLvlRank(ArsenalSaveData data)
    {
        if (data == null || data.playerRank == null)
            return (0, "Unranked");

        int level = data.playerRank.level;
        string rank = string.IsNullOrEmpty(data.playerRank.rank) ? "Unranked" : data.playerRank.rank;

        return (level, rank);
    }

    private static string Truncate(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLen ? value : value.Substring(0, maxLen);
    }
}
