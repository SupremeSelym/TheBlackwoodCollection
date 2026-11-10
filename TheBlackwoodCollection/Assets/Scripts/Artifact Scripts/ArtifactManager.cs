using UnityEngine;
using System;
using System.Collections.Generic;

// Class for managing artifacts
public static class ArtifactManager
{
    static readonly List<ArtifactBase> all_artifacts = new();
    public static IReadOnlyList<ArtifactBase> All_Artifacts => all_artifacts;   // Publicly viewable (but not modifiable) list of the artifacts
    public static event Action<ArtifactBase> ArtifactBreached;

    // Add an artifact to the overall artifact list
    public static void Register(ArtifactBase art)
    {
        if (!all_artifacts.Contains(art))
            all_artifacts.Add(art);
    }

    // Remove an artifact from the overall artifact list
    public static void Unregister(ArtifactBase art)
    {
        all_artifacts.Remove(art);
    }

    // Remove ALL artifacts from the overall artifact list; use with caution!
    public static void UnregisterAll()
    {
        all_artifacts.Clear();
    }

    // Invokes the ArtifactBreached event for a given artifact
    public static void NotifyBreached(ArtifactBase art)
    {
        ArtifactBreached?.Invoke(art);
    }

    // Applies the given effect method to all other artifacts; effect method should include
    // ways to limit which artifacts are affected (by distance, which wing the artifact is in, etc.)
    public static void ApplyEffectToOthers(ArtifactBase self, Action<ArtifactBase> effect)
    {
        foreach (var art in all_artifacts)
            if (art != self)
                effect(art);
    }

}


