using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Screens")]
    public GameObject mainScreen;
    public GameObject characterSelectScreen;
    public GameObject loadingScreen;
    public GameObject difficultyScreen;

    [Header("Loading Settings")]
    public TMP_Text loadingText;
    public float loadingDuration = 3.5f;
    public string gameSceneName = "Basic_Learning";

    [Header("Character Previews")]
    public GameObject[] previewCharacters;

    int previewIndex;
    bool isLoading;

    void Start()
    {
        previewIndex = GameManager.GetSelectedPlayerIndex();
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        isLoading = false;
        SetScreen(mainScreen, true);
        SetScreen(characterSelectScreen, false);
        SetScreen(loadingScreen, false);
        SetScreen(difficultyScreen, false);
    }

    public void OpenCharacterSelect()
    {
        if (isLoading) return;
        previewIndex = ClampPreviewIndex(GameManager.GetSelectedPlayerIndex());
        SetScreen(mainScreen, false);
        SetScreen(loadingScreen, false);
        SetScreen(characterSelectScreen, true);
        SetScreen(difficultyScreen, false);
        ShowPreview(previewIndex);
    }

    public void OpenDifficultySelect()
    {
        if (isLoading) return;
        SetScreen(mainScreen, false);
        SetScreen(characterSelectScreen, false);
        SetScreen(loadingScreen, false);
        SetScreen(difficultyScreen, true);
    }

    public void SetDifficulty(float speedMultiplier)
    {
        PlayerPrefs.SetFloat("GameSpeedMultiplier", speedMultiplier);
        PlayerPrefs.Save();
        Debug.Log("Difficulty saved: " + speedMultiplier);
        ShowMainMenu();
    }

    public void SelectCurrentCharacter()
    {
        GameManager.SetSelectedPlayerIndex(ClampPreviewIndex(previewIndex));
        ShowMainMenu();
    }

    public void OnPlayPressed()
    {
        if (isLoading) return;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        isLoading = true;
        SetScreen(mainScreen, false);
        SetScreen(characterSelectScreen, false);
        SetScreen(difficultyScreen, false);
        SetScreen(loadingScreen, true);

        float elapsed = 0f;
        while (elapsed < loadingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        SceneManager.LoadScene(gameSceneName);
    }

    void ShowPreview(int index)
    {
        if (previewCharacters == null) return;
        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] != null) previewCharacters[i].SetActive(i == index);
        }
    }

    int ClampPreviewIndex(int index)
    {
        if (previewCharacters == null || previewCharacters.Length == 0) return 0;
        return Mathf.Clamp(index, 0, previewCharacters.Length - 1);
    }

    static void SetScreen(GameObject screen, bool visible)
    {
        if (screen != null) screen.SetActive(visible);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit Checked!");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}