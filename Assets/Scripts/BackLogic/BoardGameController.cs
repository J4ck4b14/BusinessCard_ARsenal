using System.Collections;
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
    [SerializeField] private BoardWorldController boardWorldController;
    [SerializeField] private PlayerTankController playerTankController;
    [SerializeField] private WaveDirector waveDirector;
    [SerializeField] private ShieldPlacementController shieldPlacementController;

    [Header("Optional helpers")]
    [SerializeField] private BoardGameUiBinder ui;
    [SerializeField] private BoardGameProgress progress;

    [Header("Flow")]
    [SerializeField] private float countdownStep = 0.7f;
    [SerializeField] private int firstWaveIndex = 1;
    [SerializeField] private int baseWaveScore = 100;

    private Coroutine countdownCoroutine;
    private BoardGameState currentState;
    private int currentWaveIndex;
    private int currentScore;

    public BoardGameState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public int CurrentScore => currentScore;
    public int BestScore => progress != null ? progress.BestScore : 0;

    private void Reset()
    {
        backHandler = GetComponentInParent<BackTrackedContentHandler>();
        boardWorldController = GetComponentInChildren<BoardWorldController>(true);

        ui = GetComponent<BoardGameUiBinder>();
        progress = GetComponent<BoardGameProgress>();
    }

    private void Awake()
    {
        if (backHandler == null)
            backHandler = GetComponentInParent<BackTrackedContentHandler>();

        if (boardWorldController == null)
            boardWorldController = GetComponentInChildren<BoardWorldController>(true);

        if (playerTankController == null)
            playerTankController = GetComponentInChildren<PlayerTankController>(true);

        if (waveDirector == null)
            waveDirector = GetComponentInChildren<WaveDirector>(true);

        if (ui == null)
            ui = GetComponent<BoardGameUiBinder>();

        if (progress == null)
            progress = GetComponent<BoardGameProgress>();

        progress?.EnsureLoaded();
    }

    private void Start()
    {
        SetState(BoardGameState.Idle, force: true);
    }

    private void OnDisable()
    {
        StopCountdown();
        if (boardWorldController != null)
            boardWorldController.SetSimulationActive(false);
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
        progress?.BeginRun();

        StopCountdown();

        currentWaveIndex = 0;
        currentScore = 0;

        boardWorldController?.BeginRun();
        playerTankController?.ResetForRun();
        shieldPlacementController?.ClearAll();
        waveDirector?.ClearWave();

        SetState(BoardGameState.Countdown);
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public void CompleteCurrentWave()
    {
        if (currentState != BoardGameState.Playing)
            return;

        currentScore += baseWaveScore * Mathf.Max(1, currentWaveIndex);
        progress?.UpdateRunResultsIfBetter(currentScore, currentWaveIndex);

        SetState(BoardGameState.WaveClear);
    }

    public void ShowUpgradeChoice()
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
        progress?.UpdateRunResultsIfBetter(currentScore, currentWaveIndex);

        SetState(BoardGameState.GameOver);

        waveDirector?.ClearWave();
        shieldPlacementController?.ClearAll();
    }

    public void ReturnToIdle()
    {
        StopCountdown();
        SetState(BoardGameState.Idle);

        waveDirector?.ClearWave();
        shieldPlacementController?.ClearAll();
    }

    private IEnumerator CountdownCoroutine()
    {
        ui?.SetCountdownText("3");
        yield return new WaitForSeconds(countdownStep);

        ui?.SetCountdownText("2");
        yield return new WaitForSeconds(countdownStep);

        ui?.SetCountdownText("1");
        yield return new WaitForSeconds(countdownStep);

        ui?.SetCountdownText("GO!");
        yield return new WaitForSeconds(countdownStep * 0.75f);

        countdownCoroutine = null;
        StartWave(firstWaveIndex);
    }

    private void StartWave(int waveIndex)
    {
        StopCountdown();

        currentWaveIndex = waveIndex;
        Debug.Log($"STARTING WAVE {currentWaveIndex}");

        SetState(BoardGameState.Playing);
        playerTankController?.ResetForRun();
        waveDirector?.StartWave(currentWaveIndex);
    }

    private void SetState(BoardGameState newState, bool force = false)
    {
        if (!force && currentState == newState)
            return;

        currentState = newState;

        // Fixed: compute from NEW state, do it once.
        if (boardWorldController != null)
            boardWorldController.SetSimulationActive(IsSimulationActive(currentState));

        SyncBackHandler();
        ui?.ApplyStateVisuals(currentState);
        ui?.UpdateHud(currentState, currentWaveIndex, currentScore, BestScore);
    }

    private static bool IsSimulationActive(BoardGameState state)
    {
        // Decide what "sim active" means once, in one place.
        return state == BoardGameState.Playing;
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

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        ui?.ClearCountdownText();
    }
}
