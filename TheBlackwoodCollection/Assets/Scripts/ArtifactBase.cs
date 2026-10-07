using UnityEngine;

public class ArtifactBase : MonoBehaviour
{
    public string ArtifactName;
    public string ArtifactDescription;
    private bool Contained = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnFailContainment()
    {
        Contained = false;
        BreachEffect();
    }

    private void OnRestoreContainment()
    {
        Contained = true;
        EndBreachEffect();
    }

    public void BreachEffect()
    {

    }

    public void EndBreachEffect()
    {

    }
}
