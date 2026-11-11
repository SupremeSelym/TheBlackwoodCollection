using UnityEngine;
using Unity.Netcode;

public class SecurityCamera : NetworkBehaviour
{
    private GameObject CameraBody;
    private Camera own_camera;

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnActiveCamera(Material render_texture)
    {
        
        own_camera.targetTexture = render_texture;
    }
}
