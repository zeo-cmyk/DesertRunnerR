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

    [Header("Buttons")]
    public Button playButton;
    public Button characterButton;
    public Button selectButton;
    public Button quitButton;

    [Header("Loading")]
    public TMP_Text loadingText;
    public float loadingDuration = 3.5f;
    public string gameSceneName = "Basic_Learning";

    [Header("Character Previews")]
    [Tooltip("The 4 menu characters named 1, 2, 3, 4. Index 0 is character 1.")]
    public GameObject[] previewCharacters;

    int previewIndex;
    bool swipeArmed;
    float swipeStartX;
    float swipeStartY;
    bool isLoading;

    void Awake()
    {
        AutoBind();
        WireButtons();
    }

    void Start()
    {
        previewIndex = GameManager.GetSelectedPlayerIndex();
        HideAllPreviews();
        ShowMainMenu();
    }

    void Update()
    {
        if (isLoading || characterSelectScreen == null || !characterSelectScreen.activeSelf)
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

        if (playButton == null)
            playButton = FindButton("Play");

        if (characterButton == null)
            characterButton = FindButton("CharacterSelection");

        if (selectButton == null)
            selectButton = FindButton("Select");

        if (quitButton == null)
            quitButton = FindButton("Quit");

        if (loadingText == null && loadingScreen != null)
            loadingText = loadingScreen.GetComponentInChildren<TMP_Text>(true);

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
    }

    public void OpenCharacterSelect()
    {
        if (isLoading)
            return;

        previewIndex = ClampPreviewIndex(GameManager.GetSelectedPlayerIndex());

        SetScreen(mainScreen, false);
        SetScreen(loadingScreen, false);
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

    IEnumerator PlaySequence()
    {
        isLoading = true;

        HideAllPreviews();
        SetScreen(mainScreen, false);
        SetScreen(characterSelectScreen, false);
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

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 72f;
        tmp.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        tmp.raycastTarget = false;

        if (loadingText != null && loadingText.font != null)
            tmp.font = loadingText.font;
    }

    void StretchCharacterSelect()
    {
        if (characterSelectScreen == null)
            return;

        RectTransform rect = characterSelectScreen.GetComponent<RectTransform>();

        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
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
