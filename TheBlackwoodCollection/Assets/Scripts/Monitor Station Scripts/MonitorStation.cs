using UnityEngine;
using Unity.Netcode;

public class MonitorStation : NetworkBehaviour
{
    [SerializeField] public NetworkVariable<SecurityCamera>[] MonitorFeeds;
    [SerializeField] protected NetworkVariable<int> MonitorIndex;
    public Material MonitorTexture;

    // Update is called once per frame
    void Update()
    {
        
    }

    //public void SetCamera(int new_index)
    //{
    //    if (IsHost)
    //    {
    //        MonitorIndex = new_index;
            
    //    }
    //}
}
