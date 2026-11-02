using UnityEngine;

[CreateAssetMenu(menuName = "Containment/Artifact Data")]
public class ArtifactData : ScriptableObject
{
    public string ArtifactName;
    [TextArea] public string ArtifactDescription;
    public Sprite Icon;

    [Header("Containment")]
    public float MaxContainment = 100f;                     // How "soothed" the artifact can be
    public float BaseDecayPerSecond = 1.5f;                 // Amount that the containment decays per second
    [Range(0f, 1f)] public float AgitatedThreshold = 0.6f;  // Percent threshold when artifact becomes agitated
    [Range(0f, 1f)] public float CriticalThreshold = 0.3f;  // Percent threshold when artifact reaches critical agitation
    [Range(0f, 1f)] public float BreachThreshold = 0.1f;    // Percent threshold when artifact breaches containment

    [Header("Interaction")]
    public float InteractRange = 3f;                        // Range that the artifact can be interacted from
    public float RecontainAmount = 40f;                     // Amount of containment restored by interaction

}
