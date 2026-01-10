using UnityEngine;

public static class SaveManager
{
    public static void SaveResult(string mapId, int score, float time)
    {
        PlayerPrefs.SetInt($"score_{mapId}", score);
        PlayerPrefs.SetFloat($"time_{mapId}", time);
        PlayerPrefs.Save();
    }

    public static int GetBestScore(string mapId)
    {
        return PlayerPrefs.GetInt($"score_{mapId}", 0);
    }

    public static float GetBestTime(string mapId)
    {
        return PlayerPrefs.GetFloat($"time_{mapId}", 0f);
    }
}
