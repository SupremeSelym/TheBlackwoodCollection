using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    ISession currentSession;
    IList<ISessionInfo> sessionList = new List<ISessionInfo>();
    string sessionName = "My Game";
    string joinCode = "";
    bool busy;

    public ISession CurrentSession => currentSession;

    void Awake() => DontDestroyOnLoad(gameObject);

    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // ---------- HOST ----------
    public async Task CreateSession(string name, int maxPlayers = 4, bool isPrivate = false)
    {
        var options = new SessionOptions
        {
            Name = name,
            MaxPlayers = maxPlayers,
            IsPrivate = isPrivate
        }.WithRelayNetwork();   // Relay + Netcode host startup handled for you

        currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log($"Session created. Join code: {currentSession.Code}");
    }

    // ---------- CLIENT ----------
    public async Task RefreshSessionList()
    {
        var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        sessionList = results.Sessions;
    }

    public async Task JoinById(string sessionId)
    {
        currentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
    }

    public async Task JoinByCode(string code)
    {
        currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
    }

    public async Task QuickJoin()
    {
        // Joins an open session, or creates one if none exists
        currentSession = await MultiplayerService.Instance.MatchmakeSessionAsync(
            new QuickJoinOptions { Timeout = System.TimeSpan.FromSeconds(10), CreateSession = true },
            new SessionOptions { MaxPlayers = 4 }.WithRelayNetwork());
    }

    // ---------- LEAVE ----------
    public async Task LeaveSession()
    {
        if (currentSession == null) return;

        try
        {
            if (currentSession.IsHost) await currentSession.AsHost().DeleteAsync();
            else await currentSession.LeaveAsync();
        }
        catch (System.Exception e) { Debug.LogWarning(e); }

        currentSession = null;
    } 

    async void OnApplicationQuit() => await LeaveSession();

    // ---------- TEST UI ----------
    async void OnGUI()
    {
        if (busy) return;
        if (currentSession != null) return;
        //if (currentSession != null)
        //{
        //    GUILayout.BeginArea(new Rect(10, 10, 350, 120));
        //    GUILayout.Label($"Session: {currentSession.Name}   Code: {currentSession.Code}");
        //    GUILayout.Label($"Players: {currentSession.PlayerCount}/{currentSession.MaxPlayers}");
        //    if (GUILayout.Button("Leave")) { busy = true; await LeaveSession(); busy = false; }
        //    GUILayout.EndArea();
        //    return;
        //}

        GUILayout.BeginArea(new Rect(10, 10, 350, 500));

        sessionName = GUILayout.TextField(sessionName, 20);
        if (GUILayout.Button("Create Session")) { busy = true; await CreateSession(sessionName); busy = false; }
        if (GUILayout.Button("Quick Join")) { busy = true; await QuickJoin(); busy = false; }

        joinCode = GUILayout.TextField(joinCode, 8);
        if (GUILayout.Button("Join by Code")) { busy = true; await JoinByCode(joinCode); busy = false; }

        if (GUILayout.Button("Refresh List")) { busy = true; await RefreshSessionList(); busy = false; }

        foreach (var s in sessionList)
        {
            int players = s.MaxPlayers - s.AvailableSlots;
            if (GUILayout.Button($"{s.Name}  ({players}/{s.MaxPlayers})"))
            { busy = true; await JoinById(s.Id); busy = false; }
        }

        GUILayout.EndArea();
    }
}