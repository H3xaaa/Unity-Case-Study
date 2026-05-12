using UnityEngine;

// ============================================================
// CameraController.cs
// Updated to support both story mode and multiplayer.
// In story mode: assign player in Inspector as before.
// In multiplayer: MPGameManager calls SetTarget() at runtime.
// ============================================================

public class MPCameraController : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public float minX, maxX, minY, maxY;

    public void SetTarget(Transform target)
    {
        player = target;
    }

    private void Update()
    {
        if (player == null) return;

        float x = player.position.x;
        float y = player.position.y;

        if (useBounds)
        {
            x = Mathf.Clamp(x, minX, maxX);
            y = Mathf.Clamp(y, minY, maxY);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }
}