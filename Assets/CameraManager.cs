using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;
    public float panSpeed = 3f;
    private Vector3 targetPosition;
    private bool isPanning = false;

    // Cached references — no more FindObjectOfType every frame
    private ZoeyAI zoey;
    private CurlyMovement curly;

    void Awake()
    {
        instance = this;
        targetPosition = transform.position;
    }

    void Start()
    {
        // Cache once at startup
        zoey = FindObjectOfType<ZoeyAI>();
        curly = FindObjectOfType<CurlyMovement>();
    }

    void Update()
    {
        if (isPanning)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, panSpeed * Time.unscaledDeltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isPanning = false;
                Time.timeScale = 1f;
            }
        }
    }

    public void MoveToZone(Vector3 position)
    {
        targetPosition = new Vector3(position.x, position.y, transform.position.z);
        isPanning = true;
        Time.timeScale = 0f;
        BringZoeyIntoZone(targetPosition);
    }

    void BringZoeyIntoZone(Vector3 zoneCenter)
    {
        if (zoey == null || zoey.isPaused) return;

        float camHeight = Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;

        float padding = 1f;
        float minX = zoneCenter.x - camWidth + padding;
        float maxX = zoneCenter.x + camWidth - padding;
        float minY = zoneCenter.y - camHeight + padding;
        float maxY = zoneCenter.y + camHeight - padding;

        Vector3 zoeyPos = zoey.transform.position;
        bool isOutside = zoeyPos.x < minX || zoeyPos.x > maxX || zoeyPos.y < minY || zoeyPos.y > maxY;

        if (isOutside && curly != null)
        {
            Vector3 destination = curly.transform.position + new Vector3(1f, 0f, 0f);
            destination.x = Mathf.Clamp(destination.x, minX, maxX);
            destination.y = Mathf.Clamp(destination.y, minY, maxY);
            zoey.MoveToPosition(destination);
        }
    }
}