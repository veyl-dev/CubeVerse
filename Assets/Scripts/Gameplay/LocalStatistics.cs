using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CubeVerse.Gameplay
{
    [Serializable]
    public sealed class LocalStatistics
    {
        public int totalSolves;
        public float bestSingle = -1f;
        public float bestAo5 = -1f;
        public List<float> recentSolves = new List<float>();

        public static LocalStatistics Load()
        {
            const string key = "CubeVerse.LocalStatistics.v1";
            if (!PlayerPrefs.HasKey(key)) return new LocalStatistics();
            try { return JsonUtility.FromJson<LocalStatistics>(PlayerPrefs.GetString(key)) ?? new LocalStatistics(); }
            catch { return new LocalStatistics(); }
        }

        public void Record(float seconds)
        {
            totalSolves++;
            if (bestSingle < 0 || seconds < bestSingle) bestSingle = seconds;
            recentSolves.Add(seconds);
            if (recentSolves.Count > 100) recentSolves.RemoveAt(0);
            if (recentSolves.Count >= 5)
            {
                float ao5 = AverageOfFive(recentSolves.Skip(recentSolves.Count - 5).Take(5).ToArray());
                if (ao5 >= 0 && (bestAo5 < 0 || ao5 < bestAo5)) bestAo5 = ao5;
            }
            PlayerPrefs.SetString("CubeVerse.LocalStatistics.v1", JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        // WCA-style Ao5: discard one fastest and one slowest result, then average the remaining three.
        public static float AverageOfFive(IReadOnlyList<float> times)
        {
            if (times == null || times.Count != 5 || times.Any(t => t < 0 || float.IsNaN(t) || float.IsInfinity(t))) return -1f;
            float sum = 0f, min = float.MaxValue, max = float.MinValue;
            foreach (float t in times) { sum += t; min = Math.Min(min, t); max = Math.Max(max, t); }
            return (sum - min - max) / 3f;
        }
    }
}
