using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Linq;
using UnityEngine;

public class RelayConnection : MonoBehaviour
{
    public string JoinCode { get; private set; }
    string inputCode = "";

    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
    }

    // HOST: create an allocation, get a join code, start hosting
    public async Task StartHostWithRelay(int maxPlayers = 4)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var endpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.ConnectionData,   // host passes its own connection data twice
            true);                       // isSecure (dtls)

        NetworkManager.Singleton.StartHost();
        Debug.Log("Join code: " + JoinCode);
    }

    public async Task StartClientWithRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var endpoint = joinAllocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            endpoint.Host,
            (ushort)endpoint.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            true);

        NetworkManager.Singleton.StartClient();
    }
    // Minimal test UI
    async void OnGUI()
    {
        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            if (!string.IsNullOrEmpty(JoinCode))
                GUI.Label(new Rect(10, 10, 300, 30), "Join code: " + JoinCode);
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));

        if (GUILayout.Button("Host (Relay)"))
            await StartHostWithRelay();

        inputCode = GUILayout.TextField(inputCode, 6);
        if (GUILayout.Button("Join (Relay)"))
            await StartClientWithRelay(inputCode);

        GUILayout.EndArea();
    }
}