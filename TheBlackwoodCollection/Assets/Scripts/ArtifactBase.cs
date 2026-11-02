using UnityEngine;
using Unity.Netcode;

public enum ArtifactState { Contained, Agitated, Critical, Breached }


public class ArtifactBase : MonoBehaviour
{
    private bool Contained;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Contained = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnFailContainment()
    {
        if (Contained)
        {
            Contained = false;
            BreachEffect();
        }
    }

    private void OnRestoreContainment()
    {
        if (!Contained)
        {
            Contained = true;
            EndBreachEffect();
        }
    }

    public void BreachEffect()
    {

    }

    public void EndBreachEffect()
    {

    }

    public bool GetContainmentStatus()
    {
        return Contained;
    }
}
