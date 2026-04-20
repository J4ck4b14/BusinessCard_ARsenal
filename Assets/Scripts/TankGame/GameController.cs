using UnityEngine;

public class GameController : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    [SerializeField] private GameState currentState;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private GameObject[] enemyPrefabs;
    private GameObject player;

    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.MainMenu:
                // Handle main menu state
                break;
            case GameState.Playing:
                // Get player and start the game
                if(!FindFirstObjectByType<TankController>())
                {
                    player = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
                    Instantiate(enemyPrefabs[0], new Vector3(
                        Random.Range(playerSpawnPoint.position.x - 10f, playerSpawnPoint.position.x + 5f),
                        playerSpawnPoint.position.y,
                        Random.Range(playerSpawnPoint.position.z - 10f, playerSpawnPoint.position.z + 10f)), enemyPrefabs[0].transform.rotation);
                    foreach (Health health in FindObjectsByType<Health>(sortMode: FindObjectsSortMode.None))
                    {
                        health.StartRun();
                    }
                }
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                // Pause the game
                 Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                // Handle game over state

                break;
        }
    }

    private void OnGUI()
    {
        // Create buttons to change game states for testing
        if (GUILayout.Button("Main Menu"))
        {
            ChangeState(GameState.MainMenu);
        }
        if (GUILayout.Button("Play"))
        {
            ChangeState(GameState.Playing);
        }
        if (GUILayout.Button("Pause"))
        {
            if(Time.timeScale == 1f)
                ChangeState(GameState.Paused);
            else
                ChangeState(GameState.Playing);
        }
    }
}
