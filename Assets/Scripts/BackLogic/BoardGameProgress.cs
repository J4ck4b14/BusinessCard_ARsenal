using UnityEngine;

public sealed class BoardGameProgress : MonoBehaviour
{
    [SerializeField] private ArsenalSaveData saveData;

    public int BestScore => saveData != null ? saveData.bestScore : 0;
    public int HighestWaveReached => saveData != null ? saveData.highestWaveReached : 0;
    public int TotalRuns => saveData != null ? saveData.totalRuns : 0;

    private void Awake()
    {
        if (saveData == null)
            saveData = ArsenalSaveSystem.LoadLatest();
    }

    public void EnsureLoaded()
    {
        if (saveData == null)
            saveData = ArsenalSaveSystem.LoadLatest() ?? new ArsenalSaveData();
    }

    public void BeginRun()
    {
        EnsureLoaded();
        saveData.totalRuns++;
        ArsenalSaveSystem.SaveNew(saveData);
    }

    public void UpdateRunResultsIfBetter(int score, int waveIndex)
    {
        EnsureLoaded();

        var changed = false;

        if (score > saveData.bestScore)
        {
            saveData.bestScore = score;
            changed = true;
        }

        if (waveIndex > saveData.highestWaveReached)
        {
            saveData.highestWaveReached = waveIndex;
            changed = true;
        }

        if (changed)
            ArsenalSaveSystem.SaveNew(saveData);
    }
}