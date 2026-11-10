using UnityEngine;

public class PowerConsumer : MonoBehaviour
{
    [SerializeField] private ElectricalGrid grid;
    [SerializeField] private Light[] lights;
    [SerializeField] private GameObject[] objectsToEnable;

    private void OnEnable()
    {
        if (grid == null)
            return;

        grid.HasPower.OnValueChanged += OnPowerChanged;
        ApplyPower(grid.HasPower.Value);
    }

    private void OnDisable()
    {
        if (grid != null)
            grid.HasPower.OnValueChanged -= OnPowerChanged;
    }

    private void OnPowerChanged(bool previous, bool current)
    {
        ApplyPower(current);
    }

    private void ApplyPower(bool powered)
    {
        foreach (Light lightSource in lights)
        {
            if (lightSource != null)
                lightSource.enabled = powered;
        }

        foreach (GameObject target in objectsToEnable)
        {
            if (target != null)
                target.SetActive(powered);
        }
    }
}
