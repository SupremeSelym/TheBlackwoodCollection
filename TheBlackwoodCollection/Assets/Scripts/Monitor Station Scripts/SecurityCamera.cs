using UnityEngine;
using Unity.Netcode;


public class SecurityCamera : NetworkBehaviour
{
    private GameObject CameraBody;
    private Camera own_camera;
    private Transform monitor_screen;
    [SerializeField] private float TargetFramerate = 20f;

    // Update is called once per frame
    void Update()
    {
        
    }

    //private void OnActiveCamera(Material render_texture)
    //{
        
    //    own_camera.targetTexture = render_texture;
    //}
}