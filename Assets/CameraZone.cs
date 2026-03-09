using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public Vector3 cameraPosition;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<CurlyMovement>() != null)
        {
            CameraManager.instance.MoveToZone(cameraPosition);
        }
    }
}