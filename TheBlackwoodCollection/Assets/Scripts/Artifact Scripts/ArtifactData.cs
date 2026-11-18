using UnityEngine;

[CreateAssetMenu(menuName = "Containment/Artifact Data")]
public class ArtifactData : ScriptableObject
{
    public string ArtifactName;
    [TextArea] public string ArtifactDescription;
    public Sprite Icon;

    [Header("Containment")]
    [Tooltip("How \"soothed\" the artifact can be. Normal decay is 1 per second.")]
    public float MaxContainment = 100f;
    [Tooltip("Amount that the containment decays per second (1 = 1 unit containment per second).")]
    public float BaseDecayPerSecond = 1.5f;
    [Tooltip("Percent threshold when artifact becomes agitated (0.6 = 60% of MaxContainment).")]
    [Range(0f, 1f)] public float AgitatedThreshold = 0.6f;
    [Tooltip("Percent threshold when artifact reaches critical agitation.")]
    [Range(0f, 1f)] public float CriticalThreshold = 0.3f;
    [Tooltip("Percent threshold when artifact breaches containment.")]
    [Range(0f, 1f)] public float BreachThreshold = 0.1f;

    [Header("Interaction")]
    [Tooltip("Range that the artifact can be interacted from in standard units.")]
    public float InteractRange = 3f;
    [Tooltip("Amount of containment restored by interaction (temp var until unique containment methods established)")]
    public float RecontainAmount = 40f;
}
