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

    [Tooltip("Assign your 3 different Environment Patch prefabs.")]
    public GameObject[] envPatchPrefabs = new GameObject[3];

    [Tooltip("Assign your PLAYER PREFAB here. Do NOT assign the player from the scene.")]
    public GameObject playerPrefab;

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

    [Tooltip("TMP text that displays the final coin score.")]
    public TMP_Text gameOverScoreText;

    [Tooltip("Your own Coins HUD TMP text from the Canvas.")]
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


    // =========================================================
    // INTERNAL VARIABLES
    // =========================================================

    readonly List<EnvironmentPatch> patches =
        new List<EnvironmentPatch>();


    // This stores the spawn point ONLY from the FIRST patch.
    Transform firstPatchSpawnPoint;


    float farthestPatchZ;

    int coins;

    bool gameStarted;

    bool isGameOver;

    TMP_FontAsset uiFont;


    // =========================================================
    // AWAKE
    // =========================================================

    void Awake()
    {
        Instance = this;


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
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

        if (playerPrefab == null)
        {
            Debug.LogError(
                "GameManager: Player Prefab is NOT assigned."
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
                playerPrefab,
                firstPatchSpawnPoint.position,
                firstPatchSpawnPoint.rotation
            );


        // Give the instantiated player a clear name.
        playerObject.name =
            playerPrefab.name;


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
        // CREATE 3 PATCHES
        // -----------------------------------------------------

        for (int i = 0; i < 3; i++)
        {
            GameObject prefab =
                GetPatchPrefab(i);


            if (prefab == null)
            {
                Debug.LogError(
                    "GameManager: Environment Patch Prefab slot " +
                    i +
                    " is empty."
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

        RefreshHud();
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
        // HUD
        // -----------------------------------------------------

        if (hudText == null)
        {
            Transform found =
                canvas.transform.Find(
                    "HudCoins"
                );


            if (found != null)
            {
                hudText =
                    found.GetComponent<TMP_Text>();
            }
        }


        if (hudText == null)
        {
            Debug.LogWarning(
                "GameManager: HUD Text is not assigned. " +
                "Drag your Coins TMP text into the Hud Text field."
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


        if (startScreen != null)
        {
            startScreen.SetActive(true);
        }


        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }


        if (hudText != null)
        {
            hudText.text =
                "Coins  0";

            hudText.gameObject.SetActive(false);
        }
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


        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }


        if (hudText != null)
        {
            hudText.gameObject.SetActive(true);

            hudText.text =
                "Coins  " + coins;
        }


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
                "Coins  " + coins;
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

        if (hudText != null)
        {
            hudText.gameObject.SetActive(false);
        }


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

    void RefreshHud()
    {
        if (hudText != null)
        {
            hudText.text =
                "Coins  " + coins;
        }
    }
}
