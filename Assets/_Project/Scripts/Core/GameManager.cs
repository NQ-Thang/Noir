using System;
using UnityEngine;

public enum GameState
{
    Booting,
    Gameplay,
    Paused,
    GameOver
}

/// <summary>
/// GameManager là đối tượng trung tâm quản lý trạng thái của game.
/// Tồn tại trong Persistent Scene (Core_Scene) và sống xuyên suốt toàn bộ vòng đời ứng dụng.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<GameState> OnGameStateChanged;

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Booting;
    public GameState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;
        Debug.Log($"[GameManager] State changed from {previousState} to {newState}");

        switch (newState)
        {
            case GameState.Gameplay:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 1f;
                break;
        }

        OnGameStateChanged?.Invoke(currentState);
    }

    public void TogglePause()
    {
        if (currentState == GameState.Gameplay)
        {
            SetState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            SetState(GameState.Gameplay);
        }
    }
}
