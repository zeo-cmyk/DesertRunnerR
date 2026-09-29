
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    // =========================================================
    // PREFABS
    // =========================================================

    [Header("Prefabs")]

    [Tooltip("Assign your Environment Patch prefabs. Index 0 = Environment 0, Index 1 = Environment 1, etc.")]
    public GameObject[] envPatchPrefabs = new GameObject[3];

    [Tooltip("Assign your PLAYER PREFAB here. Do NOT assign the player from the scene. Used if Player Prefabs is empty.")]
    public GameObject playerPrefab;

    [Tooltip("Character prefabs matching the Main Menu previews. Index 0 = character 1, index 1 = character 2, etc.")]
    public GameObject[] playerPrefabs;

    public const string SelectedPlayerPrefKey = "SelectedPlayerIndex";

    // PlayerPrefs key for environment selection.
    // 0 = envPatchPrefabs[0]
    // 1 = envPatchPrefabs[1]
    public const string SelectedEnvironmentPrefKey = "SelectedEnvironmentIndex";

    [Tooltip("Edit this prefab, then place copies inside each Env Patch.")]
    public GameObject coinPrefab;

    [Tooltip("Edit this prefab, then place copies inside each Env Patch.")]
    public GameObject obstaclePrefab;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [Tooltip("The player that was instantiated from Player Prefab.")]
    public PlayerRunner player;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]

    [Tooltip("Your own Start Screen GameObject from the Canvas.")]
    public GameObject startScreen;

    [Tooltip("Your Game Over Screen GameObject from the Canvas.")]
    public GameObject gameOverScreen;

    [Tooltip("TMP text that displays the final score on Game Over.")]
    public TMP_Text gameOverScoreText;

    [Tooltip("HUD Score text (increases while running).")]
    public TMP_Text hudScoreText;

    [Tooltip("HUD Coins text (increases when picking coins).")]
    public TMP_Text hudCoinsText;

    [HideInInspector]
    public TMP_Text hudText;


    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio")]

    public AudioSource SFX;

    public AudioClip pickCoin;

    public AudioClip GameoverClip;


    // =========================================================
    // TRACK
    // =========================================================

    [Header("Track")]

    [Tooltip("Forward length of each environment patch.")]
    public float patchLength = 40f;

    [Tooltip("Ground Y position.")]
    public float groundY = 1.375f;

    [Header("Score")]
    [Tooltip("Score points gained per meter of forward running.")]
    public float scorePerMeter = 1f;


    // =========================================================
    // INTERNAL VARIABLES
    // =========================================================

    readonly List<EnvironmentPatch> patches =
        new List<EnvironmentPatch>();

    // This stores the spawn point ONLY from the FIRST patch.
    Transform firstPatchSpawnPoint;

    float farthestPatchZ;

    int coins;

    int score;

    float runStartZ;

    bool gameStarted;

    bool isGameOver;

    TMP_FontAsset uiFont;

    // The environment selected from PlayerPrefs.
    int selectedEnvironmentIndex;


    // =========================================================
    // AWAKE
    // =========================================================

    void Awake()
    {
        Instance = this;

        ApplyMobileRuntimeSettings();
        GameSettings.ApplySound();

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        // -----------------------------------------------------
        // LOAD ENVIRONMENT FROM PLAYERPREFS
        // -----------------------------------------------------

        selectedEnvironmentIndex =
            GetSelectedEnvironmentIndex();

        Debug.Log(
            "GameManager: Selected Environment Index = " +
            selectedEnvironmentIndex
        );
    }


    static void ApplyMobileRuntimeSettings()
    {
        QualitySettings.SetQualityLevel(0, true);
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        CacheUiFont();

        DisableSceneLeftovers();

        // First create the environment patches.
        BuildTrack();

        // Then create the player at the first patch SpawnPoint.
        SpawnPlayer();

        SetupUi();

        ShowStartScreen();
    }


    // =========================================================
    // DISABLE OLD SCENE OBJECTS
    // =========================================================

    void DisableSceneLeftovers()
    {
        HideByName("Ground");

        HideByName("Hurdle");
    }


    static void HideByName(string objectName)
    {
        GameObject found =
            GameObject.Find(objectName);

        if (found != null)
        {
            found.SetActive(false);
        }
    }


    // =========================================================
    // CACHE UI FONT
    // =========================================================

    void CacheUiFont()
    {
        TMP_Text[] texts =
            FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        if (texts.Length > 0)
        {
            uiFont =
                texts[0].font;
        }

        if (uiFont == null)
        {
            uiFont =
                TMP_Settings.defaultFontAsset;
        }
    }


    // =========================================================
    // SPAWN PLAYER
    // =========================================================

    void SpawnPlayer()
    {
        // -----------------------------------------------------
        // CHECK PLAYER PREFAB
        // -----------------------------------------------------

        GameObject prefabToSpawn = GetSelectedPlayerPrefab();

        if (prefabToSpawn == null)
        {
            Debug.LogError(
                "GameManager: No player prefab found for the selected character."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK FIRST PATCH
        // -----------------------------------------------------

        if (patches.Count == 0)
        {
            Debug.LogError(
                "GameManager: No Environment Patches were created."
            );

            return;
        }


        // -----------------------------------------------------
        // GET ONLY THE FIRST PATCH
        // -----------------------------------------------------

        EnvironmentPatch firstPatch =
            patches[0];

        if (firstPatch == null)
        {
            Debug.LogError(
                "GameManager: First Environment Patch is null."
            );

            return;
        }


        // -----------------------------------------------------
        // GET SPAWN POINT FROM FIRST PATCH
        // -----------------------------------------------------

        firstPatchSpawnPoint =
            firstPatch.GetSpawnPoint();

        if (firstPatchSpawnPoint == null)
        {
            Debug.LogError(
                "GameManager: SpawnPoint is missing from the FIRST " +
                "EnvironmentPatch prefab."
            );

            return;
        }


        // -----------------------------------------------------
        // INSTANTIATE PLAYER
        // -----------------------------------------------------

        GameObject playerObject =
            Instantiate(
                prefabToSpawn,
                firstPatchSpawnPoint.position,
                firstPatchSpawnPoint.rotation
            );

        // Give the instantiated player a clear name.
        playerObject.name =
            prefabToSpawn.name;


        // -----------------------------------------------------
        // GET PLAYER RUNNER
        // -----------------------------------------------------

        player =
            playerObject.GetComponent<PlayerRunner>();

        if (player == null)
        {
            player =
                playerObject.GetComponentInChildren<PlayerRunner>();
        }

        if (player == null)
        {
            Debug.LogError(
                "GameManager: Player Prefab does not contain " +
                "a PlayerRunner component."
            );

            Destroy(playerObject);

            return;
        }


        // -----------------------------------------------------
        // RESET RIGIDBODY
        // -----------------------------------------------------

        Rigidbody rb =
            playerObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }


        // -----------------------------------------------------
        // LOG
        // -----------------------------------------------------

        GameSettings.ApplyDifficultyToPlayer(player);
        GameSettings.ApplySound();

        Debug.Log(
            "GameManager: Player instantiated at FIRST patch SpawnPoint."
        );

        Debug.Log(
            "Player position: " +
            playerObject.transform.position
        );
    }


    // =========================================================
    // BUILD TRACK
    // =========================================================

    void BuildTrack()
    {
        patches.Clear();

        firstPatchSpawnPoint = null;


        if (
            envPatchPrefabs == null ||
            envPatchPrefabs.Length == 0
        )
        {
            Debug.LogError(
                "GameManager: Assign your Environment Patch Prefabs."
            );

            return;
        }


        // -----------------------------------------------------
        // LOAD ENVIRONMENT INDEX FROM PLAYERPREFS
        // -----------------------------------------------------

        selectedEnvironmentIndex =
            GetSelectedEnvironmentIndex();


        // -----------------------------------------------------
        // GET SELECTED ENVIRONMENT PREFAB
        // -----------------------------------------------------

        GameObject selectedEnvironmentPrefab =
            GetSelectedEnvironmentPrefab();

        if (selectedEnvironmentPrefab == null)
        {
            Debug.LogError(
                "GameManager: No valid Environment Patch prefab " +
                "found for Environment Index " +
                selectedEnvironmentIndex
            );

            return;
        }


        Debug.Log(
            "GameManager: Loading Environment Patch Index " +
            selectedEnvironmentIndex +
            " -> " +
            selectedEnvironmentPrefab.name
        );


        // -----------------------------------------------------
        // CREATE 3 PATCHES
        // -----------------------------------------------------

        for (int i = 0; i < 3; i++)
        {
            // IMPORTANT:
            // Every patch uses the environment selected by PlayerPrefs.
            //
            // PlayerPrefs = 0
            //     -> envPatchPrefabs[0]
            //
            // PlayerPrefs = 1
            //     -> envPatchPrefabs[1]

            GameObject prefab =
                selectedEnvironmentPrefab;


            if (prefab == null)
            {
                Debug.LogError(
                    "GameManager: Selected Environment Patch Prefab " +
                    "is empty."
                );

                continue;
            }


            // -------------------------------------------------
            // INSTANTIATE PATCH
            // -------------------------------------------------

            GameObject instance =
                Instantiate(prefab);

            instance.name =
                prefab.name;


            // -------------------------------------------------
            // GET ENVIRONMENT PATCH COMPONENT
            // -------------------------------------------------

            EnvironmentPatch patch =
                instance.GetComponent<EnvironmentPatch>();

            if (patch == null)
            {
                patch =
                    instance.AddComponent<EnvironmentPatch>();
            }


            // -------------------------------------------------
            // GET PATCH LENGTH
            // -------------------------------------------------

            if (
                i == 0 &&
                patch.length > 0.01f
            )
            {
                patchLength =
                    patch.length;
            }


            // -------------------------------------------------
            // CALCULATE Z POSITION
            // -------------------------------------------------

            float z =
                i * patchLength +
                patchLength * 0.5f;


            // -------------------------------------------------
            // PLACE PATCH
            // -------------------------------------------------

            PlacePatch(
                patch,
                z
            );


            // -------------------------------------------------
            // ADD TO LIST
            // -------------------------------------------------

            patches.Add(patch);


            // -------------------------------------------------
            // FIRST PATCH ONLY
            // -------------------------------------------------

            if (i == 0)
            {
                if (patch.spawnPoint == null)
                {
                    Debug.LogError(
                        "GameManager: FIRST EnvironmentPatch " +
                        "does not have a SpawnPoint assigned."
                    );
                }
                else
                {
                    // IMPORTANT:
                    // We save this ONLY ONCE.
                    // SpawnPoints from patch 2 and patch 3
                    // are completely ignored.

                    firstPatchSpawnPoint =
                        patch.spawnPoint;

                    Debug.Log(
                        "GameManager: FIRST patch SpawnPoint found."
                    );

                    Debug.Log(
                        "First SpawnPoint world position: " +
                        firstPatchSpawnPoint.position
                    );
                }
            }
        }


        // -----------------------------------------------------
        // SET FARTHEST PATCH
        // -----------------------------------------------------

        if (patches.Count > 0)
        {
            farthestPatchZ =
                (patches.Count - 1) *
                patchLength +
                patchLength * 0.5f;
        }
    }


    // =========================================================
    // SELECTED PLAYER
    // =========================================================

    public static int GetSelectedPlayerIndex()
    {
        return PlayerPrefs.GetInt(
            SelectedPlayerPrefKey,
            0
        );
    }


    public static void SetSelectedPlayerIndex(int index)
    {
        PlayerPrefs.SetInt(
            SelectedPlayerPrefKey,
            Mathf.Max(0, index)
        );

        PlayerPrefs.Save();
    }


    GameObject GetSelectedPlayerPrefab()
    {
        int selectedIndex =
            GetSelectedPlayerIndex();

        if (
            playerPrefabs != null &&
            playerPrefabs.Length > 0
        )
        {
            selectedIndex =
                Mathf.Clamp(
                    selectedIndex,
                    0,
                    playerPrefabs.Length - 1
                );

            if (playerPrefabs[selectedIndex] != null)
            {
                return playerPrefabs[selectedIndex];
            }

            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                if (playerPrefabs[i] != null)
                {
                    return playerPrefabs[i];
                }
            }
        }

        return playerPrefab;
    }


    // =========================================================
    // SELECTED ENVIRONMENT
    // =========================================================

    public static int GetSelectedEnvironmentIndex()
    {
        int index =
            PlayerPrefs.GetInt(
                SelectedEnvironmentPrefKey,
                0
            );

        return Mathf.Max(0, index);
    }


    public static void SetSelectedEnvironmentIndex(int index)
    {
        PlayerPrefs.SetInt(
            SelectedEnvironmentPrefKey,
            Mathf.Max(0, index)
        );

        PlayerPrefs.Save();
    }


    GameObject GetSelectedEnvironmentPrefab()
    {
        if (
            envPatchPrefabs == null ||
            envPatchPrefabs.Length == 0
        )
        {
            return null;
        }


        int index =
            GetSelectedEnvironmentIndex();


        // Clamp the PlayerPrefs value to the available
        // Environment Patch array.

        index =
            Mathf.Clamp(
                index,
                0,
                envPatchPrefabs.Length - 1
            );


        selectedEnvironmentIndex =
            index;


        if (envPatchPrefabs[index] != null)
        {
            return envPatchPrefabs[index];
        }


        // -----------------------------------------------------
        // FALLBACK IF SELECTED SLOT IS EMPTY
        // -----------------------------------------------------

        for (int i = 0; i < envPatchPrefabs.Length; i++)
        {
            if (envPatchPrefabs[i] != null)
            {
                Debug.LogWarning(
                    "GameManager: Environment Patch index " +
                    index +
                    " is empty. Using index " +
                    i +
                    " instead."
                );

                selectedEnvironmentIndex =
                    i;

                return envPatchPrefabs[i];
            }
        }


        return null;
    }


    // =========================================================
    // GET PATCH PREFAB
    // =========================================================

    GameObject GetPatchPrefab(int index)
    {
        if (
            envPatchPrefabs == null ||
            envPatchPrefabs.Length == 0
        )
        {
            return null;
        }


        // -----------------------------------------------------
        // USE REQUESTED SLOT
        // -----------------------------------------------------

        if (
            index < envPatchPrefabs.Length &&
            envPatchPrefabs[index] != null
        )
        {
            return envPatchPrefabs[index];
        }


        // -----------------------------------------------------
        // FALLBACK TO LAST AVAILABLE PREFAB
        // -----------------------------------------------------

        for (
            int i = envPatchPrefabs.Length - 1;
            i >= 0;
            i--
        )
        {
            if (envPatchPrefabs[i] != null)
            {
                return envPatchPrefabs[i];
            }
        }


        return null;
    }


    // =========================================================
    // PLACE PATCH
    // =========================================================

    void PlacePatch(
        EnvironmentPatch patch,
        float z
    )
    {
        patch.transform.SetPositionAndRotation(
            new Vector3(
                0f,
                0f,
                z
            ),
            Quaternion.identity
        );

        patch.ResetContents();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (
            !gameStarted ||
            isGameOver ||
            player == null
        )
        {
            return;
        }


        RecyclePatches();

        UpdateRunScore();

        RefreshHud();
    }


    // =========================================================
    // RUN SCORE
    // =========================================================

    void UpdateRunScore()
    {
        if (player == null)
            return;

        float distance =
            Mathf.Max(
                0f,
                player.transform.position.z - runStartZ
            );

        score =
            Mathf.FloorToInt(
                distance * scorePerMeter
            );
    }


    // =========================================================
    // RECYCLE PATCHES
    // =========================================================

    void RecyclePatches()
    {
        float recycleLine =
            player.transform.position.z -
            patchLength * 0.6f;


        for (
            int i = 0;
            i < patches.Count;
            i++
        )
        {
            EnvironmentPatch patch =
                patches[i];


            if (patch == null)
                continue;


            float length =
                patch.length > 0.01f
                    ? patch.length
                    : patchLength;


            // -------------------------------------------------
            // PATCH STILL IN FRONT OF PLAYER
            // -------------------------------------------------

            if (
                patch.transform.position.z +
                length * 0.5f >
                recycleLine
            )
            {
                continue;
            }


            // -------------------------------------------------
            // MOVE PATCH TO FRONT
            // -------------------------------------------------

            farthestPatchZ +=
                length;


            PlacePatch(
                patch,
                farthestPatchZ
            );
        }
    }


    // =========================================================
    // UI SETUP
    // =========================================================

    void SetupUi()
    {
        Canvas canvas =
            FindFirstObjectByType<Canvas>();


        if (canvas == null)
        {
            Debug.LogError(
                "GameManager: Canvas not found."
            );

            return;
        }


        // -----------------------------------------------------
        // START SCREEN
        // -----------------------------------------------------

        if (startScreen == null)
        {
            Debug.LogError(
                "GameManager: Please drag your StartScreen " +
                "GameObject into the Start Screen field."
            );
        }


        // -----------------------------------------------------
        // GAME OVER SCREEN
        // -----------------------------------------------------

        if (gameOverScreen == null)
        {
            Transform found =
                canvas.transform.Find(
                    "GameOverScreen"
                );

            if (found != null)
            {
                gameOverScreen =
                    found.gameObject;
            }
        }


        // -----------------------------------------------------
        // GAME OVER SCORE TEXT
        // -----------------------------------------------------

        if (
            gameOverScoreText == null &&
            gameOverScreen != null
        )
        {
            TMP_Text[] texts =
                gameOverScreen.GetComponentsInChildren<TMP_Text>(
                    true
                );


            foreach (TMP_Text text in texts)
            {
                if (
                    text.gameObject.name.Contains("Text") ||
                    text.gameObject.name.Contains("Score") ||
                    text.gameObject.name.Contains("Coins")
                )
                {
                    gameOverScoreText =
                        text;

                    break;
                }
            }
        }


        // -----------------------------------------------------
        // HUD SCORE + COINS
        // -----------------------------------------------------

        if (hudScoreText == null)
        {
            Transform found =
                canvas.transform.Find("Score");

            if (found != null)
            {
                hudScoreText =
                    found.GetComponent<TMP_Text>();
            }
        }


        if (hudCoinsText == null)
        {
            Transform found =
                canvas.transform.Find("Coins");

            if (found != null)
            {
                hudCoinsText =
                    found.GetComponent<TMP_Text>();
            }
        }


        // Legacy field fallback
        if (hudCoinsText == null && hudText != null)
            hudCoinsText = hudText;

        if (hudScoreText == null && hudText != null)
            hudScoreText = hudText;


        if (
            hudScoreText == null ||
            hudCoinsText == null
        )
        {
            Debug.LogWarning(
                "GameManager: Assign HUD Score and Coins TMP texts."
            );
        }
    }


    // =========================================================
    // START SCREEN
    // =========================================================

    void ShowStartScreen()
    {
        gameStarted = false;

        isGameOver = false;

        coins = 0;

        score = 0;

        runStartZ = 0f;


        if (startScreen != null)
        {
            startScreen.SetActive(true);
        }


        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }


        SetHudVisible(false);

        RefreshHud();
    }


    // =========================================================
    // START RUN
    // =========================================================

    public void StartRun()
    {
        if (
            gameStarted ||
            isGameOver
        )
        {
            return;
        }


        gameStarted = true;

        score = 0;
        coins = 0;


        if (player != null)
        {
            runStartZ =
                player.transform.position.z;
        }


        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }


        SetHudVisible(true);

        RefreshHud();


        if (player != null)
        {
            player.BeginRun();
        }
        else
        {
            Debug.LogError(
                "GameManager: Player was not instantiated."
            );
        }
    }


    // =========================================================
    // COIN
    // =========================================================

    public void CollectCoin(Coin coin)
    {
        if (
            coin == null ||
            isGameOver
        )
        {
            return;
        }


        coins +=
            coin.value;


        if (
            SFX != null &&
            pickCoin != null
        )
        {
            SFX.PlayOneShot(
                pickCoin
            );
        }


        RefreshHud();


        if (coin != null)
        {
            coin.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // POP THEN HIDE
    // =========================================================

    IEnumerator PopThenHide(
        GameObject coin
    )
    {
        Vector3 start =
            coin.transform.localScale;


        float t = 0f;


        while (t < 0.18f)
        {
            t +=
                Time.deltaTime;


            if (coin != null)
            {
                coin.transform.localScale =
                    start *
                    (1f + t * 4f);
            }


            yield return null;
        }
    }


    // =========================================================
    // GAME OVER
    // =========================================================

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }


        isGameOver = true;


        StartCoroutine(
            ShowGameOverAfterDelay()
        );
    }


    IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSeconds(1f);


        // -----------------------------------------------------
        // GAME OVER SCORE
        // -----------------------------------------------------

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text =
                "Score  " + score +
                "\nCoins  " + coins;
        }


        // -----------------------------------------------------
        // SHOW GAME OVER SCREEN
        // -----------------------------------------------------

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }


        // -----------------------------------------------------
        // GAME OVER SOUND
        // -----------------------------------------------------

        if (
            SFX != null &&
            GameoverClip != null
        )
        {
            SFX.PlayOneShot(
                GameoverClip
            );
        }


        // -----------------------------------------------------
        // HIDE HUD
        // -----------------------------------------------------

        SetHudVisible(false);


        // -----------------------------------------------------
        // UNLOCK CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }


    // =========================================================
    // RESTART
    // =========================================================

    public void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }


    // =========================================================
    // HUD
    // =========================================================

    void SetHudVisible(bool visible)
    {
        if (hudScoreText != null)
            hudScoreText.gameObject.SetActive(visible);

        if (hudCoinsText != null)
            hudCoinsText.gameObject.SetActive(visible);

        if (
            hudText != null &&
            hudText != hudScoreText &&
            hudText != hudCoinsText
        )
        {
            hudText.gameObject.SetActive(visible);
        }
    }


    void RefreshHud()
    {
        if (hudScoreText != null)
        {
            hudScoreText.text =
                "Score  " + score;
        }

        if (hudCoinsText != null)
        {
            hudCoinsText.text =
                "Coins  " + coins;
        }
        else if (hudText != null)
        {
            hudText.text =
                "Coins  " + coins;
        }
    }


    // =========================================================
    // EXIT
    // =========================================================

    public void ExitGame()
    {
        Application.Quit();
    }


    // =========================================================
    // PAUSE
    // =========================================================

    public GameObject pauseMenu;

    public void PausedGame()
    {
        Time.timeScale = 0;

        if (pauseMenu != null)
            pauseMenu.SetActive(true);
    }


    public void ResumeGame()
    {
        Time.timeScale = 1;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }
}

