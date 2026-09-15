using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoSkill : MonoBehaviour
{
    [Header("Echo")]
    [SerializeField] private float scanRadius = 5f;
    [SerializeField] private float waveDuration = 0.8f;

    [Header("Reveal Outline")]
    [SerializeField] private float revealDuration = 2f;
    [SerializeField] private float outlineWidth = 0.08f;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private int outlineSortingOrder = 100;

    [Header("Outline Blink")]
    [SerializeField] private float blinkInterval = 0.12f;

    [Header("Wave Visual")]
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float waveWidth = 0.08f;
    [SerializeField] private Color waveColor = Color.white;

    private SkillState skillState;

    private Dictionary<Enemy, GameObject> activeOutlines =
        new Dictionary<Enemy, GameObject>();

    private Dictionary<Enemy, Coroutine> outlineCoroutines =
        new Dictionary<Enemy, Coroutine>();

    private void Awake()
    {
        skillState = GetComponent<SkillState>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseEcho();
        }
    }

    // =========================================================
    // USE ECHO
    // =========================================================

    private void UseEcho()
    {
        if (skillState == null)
        {
            Debug.LogWarning("Player ไม่มี SkillState");
            return;
        }

        if (!skillState.TryUseSkill())
            return;

        StartCoroutine(EchoWave());
    }

    // =========================================================
    // ECHO WAVE
    // =========================================================

    private IEnumerator EchoWave()
    {
        GameObject waveObject =
            new GameObject("Echo Wave");

        waveObject.transform.position =
            transform.position;

        LineRenderer line =
            waveObject.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = circleSegments;

        line.startWidth = waveWidth;
        line.endWidth = waveWidth;

        line.startColor = waveColor;
        line.endColor = waveColor;

        line.sortingOrder =
            outlineSortingOrder;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader != null)
        {
            line.material =
                new Material(shader);
        }

        // สร้างรูปร่างวงกลม
        for (int i = 0; i < circleSegments; i++)
        {
            float angle =
                (float)i /
                circleSegments *
                Mathf.PI * 2f;

            float x =
                Mathf.Cos(angle);

            float y =
                Mathf.Sin(angle);

            line.SetPosition(
                i,
                new Vector3(
                    x,
                    y,
                    0f
                )
            );
        }

        HashSet<Enemy> revealedEnemies =
            new HashSet<Enemy>();

        float timer = 0f;

        while (timer < waveDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / waveDuration
                );

            float currentRadius =
                Mathf.Lerp(
                    0f,
                    scanRadius,
                    progress
                );

            waveObject.transform.localScale =
                Vector3.one *
                currentRadius;

            ScanWave(
                currentRadius,
                revealedEnemies
            );

            yield return null;
        }

        Destroy(waveObject);
    }

    // =========================================================
    // SCAN ENEMY
    // =========================================================

    private void ScanWave(
        float currentRadius,
        HashSet<Enemy> revealedEnemies)
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                currentRadius
            );

        foreach (Collider2D hit in hits)
        {
            Enemy enemy =
                hit.GetComponent<Enemy>();

            if (enemy == null)
                continue;

            if (revealedEnemies.Contains(enemy))
                continue;

            revealedEnemies.Add(enemy);

            RevealEnemy(enemy);
        }
    }

    // =========================================================
    // REVEAL ENEMY
    // =========================================================

    private void RevealEnemy(Enemy enemy)
    {
        // ลบ Outline เก่า
        if (activeOutlines.TryGetValue(
            enemy,
            out GameObject oldOutline))
        {
            if (oldOutline != null)
            {
                Destroy(oldOutline);
            }

            activeOutlines.Remove(enemy);
        }

        // หยุด Coroutine เก่า
        if (outlineCoroutines.TryGetValue(
            enemy,
            out Coroutine oldCoroutine))
        {
            if (oldCoroutine != null)
            {
                StopCoroutine(oldCoroutine);
            }

            outlineCoroutines.Remove(enemy);
        }

        // สร้าง Outline
        GameObject outlineObject =
            new GameObject("Echo Outline");

        outlineObject.transform.SetParent(
            enemy.transform,
            false
        );

        LineRenderer outline =
            outlineObject.AddComponent<LineRenderer>();

        outline.useWorldSpace = false;
        outline.loop = true;

        outline.startWidth =
            outlineWidth;

        outline.endWidth =
            outlineWidth;

        outline.startColor =
            outlineColor;

        outline.endColor =
            outlineColor;

        outline.sortingLayerName =
            "Default";

        outline.sortingOrder =
            outlineSortingOrder;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader != null)
        {
            outline.material =
                new Material(shader);
        }

        // สร้างรูปร่างตาม Collider
        CreateColliderOutline(
            enemy,
            outline
        );

        activeOutlines[enemy] =
            outlineObject;

        // เริ่มกระพริบ
        Coroutine coroutine =
            StartCoroutine(
                BlinkAndRemoveOutline(
                    enemy,
                    outlineObject
                )
            );

        outlineCoroutines[enemy] =
            coroutine;

        Debug.Log(
            "ECHO พบ Enemy: " +
            enemy.name
        );
    }

    // =========================================================
    // DETECT COLLIDER TYPE
    // =========================================================

    private void CreateColliderOutline(
        Enemy enemy,
        LineRenderer line)
    {
        Collider2D collider =
            enemy.GetComponent<Collider2D>();

        if (collider == null)
        {
            Debug.LogWarning(
                enemy.name +
                " ไม่มี Collider2D"
            );

            return;
        }

        // BOX
        BoxCollider2D box =
            collider as BoxCollider2D;

        if (box != null)
        {
            CreateBoxOutline(
                box,
                line
            );

            return;
        }

        // CIRCLE
        CircleCollider2D circle =
            collider as CircleCollider2D;

        if (circle != null)
        {
            CreateCircleOutline(
                circle,
                line
            );

            return;
        }

        // CAPSULE
        CapsuleCollider2D capsule =
            collider as CapsuleCollider2D;

        if (capsule != null)
        {
            CreateCapsuleOutline(
                capsule,
                line
            );

            return;
        }

        // POLYGON
        PolygonCollider2D polygon =
            collider as PolygonCollider2D;

        if (polygon != null)
        {
            CreatePolygonOutline(
                polygon,
                line
            );

            return;
        }

        // EDGE
        EdgeCollider2D edge =
            collider as EdgeCollider2D;

        if (edge != null)
        {
            CreateEdgeOutline(
                edge,
                line
            );

            return;
        }

        // COMPOSITE
        CompositeCollider2D composite =
            collider as CompositeCollider2D;

        if (composite != null)
        {
            CreateCompositeOutline(
                composite,
                line
            );

            return;
        }

        // ถ้าไม่ตรงกับชนิดด้านบน
        // ใช้ Bounds เป็นกรอบสำรอง
        CreateBoundsOutline(
            collider,
            line
        );
    }

    // =========================================================
    // BOX
    // =========================================================

    private void CreateBoxOutline(
        BoxCollider2D box,
        LineRenderer line)
    {
        line.positionCount = 4;
        line.loop = true;

        Vector2 size =
            box.size;

        Vector2 offset =
            box.offset;

        Vector2 bottomLeft =
            offset +
            new Vector2(
                -size.x / 2f,
                -size.y / 2f
            );

        Vector2 topLeft =
            offset +
            new Vector2(
                -size.x / 2f,
                size.y / 2f
            );

        Vector2 topRight =
            offset +
            new Vector2(
                size.x / 2f,
                size.y / 2f
            );

        Vector2 bottomRight =
            offset +
            new Vector2(
                size.x / 2f,
                -size.y / 2f
            );

        line.SetPosition(
            0,
            bottomLeft
        );

        line.SetPosition(
            1,
            topLeft
        );

        line.SetPosition(
            2,
            topRight
        );

        line.SetPosition(
            3,
            bottomRight
        );
    }

    // =========================================================
    // CIRCLE
    // =========================================================

    private void CreateCircleOutline(
        CircleCollider2D circle,
        LineRenderer line)
    {
        int segments = 32;

        line.positionCount =
            segments;

        line.loop = true;

        float radius =
            circle.radius;

        Vector2 offset =
            circle.offset;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                (float)i /
                segments *
                Mathf.PI * 2f;

            float x =
                Mathf.Cos(angle) *
                radius;

            float y =
                Mathf.Sin(angle) *
                radius;

            line.SetPosition(
                i,
                new Vector3(
                    offset.x + x,
                    offset.y + y,
                    0f
                )
            );
        }
    }

    // =========================================================
    // CAPSULE
    // =========================================================

    private void CreateCapsuleOutline(
        CapsuleCollider2D capsule,
        LineRenderer line)
    {
        int segments = 32;

        Vector2 size =
            capsule.size;

        Vector2 offset =
            capsule.offset;

        float width =
            size.x;

        float height =
            size.y;

        float radius =
            Mathf.Min(
                width,
                height
            ) / 2f;

        List<Vector3> points =
            new List<Vector3>();

        if (capsule.direction ==
            CapsuleDirection2D.Vertical)
        {
            float center =
                height / 2f - radius;

            // ด้านบน
            for (int i = 0; i <= 16; i++)
            {
                float angle =
                    Mathf.Lerp(
                        0f,
                        Mathf.PI,
                        i / 16f
                    );

                float x =
                    Mathf.Cos(angle) *
                    radius;

                float y =
                    center +
                    Mathf.Sin(angle) *
                    radius;

                points.Add(
                    new Vector3(
                        offset.x + x,
                        offset.y + y,
                        0f
                    )
                );
            }

            // ด้านล่าง
            for (int i = 0; i <= 16; i++)
            {
                float angle =
                    Mathf.Lerp(
                        Mathf.PI,
                        Mathf.PI * 2f,
                        i / 16f
                    );

                float x =
                    Mathf.Cos(angle) *
                    radius;

                float y =
                    -center +
                    Mathf.Sin(angle) *
                    radius;

                points.Add(
                    new Vector3(
                        offset.x + x,
                        offset.y + y,
                        0f
                    )
                );
            }
        }
        else
        {
            float center =
                width / 2f - radius;

            // ด้านขวา
            for (int i = 0; i <= 16; i++)
            {
                float angle =
                    Mathf.Lerp(
                        -Mathf.PI / 2f,
                        Mathf.PI / 2f,
                        i / 16f
                    );

                float x =
                    center +
                    Mathf.Cos(angle) *
                    radius;

                float y =
                    Mathf.Sin(angle) *
                    radius;

                points.Add(
                    new Vector3(
                        offset.x + x,
                        offset.y + y,
                        0f
                    )
                );
            }

            // ด้านซ้าย
            for (int i = 0; i <= 16; i++)
            {
                float angle =
                    Mathf.Lerp(
                        Mathf.PI / 2f,
                        Mathf.PI * 1.5f,
                        i / 16f
                    );

                float x =
                    -center +
                    Mathf.Cos(angle) *
                    radius;

                float y =
                    Mathf.Sin(angle) *
                    radius;

                points.Add(
                    new Vector3(
                        offset.x + x,
                        offset.y + y,
                        0f
                    )
                );
            }
        }

        line.positionCount =
            points.Count;

        line.loop = true;

        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }

    // =========================================================
    // POLYGON
    // =========================================================

    private void CreatePolygonOutline(
        PolygonCollider2D polygon,
        LineRenderer line)
    {
        if (polygon.pathCount <= 0)
            return;

        Vector2[] points =
            polygon.GetPath(0);

        if (points.Length < 2)
            return;

        line.positionCount =
            points.Length;

        line.loop = true;

        for (int i = 0; i < points.Length; i++)
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }

    // =========================================================
    // EDGE
    // =========================================================

    private void CreateEdgeOutline(
        EdgeCollider2D edge,
        LineRenderer line)
    {
        Vector2[] points =
            edge.points;

        if (points.Length < 2)
            return;

        line.positionCount =
            points.Length;

        // EdgeCollider ไม่ใช่รูปปิด
        line.loop = false;

        for (int i = 0; i < points.Length; i++)
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }

    // =========================================================
    // COMPOSITE
    // =========================================================

    private void CreateCompositeOutline(
        CompositeCollider2D composite,
        LineRenderer line)
    {
        if (composite.pathCount <= 0)
            return;

        // ใช้ Path แรก
        int pointCount =
            composite.GetPathPointCount(0);

        if (pointCount < 2)
            return;

        Vector2[] points =
            new Vector2[pointCount];

        composite.GetPath(
            0,
            points
        );

        line.positionCount =
            points.Length;

        line.loop = true;

        for (int i = 0; i < points.Length; i++)
        {
            line.SetPosition(
                i,
                points[i]
            );
        }
    }

    // =========================================================
    // FALLBACK
    // =========================================================

    private void CreateBoundsOutline(
        Collider2D collider,
        LineRenderer line)
    {
        Bounds bounds =
            collider.bounds;

        Vector3 center =
            collider.transform.InverseTransformPoint(
                bounds.center
            );

        Vector3 size =
            bounds.size;

        Vector3 bottomLeft =
            center +
            new Vector3(
                -size.x / 2f,
                -size.y / 2f,
                0f
            );

        Vector3 topLeft =
            center +
            new Vector3(
                -size.x / 2f,
                size.y / 2f,
                0f
            );

        Vector3 topRight =
            center +
            new Vector3(
                size.x / 2f,
                size.y / 2f,
                0f
            );

        Vector3 bottomRight =
            center +
            new Vector3(
                size.x / 2f,
                -size.y / 2f,
                0f
            );

        line.positionCount = 4;
        line.loop = true;

        line.SetPosition(
            0,
            bottomLeft
        );

        line.SetPosition(
            1,
            topLeft
        );

        line.SetPosition(
            2,
            topRight
        );

        line.SetPosition(
            3,
            bottomRight
        );
    }

    // =========================================================
    // BLINK + REMOVE
    // =========================================================

    private IEnumerator BlinkAndRemoveOutline(
        Enemy enemy,
        GameObject outlineObject)
    {
        LineRenderer outline =
            outlineObject.GetComponent<LineRenderer>();

        float timer = 0f;

        bool visible = true;

        while (timer < revealDuration)
        {
            if (outline != null)
            {
                outline.enabled = visible;

                visible = !visible;
            }

            yield return new WaitForSeconds(
                blinkInterval
            );

            timer += blinkInterval;
        }

        // หมดเวลา
        if (outlineObject != null)
        {
            Destroy(outlineObject);
        }

        // ลบจาก Dictionary
        if (activeOutlines.TryGetValue(
            enemy,
            out GameObject currentOutline))
        {
            if (currentOutline ==
                outlineObject)
            {
                activeOutlines.Remove(enemy);
            }
        }

        if (outlineCoroutines.ContainsKey(enemy))
        {
            outlineCoroutines.Remove(enemy);
        }
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.white;

        Gizmos.DrawWireSphere(
            transform.position,
            scanRadius
        );
    }
}