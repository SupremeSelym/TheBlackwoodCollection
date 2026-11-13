using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyScreen : MonoBehaviour
{
    [SerializeField] SessionManager sessions;
    [SerializeField] string gameScene = "MU-Frame";   // must be in Build Settings

    const string NameKey = "name";
    const string ReadyKey = "ready";

    ISession session;
    string playerName;

    void Awake()
    {
        playerName = "Player" + Random.Range(100, 999);
    }
    bool busy;

    void Update()
    {
        // Notice when the SessionManager joins or leaves a session
        if (sessions.CurrentSession == session) return;

        if (session != null) session.Deleted -= OnSessionDeleted;
        session = sessions.CurrentSession;

        if (session != null)
        {
            session.Deleted += OnSessionDeleted;
            _ = InitLocalPlayer();
        }
    }

    async Task InitLocalPlayer()
    {
        session.CurrentPlayer.SetProperty(NameKey,
            new PlayerProperty(playerName, VisibilityPropertyOptions.Member));
        session.CurrentPlayer.SetProperty(ReadyKey,
            new PlayerProperty("0", VisibilityPropertyOptions.Member));
        await session.SaveCurrentPlayerDataAsync();
    }

    void OnSessionDeleted()
    {
        // Host closed the lobby
        session = null;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }

    // ---------- Helpers ----------
    static string GetProp(IReadOnlyPlayer p, string key, string fallback = "") =>
        p.Properties != null && p.Properties.TryGetValue(key, out var prop) ? prop.Value : fallback;

    bool AllReady()
    {
        foreach (var p in session.Players)
            if (GetProp(p, ReadyKey, "0") != "1") return false;
        return session.Players.Count > 1;   // remove this if you want to allow solo starts
    }

    async Task ToggleReady()
    {
        bool isReady = GetProp(session.CurrentPlayer, ReadyKey, "0") == "1";
        session.CurrentPlayer.SetProperty(ReadyKey,
            new PlayerProperty(isReady ? "0" : "1", VisibilityPropertyOptions.Member));
        await session.SaveCurrentPlayerDataAsync();
    }

    async Task StartGame()
    {
        // Lock the session so nobody joins mid-game
        var host = session.AsHost();
        host.IsLocked = true;
        await host.SavePropertiesAsync();

        // Only the server/host may load scenes through Netcode.
        // Every connected client loads the scene automatically.
        NetworkManager.Singleton.SceneManager.LoadScene(gameScene, LoadSceneMode.Single);
    }

    // ---------- UI ----------
    async void OnGUI()
    {
        if (session == null || busy) return;
        if (SceneManager.GetActiveScene().name == gameScene) return;   // hide in-game

        GUILayout.BeginArea(new Rect(10, 10, 380, 500));

        GUILayout.Label($"Lobby: {session.Name}");
        GUILayout.Label($"Join code: {session.Code}");
        GUILayout.Label($"Players: {session.Players.Count}/{session.MaxPlayers}");
        GUILayout.Space(8);

        foreach (var p in session.Players)
        {
            string name = GetProp(p, NameKey, "Connecting...");
            bool ready = GetProp(p, ReadyKey, "0") == "1";
            string tags = (p.Id == session.Host ? " [HOST]" : "") +
                          (p.Id == session.CurrentPlayer.Id ? " (you)" : "");
            GUILayout.Label($"{(ready ? "✔" : "…")}  {name}{tags}");
        }

        GUILayout.Space(8);

        bool localReady = GetProp(session.CurrentPlayer, ReadyKey, "0") == "1";
        if (GUILayout.Button(localReady ? "Unready" : "Ready"))
        { busy = true; await ToggleReady(); busy = false; }

        if (session.IsHost)
        {
            GUI.enabled = AllReady();
            if (GUILayout.Button("Start Game"))
            { busy = true; await StartGame(); busy = false; }
            GUI.enabled = true;

            if (!AllReady()) GUILayout.Label("Waiting for everyone to be ready...");
        }
        else
        {
            GUILayout.Label("Waiting for the host to start...");
        }

        if (GUILayout.Button("Leave"))
        { busy = true; await sessions.LeaveSession(); busy = false; }

        GUILayout.EndArea();
    }
}