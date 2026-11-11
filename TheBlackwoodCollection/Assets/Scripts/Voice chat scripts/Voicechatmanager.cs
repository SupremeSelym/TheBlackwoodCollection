using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine.InputSystem;


public class Voicechatmanager : MonoBehaviour
{
   
    public static Voicechatmanager Instance { get; private set; }

    [Header("Proximity (3D)")]
    [Tooltip("Distance (in Unity units) where voices become inaudible.")]
    [SerializeField] private int audibleDistance = 32;
    [Tooltip("Distance within which voices are at full volume")]
    [SerializeField] private int conversationalDistance = 2;
    [SerializeField, Range(0.1f, 5f)] private float fadeIntensity = 1f;

    [Header("Radio")]
    [Tooltip("While Holding the radio key, people standing near you also hear you.")]
    [SerializeField] private bool alsoHeardNearby = true;

    [Header("Session")]
    [Tooltip("Join voice automatically when this player connects with Netcode.")]
    [SerializeField] private bool autoJoinOnConnect = true;
    [Tooltip("Everyone using the same ID shares voice channels. Replace with your Relay join code or lobby vode for real use.")]
    [SerializeField] private string defaultSessionID = "default";


    [Header("Testing")]
    [Tooltip("Give the player a radio as soon as they join, so you can test without an inventory system.")]
    [SerializeField] private bool equipRadioOnJoinForTesting = false;
    [SerializeField] private int testFrequency = 1;

    private static bool vivoxInitialized;

    private string sessionID;
    private string proximityChannel;
    private string radioChannel;
    private Transform localHead;

    private bool inSession;
    private bool joining;
    private bool hasRadio;
    private bool radioHeld;

    private bool applying;
    private bool dirty;

    public bool HasRadio => hasRadio;
    public bool InSession => inSession;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!autoJoinOnConnect) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;


        //Player may be connected already, connected to menu, then loaded this scene
        if (nm.IsConnectedClient)
        {
            _ = JoinSessionAsync(defaultSessionID);

        }

        else {
            nm.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {

        var nm = NetworkManager.Singleton;

        if(nm != null && clientId == nm.LocalClientId)
        {
            _ = JoinSessionAsync(defaultSessionID);
        }
    }


    //For joining / Leaving

    public async Task JoinSessionAsync(string id)
    {

        if (inSession || joining) return;
        joining = true;

        try
        {
            sessionID = id;

            if(UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (!vivoxInitialized)
            {
                await VivoxService.Instance.InitializeAsync();
                vivoxInitialized = true;
            }

            if (!VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.LoginAsync();
            }

            proximityChannel = $"prox_{sessionID}";
            var props = new Channel3DProperties(audibleDistance, conversationalDistance, 
                fadeIntensity, AudioFadeModel.InverseByDistance);

            await VivoxService.Instance.JoinPositionalChannelAsync(proximityChannel, ChatCapability.AudioOnly, props);

            inSession = true;
            RequestApplyTransmission();

            if (equipRadioOnJoinForTesting)
            {
                await EquipRadioAsync(testFrequency);
            }

            Debug.Log($"[Voice] Joined proximity channel {proximityChannel}");
        }
        
        catch (Exception e)
        {
            Debug.LogError($"[Voice] Failed to join voice: {e}");
        }

        finally
        {

            joining = false;
        }
    }

    public async Task LeaveVoiceAsync()
    {
        if (!inSession) return;

        inSession = false;
        hasRadio = false;
        radioHeld = false;

        try
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();
        }

        catch(Exception e)
        {

            Debug.LogWarning($"[Voice] Error leaving channels: {e.Message}");
        }
    }

    // The Radio

    // Give the local player a radio tuned to this frequency (call again to change frequency)

    public async Task EquipRadioAsync(int frequency)
    {

        if (!inSession) return;

        try
        {

            if (hasRadio)
            {

                string oldChannel = radioChannel;

                hasRadio = false;
                radioHeld = false;
                radioChannel = null;

                await VivoxService.Instance.LeaveChannelAsync(oldChannel);
            }

            radioChannel = $"radio_{sessionID}_{frequency}";

            await VivoxService.Instance.JoinGroupChannelAsync(radioChannel, ChatCapability.AudioOnly);
            hasRadio = true;

            //Joining a channel can switch where the mic goes, so set it again
            RequestApplyTransmission();
            Debug.Log($"[Voice] Radio tuned to frequency {frequency}");
        }

        catch (Exception e)
        {
            Debug.LogError($"[Voice] Failed to equip radio: {e}");
        }
    }

    //Take the radio away 

    public async Task UnequipRadioAsync()
    {
        if (!hasRadio) return;

        string oldChannel = radioChannel;
        hasRadio = false;
        radioHeld = false;
        radioChannel = null;

        try
        {
            await VivoxService.Instance.LeaveChannelAsync(oldChannel);
        }

        catch(Exception e)
        {

            Debug.LogWarning($"[Voice] Error leaving radio channel: {e.Message}");
        }

        RequestApplyTransmission();
    }


    //Local Player

    public void SetLocalPlayer(Transform head) => localHead = head;

    public void ClearLocalPlayer(Transform head)
    {
        if (localHead == head) localHead = null;
    }

    //Per Frame

    private void Update()
    {
        if (!inSession) return;

        //Tell Vivox where the head is and which way it faces. Other players do the same
        //On own machines, nothing sent over netcode

        if(localHead != null)
        {

            VivoxService.Instance.Set3DPosition(localHead.gameObject, proximityChannel);
        }

        bool held = hasRadio && RadioKeyHeld();
        if (held != radioHeld)
        {
            radioHeld = held;
            RequestApplyTransmission();
        }
    }

    private bool RadioKeyHeld()
    {
        return Keyboard.current != null && Keyboard.current.vKey.isPressed;
    }

    //Mic routing

    //The changes are queued so quick key taps can't finish out of order

    private async void RequestApplyTransmission()
    {

        if (applying)
        {
            dirty = true;
            return;
        }

        applying = true;
        try
        {

            do
            {
                dirty = false;
                await ApplyTransmissionAsync();
            }
            while (dirty);
        }

        catch (Exception e)
        {

            Debug.LogWarning($"[Voice] Transmission change failed: {e.Message}");

        }
        finally
        {
            applying = false;
        }
    }


    private async Task ApplyTransmissionAsync()
    {
        if (!inSession) return;

        if (hasRadio && radioHeld)
        {

            if (alsoHeardNearby)
            {
                await
                    VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All);

            }
            else
            {
                await
                    VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, radioChannel);

            }
        }
        else
        {
            await
                VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, proximityChannel);
        }
    }
    
    //For Cleanup
    
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        var nm = NetworkManager.Singleton;
        
        if (nm != null) nm.OnClientConnectedCallback -= OnClientConnected;
        
        if (inSession)
        {
            _ = LeaveVoiceAsync();
        }
    }
    
}
