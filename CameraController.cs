using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("Units per second for the camera")]
    public float cameraSpeed = 5f;

    void Update()
    {
        Vector3 m = new Vector3(
            Input.GetKey(KeyCode.D) ? +1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            Input.GetKey(KeyCode.W) ? +1 : Input.GetKey(KeyCode.S) ? -1 : 0,
            0
        );
        if (m.sqrMagnitude > 1) m.Normalize();
        transform.Translate(m * cameraSpeed * Time.deltaTime, Space.World);
    }
}