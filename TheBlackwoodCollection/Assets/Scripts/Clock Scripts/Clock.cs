using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameClock : NetworkBehaviour
{
    [Header("References")]
    public TMP_Text clockText;

    [Header("Settings")]
    [Tooltip("In-game seconds that pass per real second.")]
    public float gameSecondsPerRealSecond = 12f;
    [Tooltip("Hour the clock stops at (24h format). 6 = 6:00 AM.")]
    public int endHour = 6;

    /// Fired once on each player's machine when the clock reaches the end time.
    public event Action OnClockFinished;

    public bool IsFinished { get; private set; }
    public float GameSeconds { get; private set; }

    // The server's network time at the moment the clock started.
    // Only the server can write it; everyone (including late joiners) can read it.
    private readonly NetworkVariable<double> startTime = new NetworkVariable<double>(-1.0);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            startTime.Value = NetworkManager.ServerTime.Time;
    }

    private void Start()
    {
        UpdateText(); // shows 12:00 AM until the network is running
    }

    private void Update()
    {
        if (IsFinished) return;
        if (!IsSpawned || startTime.Value < 0) return;

        double elapsedReal = NetworkManager.ServerTime.Time - startTime.Value;
        if (elapsedReal < 0) elapsedReal = 0;

        float endSeconds = endHour * 3600f;
        GameSeconds = (float)(elapsedReal * gameSecondsPerRealSecond);

        if (GameSeconds >= endSeconds)
        {
            GameSeconds = endSeconds;
            IsFinished = true;
            UpdateText();
            OnClockFinished?.Invoke();
            return;
        }

        UpdateText();
    }

    private void UpdateText()
    {
        int totalMinutes = (int)(GameSeconds / 60f);
        int hour24 = totalMinutes / 60;
        int minute = totalMinutes % 60;

        string suffix = hour24 < 12 ? "AM" : "PM";
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;

        clockText.text = $"{hour12}:{minute:00} {suffix}";
    }
}