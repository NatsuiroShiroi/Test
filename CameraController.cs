using UnityEngine;

//Handles camera panning with WASD keys
public class CameraController : MonoBehaviour
{
    [Tooltip("Units per second for the camera")] public float cameraSpeed = 5f;

    void Update()
    {
        HandleCameraMove();
    }

    private void HandleCameraMove()
    {
        float camMx = 0f, camMy = 0f;
        if (Input.GetKey(KeyCode.W)) camMy += 1f;
        if (Input.GetKey(KeyCode.S)) camMy -= 1f;
        if (Input.GetKey(KeyCode.A)) camMx -= 1f;
        if (Input.GetKey(KeyCode.D)) camMx += 1f;

        Vector3 camMove = new Vector3(camMx, camMy, 0f);
        if (camMove.sqrMagnitude > 1f) camMove.Normalize();
        Camera.main.transform.Translate(camMove * cameraSpeed * Time.deltaTime, Space.World);
    }
}
