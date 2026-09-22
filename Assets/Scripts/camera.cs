using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Camera")]
    public float smoothSpeed = 10f;

    [Header("Border")]
    public string borderTag = "Border";

    [Tooltip("เพิ่มระยะกันไม่ให้กล้องเห็นขอบ")]
    public float borderPadding = 0.2f;

    private Camera cam;

    private float mapMinX;
    private float mapMaxX;
    private float mapMinY;
    private float mapMaxY;

    void Start()
    {
        cam = GetComponent<Camera>();

        FindMapBounds();
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        // ขนาดพื้นที่ที่กล้องมองเห็น
        float cameraHeight = cam.orthographicSize;
        float cameraWidth = cameraHeight * cam.aspect;

        // จุดที่กล้องควรตาม
        float targetX = player.position.x;
        float targetY = player.position.y;

        // จำกัดกล้องไม่ให้ออกนอกแมพ
        targetX = Mathf.Clamp(
            targetX,
            mapMinX + cameraWidth + borderPadding,
            mapMaxX - cameraWidth - borderPadding
        );

        targetY = Mathf.Clamp(
            targetY,
            mapMinY + cameraHeight + borderPadding,
            mapMaxY - cameraHeight - borderPadding
        );

        Vector3 targetPosition = new Vector3(
            targetX,
            targetY,
            transform.position.z
        );

        // กล้องตาม Player
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    void FindMapBounds()
    {
        GameObject[] borders = GameObject.FindGameObjectsWithTag(borderTag);

        if (borders.Length == 0)
        {
            Debug.LogError("ไม่พบ Border ที่มี Tag = " + borderTag);
            return;
        }

        bool found = false;

        foreach (GameObject border in borders)
        {
            Collider2D col = border.GetComponent<Collider2D>();

            if (col == null)
                continue;

            Bounds bounds = col.bounds;

            if (!found)
            {
                mapMinX = bounds.min.x;
                mapMaxX = bounds.max.x;
                mapMinY = bounds.min.y;
                mapMaxY = bounds.max.y;

                found = true;
            }
            else
            {
                mapMinX = Mathf.Min(mapMinX, bounds.min.x);
                mapMaxX = Mathf.Max(mapMaxX, bounds.max.x);

                mapMinY = Mathf.Min(mapMinY, bounds.min.y);
                mapMaxY = Mathf.Max(mapMaxY, bounds.max.y);
            }
        }

        Debug.Log(
            "Map Bounds: " +
            "X(" + mapMinX + " ถึง " + mapMaxX + ") " +
            "Y(" + mapMinY + " ถึง " + mapMaxY + ")"
        );
    }
}