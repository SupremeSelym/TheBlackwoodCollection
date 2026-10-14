using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Flashlight : NetworkBehaviour
{
    [SerializeField] private Light flashlight;
    private InputAction toggleFlash;

    private NetworkVariable<bool> flashlightOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>(true);

        toggleFlash = InputSystem.actions.FindAction("Flashlight");
        flashlight.enabled = flashlightOn.Value;

        flashlightOn.OnValueChanged += OnFlashlightChanged;
    }
    public override void OnNetworkDespawn()
    {
        flashlightOn.OnValueChanged -= OnFlashlightChanged;
    }
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        toggleFlashlight();
    }

    private void toggleFlashlight()
    {
        if (toggleFlash != null && toggleFlash.WasPressedThisFrame())
        {
            flashlightOn.Value = !flashlightOn.Value;
        }
    }

    private void OnFlashlightChanged(bool oldValue, bool newValue)
    {
        flashlight.enabled = newValue;
    }
}
