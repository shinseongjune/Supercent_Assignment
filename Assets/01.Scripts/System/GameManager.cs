using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public InputMode CurrentInputMode { get; private set; } = InputMode.Gameplay;

    public event Action<InputMode> OnInputModeChanged;

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

    public void SetInputMode(InputMode mode)
    {
        if (CurrentInputMode == mode)
            return;

        CurrentInputMode = mode;
        OnInputModeChanged?.Invoke(mode);
    }
}