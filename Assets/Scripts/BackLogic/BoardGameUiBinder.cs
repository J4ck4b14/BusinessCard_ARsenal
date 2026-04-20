using TMPro;
using UnityEngine;

public sealed class BoardGameUiBinder : MonoBehaviour
{
    [Header("UI Roots")]
    [SerializeField] private GameObject idleUiRoot;
    [SerializeField] private GameObject countdownUiRoot;
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private GameObject waveClearUiRoot;
    [SerializeField] private GameObject upgradeUiRoot;
    [SerializeField] private GameObject gameOverUiRoot;

    [Header("Optional text")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;

    public void ApplyStateVisuals(BoardGameController.BoardGameState state)
    {
        SetActive(idleUiRoot, state == BoardGameController.BoardGameState.Idle);
        SetActive(countdownUiRoot, state == BoardGameController.BoardGameState.Countdown);

        var showGameplay =
            state == BoardGameController.BoardGameState.Countdown ||
            state == BoardGameController.BoardGameState.Playing ||
            state == BoardGameController.BoardGameState.WaveClear ||
            state == BoardGameController.BoardGameState.UpgradeChoice ||
            state == BoardGameController.BoardGameState.GameOver;

        SetActive(gameplayRoot, showGameplay);
        SetActive(waveClearUiRoot, state == BoardGameController.BoardGameState.WaveClear);
        SetActive(upgradeUiRoot, state == BoardGameController.BoardGameState.UpgradeChoice);
        SetActive(gameOverUiRoot, state == BoardGameController.BoardGameState.GameOver);

        if (countdownText != null && state != BoardGameController.BoardGameState.Countdown)
            countdownText.text = string.Empty;
    }

    public void UpdateHud(
        BoardGameController.BoardGameState state,
        int currentWaveIndex,
        int currentScore,
        int bestScore)
    {
        if (stateText != null)
            stateText.text = state.ToString();

        if (waveText != null)
            waveText.text = currentWaveIndex > 0 ? $"Wave {currentWaveIndex}" : "Wave -";

        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (bestScoreText != null)
            bestScoreText.text = $"Best: {bestScore}";
    }

    public void SetCountdownText(string value)
    {
        if (countdownText != null)
            countdownText.text = value;
    }

    public void ClearCountdownText()
    {
        if (countdownText != null)
            countdownText.text = string.Empty;
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }
}