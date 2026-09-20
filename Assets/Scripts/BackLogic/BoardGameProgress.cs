using UnityEngine;

public sealed class BoardGameProgress : MonoBehaviour
{
    private ArsenalSaveData saveData;

    [Header("Progression")]
    [SerializeField] private int xpPerKillScorePoint = 1;
    [SerializeField] private int waveClearBaseXp = 25;
    [SerializeField] private int xpPerLevel = 250;

    public int BestScore => saveData != null ? saveData.bestScore : 0;
    public int HighestWaveReached => saveData != null ? saveData.highestWaveReached : 0;
    public int TotalRuns => saveData != null ? saveData.totalRuns : 0;
    public int TotalEnemiesDestroyed => saveData != null ? saveData.totalEnemiesDestroyed : 0;
    public int TotalWavesCleared => saveData != null ? saveData.totalWavesCleared : 0;
    public int Level => saveData?.playerRank != null ? saveData.playerRank.level : 1;
    public string Rank => saveData?.playerRank != null ? saveData.playerRank.rank : "Recruit";

    private void Awake()
    {
        EnsureLoaded();
    }

    public void EnsureLoaded()
    {
        if (saveData != null)
            return;

        saveData = ArsenalSaveSystem.LoadCurrent() ?? new ArsenalSaveData();
        NormalizeProgression();
    }

    public void BeginRun()
    {
        EnsureLoaded();
        saveData.totalRuns++;
        ArsenalSaveSystem.SaveCurrent(saveData);
    }

    public void RegisterKill(int scoreValue)
    {
        EnsureLoaded();
        saveData.totalEnemiesDestroyed++;
        AddXp(Mathf.Max(1, scoreValue) * Mathf.Max(1, xpPerKillScorePoint));
    }

    public void RegisterWaveClear(int score, int waveIndex)
    {
        EnsureLoaded();
        saveData.totalWavesCleared++;
        AddXp(waveClearBaseXp * Mathf.Max(1, waveIndex));
        UpdateRecords(score, waveIndex);
        ArsenalSaveSystem.SaveCurrent(saveData);
    }

    public void FinalizeRun(int score, int waveIndex)
    {
        EnsureLoaded();
        UpdateRecords(score, waveIndex);
        ArsenalSaveSystem.SaveCurrent(saveData);

        // One history snapshot per completed run, rather than one file per kill/wave.
        ArsenalSaveSystem.SaveNew(saveData);
    }

    public void UpdateRunResultsIfBetter(int score, int waveIndex)
    {
        EnsureLoaded();
        UpdateRecords(score, waveIndex);
        ArsenalSaveSystem.SaveCurrent(saveData);
    }

    private void AddXp(int amount)
    {
        if (saveData.playerRank == null)
            saveData.playerRank = new ArsenalSaveData.PlayerRankEntry();

        saveData.playerRank.xp = Mathf.Max(0, saveData.playerRank.xp + Mathf.Max(0, amount));
        NormalizeProgression();
    }

    private void NormalizeProgression()
    {
        if (saveData == null)
            saveData = new ArsenalSaveData();

        if (saveData.playerRank == null)
            saveData.playerRank = new ArsenalSaveData.PlayerRankEntry();

        int level = 1 + Mathf.Max(0, saveData.playerRank.xp) / Mathf.Max(1, xpPerLevel);
        saveData.playerRank.level = Mathf.Max(1, level);
        saveData.playerRank.rank = GetRankForLevel(saveData.playerRank.level);
    }

    private void UpdateRecords(int score, int waveIndex)
    {
        if (score > saveData.bestScore)
            saveData.bestScore = score;

        if (waveIndex > saveData.highestWaveReached)
            saveData.highestWaveReached = waveIndex;
    }

    private static string GetRankForLevel(int level)
    {
        if (level >= 15) return "Ace";
        if (level >= 10) return "Veteran";
        if (level >= 6) return "Specialist";
        if (level >= 3) return "Operator";
        return "Recruit";
    }
}
