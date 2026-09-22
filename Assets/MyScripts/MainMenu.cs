using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    const float SwipeThreshold = 80f;
    const float DotInterval = 0.35f;

    [Header("Screens")]
    public GameObject mainScreen;
    public GameObject characterSelectScreen;
    public GameObject loadingScreen;
    public GameObject settingsScreen;

    [Header("Buttons")]
    public Button playButton;
    public Button characterButton;
    public Button selectButton;
    public Button quitButton;
    public Button settingsButton;

    [Header("Settings UI")]
    public Button lowDifficultyButton;
    public Button mediumDifficultyButton;
    public Button hardDifficultyButton;
    public Toggle soundToggle;
    public Button settingsBackButton;

    [Header("Loading")]
    public TMP_Text loadingText;
    public float loadingDuration = 3.5f;
    public string gameSceneName = "GamePlay";

    [Header("Character Previews")]
    [Tooltip("The 4 menu characters named 1, 2, 3, 4. Index 0 is character 1.")]
    public GameObject[] previewCharacters;

    int previewIndex;
    bool swipeArmed;
    float swipeStartX;
    float swipeStartY;
    bool isLoading;
    TMP_FontAsset uiFont;

    readonly Color selectedDifficultyColor = new Color(1f, 0.82f, 0.2f, 1f);
    readonly Color normalDifficultyColor = new Color(1f, 1f, 1f, 1f);

    void Awake()
    {
        AutoBind();
        EnsureSettingsScreen();
        WireButtons();
        GameSettings.ApplySound();
    }

    void Start()
    {
        previewIndex = GameManager.GetSelectedPlayerIndex();
        HideAllPreviews();
        RefreshSettingsUi();
        ShowMainMenu();
    }

    void Update()
    {
        if (isLoading)
            return;

        if (settingsScreen != null && settingsScreen.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ShowMainMenu();
            return;
        }

        if (characterSelectScreen == null || !characterSelectScreen.activeSelf)
            return;

        ReadKeyboard();
        ReadSwipe();
    }

    void AutoBind()
    {
        if (mainScreen == null)
            mainScreen = FindUi("MainScreen");

        if (characterSelectScreen == null)
        {
            characterSelectScreen = FindUi("CharacterSecltion");

            if (characterSelectScreen == null)
                characterSelectScreen = FindUi("CharacterSelectionScreen");
        }

        if (loadingScreen == null)
            loadingScreen = FindUi("LoadingScreen");

        if (settingsScreen == null)
            settingsScreen = FindUi("SettingsScreen");

        if (playButton == null)
            playButton = FindButton("Play");

        if (characterButton == null)
            characterButton = FindButton("CharacterSelection");

        if (selectButton == null)
            selectButton = FindButton("Select");

        if (quitButton == null)
            quitButton = FindButton("Quit");

        if (settingsButton == null)
            settingsButton = FindButton("Difficulty");

        if (loadingText == null && loadingScreen != null)
            loadingText = loadingScreen.GetComponentInChildren<TMP_Text>(true);

        if (loadingText != null)
            uiFont = loadingText.font;

        CachePreviewCharacters();
        EnsureArrowButtons();
    }

    void CachePreviewCharacters()
    {
        if (HasPreviewCharacters())
            return;

        GameObject[] allObjects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        GameObject[] found = new GameObject[4];
        int count = 0;

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];

            if (go == null || go.transform.parent != null)
                continue;

            if (go.name == "1" || go.name == "2" || go.name == "3" || go.name == "4")
            {
                int slot = go.name[0] - '1';
                found[slot] = go;
                count++;
            }
        }

        if (count > 0)
            previewCharacters = found;
    }

    bool HasPreviewCharacters()
    {
        if (previewCharacters == null || previewCharacters.Length == 0)
            return false;

        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] != null)
                return true;
        }

        return false;
    }

    void WireButtons()
    {
        BindClick(playButton, OnPlayPressed);
        BindClick(characterButton, OpenCharacterSelect);
        BindClick(selectButton, SelectCurrentCharacter);
        BindClick(quitButton, QuitGame);
        BindClick(settingsButton, OpenSettings);
        BindClick(settingsBackButton, ShowMainMenu);
        BindClick(lowDifficultyButton, () => SetDifficulty(DifficultyLevel.Low));
        BindClick(mediumDifficultyButton, () => SetDifficulty(DifficultyLevel.Medium));
        BindClick(hardDifficultyButton, () => SetDifficulty(DifficultyLevel.Hard));
        WireSoundToggle();
    }

    void WireSoundToggle()
    {
        if (soundToggle == null && settingsScreen != null)
        {
            soundToggle = settingsScreen.GetComponentInChildren<Toggle>(true);
        }

        if (soundToggle == null)
            return;

        soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
        soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
        soundToggle.SetIsOnWithoutNotify(GameSettings.IsSoundEnabled());
    }

    static void BindClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void ShowMainMenu()
    {
        isLoading = false;
        HideAllPreviews();
        SetScreen(mainScreen, true);
        SetScreen(characterSelectScreen, false);
        SetScreen(loadingScreen, false);
        SetScreen(settingsScreen, false);
    }

    public void OpenSettings()
    {
        if (isLoading)
            return;

        EnsureSettingsScreen();
        CacheSettingsRefs();
        WireButtons();

        HideAllPreviews();
        SetScreen(mainScreen, false);
        SetScreen(characterSelectScreen, false);
        SetScreen(loadingScreen, false);
        SetScreen(settingsScreen, true);
        RefreshSettingsUi();
        GameSettings.ApplySound();
    }

    public void OpenCharacterSelect()
    {
        if (isLoading)
            return;

        previewIndex = ClampPreviewIndex(GameManager.GetSelectedPlayerIndex());

        SetScreen(mainScreen, false);
        SetScreen(loadingScreen, false);
        SetScreen(settingsScreen, false);
        SetScreen(characterSelectScreen, true);

        StretchCharacterSelect();
        EnsureArrowButtons();
        ShowPreview(previewIndex);
    }

    public void SelectCurrentCharacter()
    {
        GameManager.SetSelectedPlayerIndex(ClampPreviewIndex(previewIndex));
        ShowMainMenu();
    }

    public void OnPlayPressed()
    {
        if (isLoading)
            return;

        StartCoroutine(PlaySequence());
    }

    void SetDifficulty(DifficultyLevel difficulty)
    {
        GameSettings.SetDifficulty(difficulty);
        RefreshSettingsUi();
    }

    public void OnSoundToggleChanged(bool enabled)
    {
        GameSettings.SetSoundEnabled(enabled);
    }

    void RefreshSettingsUi()
    {
        DifficultyLevel difficulty = GameSettings.GetDifficulty();

        HighlightDifficultyButton(lowDifficultyButton, difficulty == DifficultyLevel.Low);
        HighlightDifficultyButton(mediumDifficultyButton, difficulty == DifficultyLevel.Medium);
        HighlightDifficultyButton(hardDifficultyButton, difficulty == DifficultyLevel.Hard);

        if (soundToggle == null && settingsScreen != null)
            soundToggle = settingsScreen.GetComponentInChildren<Toggle>(true);

        if (soundToggle != null)
            soundToggle.SetIsOnWithoutNotify(GameSettings.IsSoundEnabled());

        GameSettings.ApplySound();
    }

    void HighlightDifficultyButton(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();

        if (image != null)
            image.color = selected ? selectedDifficultyColor : normalDifficultyColor;
    }

    IEnumerator PlaySequence()
    {
        isLoading = true;

        HideAllPreviews();
        SetScreen(mainScreen, false);
        SetScreen(characterSelectScreen, false);
        SetScreen(settingsScreen, false);
        SetScreen(loadingScreen, true);

        if (loadingScreen != null)
            loadingScreen.transform.SetAsLastSibling();

        float elapsed = 0f;
        int dotFrame = 0;
        float dotTimer = 0f;

        SetLoadingText(0);

        while (elapsed < loadingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            dotTimer += Time.unscaledDeltaTime;

            if (dotTimer >= DotInterval)
            {
                dotTimer = 0f;
                dotFrame = (dotFrame + 1) % 4;
                SetLoadingText(dotFrame);
            }

            yield return null;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    void SetLoadingText(int dotCount)
    {
        if (loadingText == null)
            return;

        switch (dotCount)
        {
            case 1:
                loadingText.text = "LOADING.";
                break;
            case 2:
                loadingText.text = "LOADING..";
                break;
            case 3:
                loadingText.text = "LOADING...";
                break;
            default:
                loadingText.text = "LOADING";
                break;
        }
    }

    void ReadKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            StepPreview(-1);

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            StepPreview(1);

        if (Input.GetKeyDown(KeyCode.Escape))
            ShowMainMenu();
    }

    void ReadSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverSelectableUi())
                return;

            swipeArmed = true;
            swipeStartX = Input.mousePosition.x;
            swipeStartY = Input.mousePosition.y;
        }

        if (!swipeArmed || !Input.GetMouseButtonUp(0))
            return;

        swipeArmed = false;

        Vector2 delta = new Vector2(
            Input.mousePosition.x - swipeStartX,
            Input.mousePosition.y - swipeStartY
        );

        if (delta.magnitude < SwipeThreshold)
            return;

        if (Mathf.Abs(delta.x) <= Mathf.Abs(delta.y))
            return;

        StepPreview(delta.x > 0f ? -1 : 1);
    }

    static bool IsPointerOverSelectableUi()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    void StepPreview(int direction)
    {
        int count = PreviewCount();

        if (count <= 0)
            return;

        previewIndex = (previewIndex + direction + count) % count;
        ShowPreview(previewIndex);
    }

    void ShowPreview(int index)
    {
        if (previewCharacters == null)
            return;

        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] != null)
                previewCharacters[i].SetActive(i == index);
        }
    }

    void HideAllPreviews()
    {
        if (previewCharacters == null)
            return;

        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] != null)
                previewCharacters[i].SetActive(false);
        }
    }

    int PreviewCount()
    {
        if (previewCharacters == null)
            return 0;

        int count = 0;

        for (int i = 0; i < previewCharacters.Length; i++)
        {
            if (previewCharacters[i] != null)
                count++;
        }

        return count;
    }

    int ClampPreviewIndex(int index)
    {
        int count = PreviewCount();

        if (count <= 0)
            return 0;

        return Mathf.Clamp(index, 0, count - 1);
    }

    void EnsureArrowButtons()
    {
        if (characterSelectScreen == null)
            return;

        if (characterSelectScreen.transform.Find("ArrowLeft") != null)
            return;

        CreateArrowButton(
            "ArrowLeft",
            "<",
            new Vector2(0f, 0.5f),
            new Vector2(80f, 0f),
            -1
        );

        CreateArrowButton(
            "ArrowRight",
            ">",
            new Vector2(1f, 0.5f),
            new Vector2(-80f, 0f),
            1
        );
    }

    void CreateArrowButton(
        string objectName,
        string label,
        Vector2 anchor,
        Vector2 anchoredPosition,
        int direction
    )
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(characterSelectScreen.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(110f, 110f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        int step = direction;
        button.onClick.AddListener(() => StepPreview(step));

        CreateLabel(buttonObject.transform, "Label", label, 72f, new Color(0.15f, 0.15f, 0.15f, 1f));
    }

    void EnsureSettingsScreen()
    {
        if (settingsScreen == null)
            settingsScreen = FindUi("SettingsScreen");

        if (settingsScreen == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas == null)
                return;

            settingsScreen = CreateSettingsScreen(canvas.transform);
        }

        CacheSettingsRefs();

        if (settingsScreen != null)
            settingsScreen.SetActive(false);
    }

    GameObject CreateSettingsScreen(Transform canvas)
    {
        GameObject screen = new GameObject(
            "SettingsScreen",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        screen.transform.SetParent(canvas, false);
        screen.layer = 5;

        RectTransform screenRect = screen.GetComponent<RectTransform>();
        StretchFull(screenRect);

        Image bg = screen.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.1f, 0.08f, 0.92f);
        bg.raycastTarget = true;

        CreateLabel(
            screen.transform,
            "Title",
            "SETTINGS",
            70f,
            Color.white,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 420f),
            new Vector2(700f, 100f)
        );

        CreateLabel(
            screen.transform,
            "DifficultyTitle",
            "DIFFICULTY",
            42f,
            new Color(1f, 0.9f, 0.7f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 260f),
            new Vector2(500f, 60f)
        );

        lowDifficultyButton = CreateSettingsButton(
            screen.transform,
            "LowButton",
            "LOW",
            new Vector2(0f, 120f)
        );

        mediumDifficultyButton = CreateSettingsButton(
            screen.transform,
            "MediumButton",
            "MEDIUM",
            new Vector2(0f, 0f)
        );

        hardDifficultyButton = CreateSettingsButton(
            screen.transform,
            "HardButton",
            "HARD",
            new Vector2(0f, -120f)
        );

        CreateLabel(
            screen.transform,
            "SoundTitle",
            "SOUND",
            42f,
            new Color(1f, 0.9f, 0.7f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -260f),
            new Vector2(500f, 60f)
        );

        soundToggle = CreateSoundToggle(screen.transform);

        settingsBackButton = CreateSettingsButton(
            screen.transform,
            "BackButton",
            "BACK",
            new Vector2(0f, -420f)
        );

        return screen;
    }

    void CacheSettingsRefs()
    {
        if (settingsScreen == null)
            return;

        if (lowDifficultyButton == null)
        {
            Transform t = settingsScreen.transform.Find("LowButton");
            if (t != null)
                lowDifficultyButton = t.GetComponent<Button>();
        }

        if (mediumDifficultyButton == null)
        {
            Transform t = settingsScreen.transform.Find("MediumButton");
            if (t != null)
                mediumDifficultyButton = t.GetComponent<Button>();
        }

        if (hardDifficultyButton == null)
        {
            Transform t = settingsScreen.transform.Find("HardButton");
            if (t != null)
                hardDifficultyButton = t.GetComponent<Button>();
        }

        // Always refresh toggle from hierarchy so UI edits don't break the link.
        Transform soundTransform = settingsScreen.transform.Find("SoundToggle");
        if (soundTransform != null)
            soundToggle = soundTransform.GetComponent<Toggle>();

        if (soundToggle == null)
            soundToggle = settingsScreen.GetComponentInChildren<Toggle>(true);

        if (settingsBackButton == null)
        {
            Transform t = settingsScreen.transform.Find("BackButton");
            if (t != null)
                settingsBackButton = t.GetComponent<Button>();
        }

        EnsureSoundToggleClickable();
    }

    void EnsureSoundToggleClickable()
    {
        if (soundToggle == null)
            return;

        RectTransform toggleRect = soundToggle.GetComponent<RectTransform>();
        if (toggleRect != null)
        {
            // Make the whole sound row easy to tap.
            toggleRect.sizeDelta = new Vector2(
                Mathf.Max(toggleRect.sizeDelta.x, 360f),
                Mathf.Max(toggleRect.sizeDelta.y, 80f)
            );
        }

        Image rootImage = soundToggle.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = soundToggle.gameObject.AddComponent<Image>();
            rootImage.color = new Color(1f, 1f, 1f, 0.01f);
        }

        rootImage.raycastTarget = true;

        if (soundToggle.targetGraphic == null)
            soundToggle.targetGraphic = rootImage;

        if (soundToggle.graphic == null)
        {
            Transform check = soundToggle.transform.Find("Background/Checkmark");
            if (check == null)
                check = soundToggle.transform.Find("Checkmark");

            if (check != null)
                soundToggle.graphic = check.GetComponent<Graphic>();
        }

        // Background should also receive clicks.
        Transform background = soundToggle.transform.Find("Background");
        if (background != null)
        {
            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null)
                bgImage.raycastTarget = true;

            RectTransform bgRect = background.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                bgRect.anchorMin = new Vector2(0f, 0.5f);
                bgRect.anchorMax = new Vector2(0f, 0.5f);
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.anchoredPosition = new Vector2(40f, 0f);
                bgRect.sizeDelta = new Vector2(56f, 56f);
            }
        }
    }

    Button CreateSettingsButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition
    )
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(parent, false);
        buttonObject.layer = 5;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(360f, 100f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = normalDifficultyColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;

        CreateLabel(
            buttonObject.transform,
            "Label",
            label,
            48f,
            new Color(0.15f, 0.12f, 0.1f, 1f)
        );

        return button;
    }

    Toggle CreateSoundToggle(Transform parent)
    {
        GameObject toggleObject = new GameObject(
            "SoundToggle",
            typeof(RectTransform),
            typeof(Toggle)
        );

        toggleObject.transform.SetParent(parent, false);
        toggleObject.layer = 5;

        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(0f, -340f);
        toggleRect.sizeDelta = new Vector2(360f, 80f);

        GameObject background = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        background.transform.SetParent(toggleObject.transform, false);
        background.layer = 5;

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(40f, 0f);
        bgRect.sizeDelta = new Vector2(56f, 56f);

        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.95f);

        GameObject checkmark = new GameObject(
            "Checkmark",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        checkmark.transform.SetParent(background.transform, false);
        checkmark.layer = 5;

        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        StretchFull(checkRect);
        checkRect.sizeDelta = new Vector2(-12f, -12f);

        Image checkImage = checkmark.GetComponent<Image>();
        checkImage.color = new Color(0.2f, 0.75f, 0.3f, 1f);

        CreateLabel(
            toggleObject.transform,
            "Label",
            "ON / OFF",
            40f,
            Color.white,
            new Vector2(0.5f, 0.5f),
            new Vector2(50f, 0f),
            new Vector2(240f, 70f)
        );

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;

        return toggle;
    }

    TextMeshProUGUI CreateLabel(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Color color,
        Vector2? anchor = null,
        Vector2? anchoredPosition = null,
        Vector2? size = null
    )
    {
        GameObject labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        labelObject.transform.SetParent(parent, false);
        labelObject.layer = 5;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();

        if (anchor.HasValue)
        {
            labelRect.anchorMin = anchor.Value;
            labelRect.anchorMax = anchor.Value;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = anchoredPosition ?? Vector2.zero;
            labelRect.sizeDelta = size ?? new Vector2(400f, 80f);
        }
        else
        {
            StretchFull(labelRect);
        }

        TextMeshProUGUI tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;

        if (uiFont != null)
            tmp.font = uiFont;

        return tmp;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    void StretchCharacterSelect()
    {
        if (characterSelectScreen == null)
            return;

        RectTransform rect = characterSelectScreen.GetComponent<RectTransform>();

        if (rect == null)
            return;

        StretchFull(rect);
    }

    static void SetScreen(GameObject screen, bool visible)
    {
        if (screen != null)
            screen.SetActive(visible);
    }

    static GameObject FindUi(string objectName)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].name == objectName)
                return allObjects[i];
        }

        return null;
    }

    static Button FindButton(string objectName)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].name != objectName)
                continue;

            Button button = allObjects[i].GetComponent<Button>();

            if (button != null)
                return button;
        }

        return null;
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
