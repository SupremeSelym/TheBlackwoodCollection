using UnityEngine;
using Unity.Netcode;

public interface IInteractable 
{
    void toggleSelection(bool isSelected);
    NetworkObject NetworkObject { get; }
}
