using System.Collections;
using TMPro;
using UnityEngine;

public class BoardGameController : MonoBehaviour
{
    public enum BoardGameState
    {
        Idle,
        Countdown,
        Playing,
        WaveClear,
        UpgradeChoice,
        GameOver,
        GalleryOpen
    }

    [Header("References")]
    [SerializeField] private BackTrackedContentHandler backHandler;
    [SerializeField] private GameObject idleUiRoot;
    [SerializeField] private GameObject countdownUiRoot;
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private GameObject waveClearUiRoot;
    [SerializeField] private GameObject upgradeUiRoot;
    [SerializeField] private GameObject gameOverUiRoot;

    [Header("Optional text")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text stateText; // Game Over, Wave Clear, Upgrade Choice, etc.
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Flow")]
    [Tooltip("Time between the \"seconds\" in the countdown.")]
    [SerializeField] private float countdownStep = 0.7f;
    [SerializeField] private int firstWaveIndex = 1;
    [SerializeField] private int baseWaveScore = 100;

    [Header("Persistence")]
    [SerializeField] private ArsenalSaveData saveData;

    private Coroutine countdownCoroutine;
    private BoardGameState currentState;
    private int currentWaveIndex;
    private int currentScore;
    private int bestScore;

    public BoardGameState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public int CurrentScore => currentScore;
    public int BestScore => saveData.bestScore;

    public void Reset()
    {
        backHandler = GetComponentInParent<BackTrackedContentHandler>();
    }

    private void Awake()
    {
        if (backHandler == null)
            backHandler = GetComponentInParent<BackTrackedContentHandler>();

        saveData = ArsenalSaveSystem.LoadCurrent();
    }

    private void Start()
    {
        SetState(BoardGameState.Idle, force: true);
        UpdateAllUi();
    }

    private void OnDisable()
    {
        StopCountdown();
    }

    public void InsertCoin()
    {
        if (currentState != BoardGameState.Idle && currentState != BoardGameState.GameOver)
            return;

        BeginNewRun();
    }

    public void ToggleGallery()
    {
        if (currentState == BoardGameState.GalleryOpen)
            CloseGallery();
        else
            OpenGallery();
    }

    public void OpenGallery()
    {
        if (currentState == BoardGameState.Countdown || currentState == BoardGameState.Playing)
            return;

        StopCountdown();
        SetState(BoardGameState.GalleryOpen);
    }

    public void CloseGallery()
    {
        if (currentState != BoardGameState.GalleryOpen)
            return;

        SetState(BoardGameState.Idle);
    }

    public void BeginNewRun()
    {
        saveData.totalRuns++;
        ArsenalSaveSystem.SaveCurrent(saveData);

        StopCountdown();

        currentWaveIndex = 0;
        currentScore = 0;

        UpdateAllUi();
        SetState(BoardGameState.Countdown);

        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public void CompleteCurrentWave()
    {
        if (currentState != BoardGameState.Playing)
            return;

        currentScore += baseWaveScore * Mathf.Max(1, currentWaveIndex);
        SaveProgressIfNeeded();

        UpdateAllUi();
        SetState(BoardGameState.WaveClear);
    }

    public void ShowUpgradedChoice()
    {
        if (currentState != BoardGameState.WaveClear)
            return;

        SetState(BoardGameState.UpgradeChoice);
    }

    public void ApplyUpgradeAndContinue()
    {
        if (currentState != BoardGameState.WaveClear && currentState != BoardGameState.UpgradeChoice)
            return;

        StartWave(currentWaveIndex + 1);
    }

    public void EndRun()
    {
        StopCountdown();
        SaveProgressIfNeeded();
        ArsenalSaveSystem.SaveNew(saveData);
        UpdateAllUi();
        SetState(BoardGameState.GameOver);
    }

    public void ReturnToIdle()
    {
        StopCountdown();
        SetState(BoardGameState.Idle);
    }

    private IEnumerator CountdownCoroutine()
    {
        if (countdownText != null) countdownText.text = "3";
        yield return new WaitForSeconds(countdownStep);

        if (countdownText != null) countdownText.text = "2";
        yield return new WaitForSeconds(countdownStep);

        if (countdownText != null) countdownText.text = "1";
        yield return new WaitForSeconds(countdownStep);

        if (countdownText != null) countdownText.text = "GO!";
        yield return new WaitForSeconds(countdownStep * 0.75f);

        countdownCoroutine = null;
        StartWave(firstWaveIndex);
    }

    private void StartWave(int waveIndex)
    {
        StopCountdown();

        currentWaveIndex = waveIndex;
        SetState(BoardGameState.Playing);
        UpdateAllUi();

        // TODO:
        // Spawn player tank
        // Spawn wave enemies
        // Start round timer / wave logic here
    }

    private void SetState(BoardGameState newState, bool force = false)
    {
        if (!force && currentState == newState)
            return;

        currentState = newState;

        SyncBackHandler();
        ApplyStateVisuals();
        UpdateAllUi();
    }

    private void SyncBackHandler()
    {
        if (backHandler == null)
            return;

        switch (currentState)
        {
            case BoardGameState.Idle:
                backHandler.SetIdle();
                break;

            case BoardGameState.GalleryOpen:
                backHandler.OpenGallery();
                break;

            case BoardGameState.Countdown:
            case BoardGameState.Playing:
            case BoardGameState.WaveClear:
            case BoardGameState.UpgradeChoice:
            case BoardGameState.GameOver:
                backHandler.StartPlaying();
                break;
        }
    }

    private void ApplyStateVisuals()
    {
        SetActive(idleUiRoot, currentState == BoardGameState.Idle);
        SetActive(countdownUiRoot, currentState == BoardGameState.Countdown);

        bool showGameplay =
            currentState == BoardGameState.Playing ||
            currentState == BoardGameState.WaveClear ||
            currentState == BoardGameState.UpgradeChoice ||
            currentState == BoardGameState.GameOver;

        SetActive(gameplayRoot, showGameplay);
        SetActive(waveClearUiRoot, currentState == BoardGameState.WaveClear);
        SetActive(upgradeUiRoot, currentState == BoardGameState.UpgradeChoice);
        SetActive(gameOverUiRoot, currentState == BoardGameState.GameOver);
    }

    private void UpdateAllUi()
    {
        if (stateText != null)
            stateText.text = currentState.ToString();

        if (waveText != null)
            waveText.text = currentWaveIndex > 0 ? $"Wave {currentWaveIndex}" : "Wave -";

        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (bestScoreText != null)
            bestScoreText.text = $"Best: {saveData.bestScore}";

        if (countdownText != null && currentState != BoardGameState.Countdown)
            countdownText.text = string.Empty;
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (countdownText != null)
            countdownText.text = string.Empty;
    }

    private void SaveProgressIfNeeded()
    {
        bool changed = false;

        if (currentScore > saveData.bestScore)
        {
            saveData.bestScore = currentScore;
            changed = true;
        }

        if(currentWaveIndex > saveData.highestWaveReached)
        {
            saveData.highestWaveReached = currentWaveIndex;
            changed = true;
        }

        if (changed)
            ArsenalSaveSystem.SaveCurrent(saveData);
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }

}
