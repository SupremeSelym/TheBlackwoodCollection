using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class ElectricalGrid : MonoBehaviour
{
    [SerializeField] public float timeToDeplete = 10f;
    [SerializeField] private Light CeilingLight;

    private InputAction genInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CeilingLight = GetComponentInChildren<Light>();
        genInput = InputSystem.actions.FindAction("Generator");
    }

    // Update is called once per frame
    void Update()
    {
        Generators();
    }

    public void Generators()
    {
        if (timeToDeplete > 0)
        {
            timeToDeplete -= Time.deltaTime;
            if (timeToDeplete <= 0)
            {
                CeilingLight.color = Color.red;
            }
        }
        if (genInput.WasPressedThisFrame())
        {
            ResetGen();
        }   
        
    }

    public void ResetGen()
    {
        timeToDeplete += timeToDeplete;
        CeilingLight.color = Color.yellow;
    }
}
