using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Tracks lifetime match statistics: fights played, wins, losses, and
    /// current win streak. Persists to PlayerPrefs so stats survive across
    /// sessions. Any system can resolve IStatsTracker to read or update.
    /// </summary>
    public interface IStatsTracker
    {
        int FightsPlayed { get; }
        int Wins { get; }
        int Losses { get; }
        int WinStreak { get; }
        int BestWinStreak { get; }

        void RecordWin();
        void RecordLoss();
        void Reset();
    }

    public class StatsTracker : MonoBehaviour, IStatsTracker
    {
        private const string PlayedKey = "Rival_Stats_Played";
        private const string WinsKey = "Rival_Stats_Wins";
        private const string LossesKey = "Rival_Stats_Losses";
        private const string StreakKey = "Rival_Stats_Streak";
        private const string BestKey = "Rival_Stats_BestStreak";

        public int FightsPlayed { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int WinStreak { get; private set; }
        public int BestWinStreak { get; private set; }

        private void Awake()
        {
            ServiceLocator.Register<IStatsTracker>(this);
            Load();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IStatsTracker>(this);
        }

        public void RecordWin()
        {
            FightsPlayed++;
            Wins++;
            WinStreak++;
            if (WinStreak > BestWinStreak) BestWinStreak = WinStreak;
            Save();
        }

        public void RecordLoss()
        {
            FightsPlayed++;
            Losses++;
            WinStreak = 0;
            Save();
        }

        public void Reset()
        {
            FightsPlayed = 0;
            Wins = 0;
            Losses = 0;
            WinStreak = 0;
            BestWinStreak = 0;
            Save();
        }

        private void Load()
        {
            FightsPlayed = PlayerPrefs.GetInt(PlayedKey);
            Wins = PlayerPrefs.GetInt(WinsKey);
            Losses = PlayerPrefs.GetInt(LossesKey);
            WinStreak = PlayerPrefs.GetInt(StreakKey);
            BestWinStreak = PlayerPrefs.GetInt(BestKey);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PlayedKey, FightsPlayed);
            PlayerPrefs.SetInt(WinsKey, Wins);
            PlayerPrefs.SetInt(LossesKey, Losses);
            PlayerPrefs.SetInt(StreakKey, WinStreak);
            PlayerPrefs.SetInt(BestKey, BestWinStreak);
            PlayerPrefs.Save();
        }
    }
}
