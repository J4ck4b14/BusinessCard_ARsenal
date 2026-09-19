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
    [SerializeField] private ChunkManager chunkManager;

    [Header("Optional helpers")]
    [SerializeField] private BoardGameUiBinder ui;
    [SerializeField] private BoardGameProgress progress;

    [Header("Flow")]
    [SerializeField] private float countdownStep = 0.7f;
    [SerializeField] private float waveClearDuration = 0.8f;
    [SerializeField] private int firstWaveIndex = 1;
    [SerializeField] private int baseWaveScore = 100;

    private Coroutine countdownCoroutine;
    private Coroutine waveTransitionCoroutine;
    private BoardGameState currentState;
    private int currentWaveIndex;
    private int currentScore;
    private bool started;

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

        if (shieldPlacementController == null)
            shieldPlacementController = GetComponentInChildren<ShieldPlacementController>(true);

        if (chunkManager == null)
            chunkManager = GetComponentInChildren<ChunkManager>(true);

        if (ui == null)
            ui = GetComponent<BoardGameUiBinder>();

        if (progress == null)
            progress = GetComponent<BoardGameProgress>();

        progress?.EnsureLoaded();
    }

    private void OnEnable()
    {
        if (!started)
            return;

        boardWorldController?.SetSimulationActive(IsSimulationActive(currentState));

        if (currentState == BoardGameState.Countdown && countdownCoroutine == null)
            countdownCoroutine = StartCoroutine(CountdownCoroutine());

        if (currentState == BoardGameState.WaveClear && waveTransitionCoroutine == null)
            waveTransitionCoroutine = StartCoroutine(WaveClearTransitionCoroutine());

        ui?.ApplyStateVisuals(currentState);
        ui?.UpdateHud(currentState, currentWaveIndex, currentScore, BestScore);
    }

    private void Start()
    {
        started = true;
        SetState(BoardGameState.Idle, force: true);
    }

    private void OnDisable()
    {
        StopCountdown();
        StopWaveTransition();
        boardWorldController?.SetSimulationActive(false);
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
        StopWaveTransition();
        SetState(BoardGameState.GalleryOpen);
    }

    public void CloseGallery()
    {
        if (currentState == BoardGameState.GalleryOpen)
            SetState(BoardGameState.Idle);
    }

    public void BeginNewRun()
    {
        progress?.BeginRun();

        StopCountdown();
        StopWaveTransition();

        currentWaveIndex = 0;
        currentScore = 0;

        boardWorldController?.BeginRun();
        playerTankController?.ResetForRun();
        shieldPlacementController?.ClearAll();
        waveDirector?.ClearWave();
        chunkManager?.RebuildForWave(firstWaveIndex);

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
        StopWaveTransition();
        waveTransitionCoroutine = StartCoroutine(WaveClearTransitionCoroutine());
    }

    public void ShowUpgradeChoice()
    {
        if (currentState == BoardGameState.WaveClear)
            SetState(BoardGameState.UpgradeChoice);
    }

    // Legacy/fallback button hook.
    public void ApplyUpgradeAndContinue()
    {
        if (currentState != BoardGameState.WaveClear && currentState != BoardGameState.UpgradeChoice)
            return;

        StartWave(currentWaveIndex + 1);
    }

    public void ApplyRapidFireUpgrade() => ApplyUpgrade(PlayerTankController.UpgradeType.RapidFire);
    public void ApplyHeavyShellsUpgrade() => ApplyUpgrade(PlayerTankController.UpgradeType.HeavyShells);
    public void ApplyMobilityUpgrade() => ApplyUpgrade(PlayerTankController.UpgradeType.Mobility);

    public void EndRun()
    {
        if (currentState == BoardGameState.GameOver)
            return;

        StopCountdown();
        StopWaveTransition();
        progress?.UpdateRunResultsIfBetter(currentScore, currentWaveIndex);

        SetState(BoardGameState.GameOver);

        waveDirector?.ClearWave();
        shieldPlacementController?.ClearAll();
    }

    public void ReturnToIdle()
    {
        StopCountdown();
        StopWaveTransition();
        SetState(BoardGameState.Idle);

        waveDirector?.ClearWave();
        shieldPlacementController?.ClearAll();
    }

    private void ApplyUpgrade(PlayerTankController.UpgradeType upgrade)
    {
        if (currentState != BoardGameState.UpgradeChoice)
            return;

        playerTankController?.ApplyUpgrade(upgrade);
        StartWave(currentWaveIndex + 1);
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

    private IEnumerator WaveClearTransitionCoroutine()
    {
        yield return new WaitForSeconds(waveClearDuration);
        waveTransitionCoroutine = null;

        if (currentState == BoardGameState.WaveClear)
            ShowUpgradeChoice();
    }

    private void StartWave(int waveIndex)
    {
        StopCountdown();
        StopWaveTransition();

        currentWaveIndex = waveIndex;
        Debug.Log($"STARTING WAVE {currentWaveIndex}");

        // Reconfigure the arena before every wave. The player keeps health and upgrades.
        chunkManager?.RebuildForWave(currentWaveIndex);

        SetState(BoardGameState.Playing);
        waveDirector?.StartWave(currentWaveIndex);
    }

    private void SetState(BoardGameState newState, bool force = false)
    {
        if (!force && currentState == newState)
            return;

        currentState = newState;

        boardWorldController?.SetSimulationActive(IsSimulationActive(currentState));
        SyncBackHandler();
        ui?.ApplyStateVisuals(currentState);
        ui?.UpdateHud(currentState, currentWaveIndex, currentScore, BestScore);
    }

    private static bool IsSimulationActive(BoardGameState state)
    {
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

    private void StopWaveTransition()
    {
        if (waveTransitionCoroutine == null)
            return;

        StopCoroutine(waveTransitionCoroutine);
        waveTransitionCoroutine = null;
    }
}
