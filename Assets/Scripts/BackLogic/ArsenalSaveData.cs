using System;
using System.Collections.Generic;

[Serializable]
public sealed class ArsenalSaveData
{
    [Serializable]
    public sealed class PlayerRankEntry
    {
        public int level = 1;
        public string rank = "Recruit";
        public int xp;

        public PlayerRankEntry() { }

        public PlayerRankEntry(int level, string rank, int xp)
        {
            this.level = level;
            this.rank = rank;
            this.xp = xp;
        }
    }

    public string playerName = "Player";
    public int bestScore;
    public int totalRuns;
    public int highestWaveReached;
    public int totalEnemiesDestroyed;
    public int totalWavesCleared;

    public PlayerRankEntry playerRank = new();

    public List<string> encounteredTankIds = new();
    public List<string> unlockedTankIds = new();
    public List<string> unlockedSkinIds = new();
}
