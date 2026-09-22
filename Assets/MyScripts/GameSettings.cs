using UnityEngine;

public enum DifficultyLevel
{
    Low = 0,
    Medium = 1,
    Hard = 2
}

public static class GameSettings
{
    public const string DifficultyPrefKey = "DifficultyLevel";
    public const string SoundPrefKey = "SoundEnabled";

    public static DifficultyLevel GetDifficulty()
    {
        int value = PlayerPrefs.GetInt(
            DifficultyPrefKey,
            (int)DifficultyLevel.Medium
        );

        if (value < 0 || value > 2)
            return DifficultyLevel.Medium;

        return (DifficultyLevel)value;
    }

    public static void SetDifficulty(DifficultyLevel difficulty)
    {
        PlayerPrefs.SetInt(DifficultyPrefKey, (int)difficulty);
        PlayerPrefs.Save();
    }

    public static bool IsSoundEnabled()
    {
        return PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
    }

    public static void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SoundPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySound();
    }

    public static void ApplySound()
    {
        bool enabled = IsSoundEnabled();

        AudioListener.volume = enabled ? 1f : 0f;
        AudioListener.pause = false;

        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
                sources[i].mute = !enabled;
        }
    }

    public static void ApplyDifficultyToPlayer(PlayerRunner player)
    {
        if (player == null)
            return;

        switch (GetDifficulty())
        {
            case DifficultyLevel.Low:
                player.runSpeed *= 0.85f;
                player.speedGainPerSecond *= 0.75f;
                player.maxRunSpeed *= 0.9f;
                break;

            case DifficultyLevel.Hard:
                player.runSpeed *= 1.2f;
                player.speedGainPerSecond *= 1.25f;
                player.maxRunSpeed *= 1.15f;
                break;
        }
    }
}
