using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    // =========================================================
    // HEALTH
    // =========================================================

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float startingHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private bool invertFacing = false; // ติ๊กถ้า sprite ตัวนี้หันด้านกลับ

    // =========================================================
    // PLAYER DETECTION
    // =========================================================

    [Header("Player Detection")]
    [SerializeField] private float detectionRange = 5f;

    // =========================================================
    // ATTACK
    // =========================================================

    [Header("Attack")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;

    // =========================================================
    // RANDOM MOVEMENT
    // =========================================================

    [Header("Random Movement")]
    [SerializeField] private float changeDirectionTimeMin = 1f;
    [SerializeField] private float changeDirectionTimeMax = 3f;

    // =========================================================
    // WALL DETECTION
    // =========================================================

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 0.5f; // ระยะจากขอบตัว ไปทางทิศที่เดิน
    [SerializeField] private float wallCheckRadius = 0.15f;  // รัศมีวงกลมตรวจ ณ จุดนั้น

    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    private Transform player;
    private PlayerController playerHealth;

    private Vector2 movementDirection;

    private float directionTimer;
    private float attackTimer;

    private bool isChasing;

    // =========================================================
    // START
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        // ป้องกัน Starting HP มากกว่า Max HP
        startingHealth = Mathf.Clamp(
            startingHealth,
            0f,
            maxHealth
        );

        CurrentHealth = startingHealth;

        Debug.Log(
            $"{gameObject.name} HP: " +
            $"{CurrentHealth} / {MaxHealth}"
        );

        FindPlayer();

        // เริ่มด้วยการสุ่มทิศ
        ChangeRandomDirection();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();

            RandomMovement();

            return;
        }

        attackTimer -= Time.deltaTime;

        float distanceToPlayer = Vector2.Distance(
            transform.position,
            player.position
        );

        // =============================================
        // PLAYER อยู่ในระยะมองเห็น
        // =============================================

        if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;

            ChasePlayer(distanceToPlayer);
        }
        else
        {
            isChasing = false;

            RandomMovement();
        }
    }

    // =========================================================
    // PHYSICS MOVEMENT
    // =========================================================

    private void FixedUpdate()
    {
        float currentSpeed = isChasing ? chaseSpeed : moveSpeed;

        UpdateFacing();

        rb.MovePosition(
            rb.position +
            movementDirection *
            currentSpeed *
            Time.fixedDeltaTime
        );
    }

    // =========================================================
    // FACING (หันหน้าตามทิศที่เดิน)
    // =========================================================

    private void UpdateFacing()
    {
        if (sr == null)
            return;

        if (movementDirection.x > 0.01f)
        {
            sr.flipX = invertFacing ? true : false;
        }
        else if (movementDirection.x < -0.01f)
        {
            sr.flipX = invertFacing ? false : true;
        }
    }

    // =========================================================
    // RANDOM MOVEMENT
    // =========================================================

    private void RandomMovement()
    {
        directionTimer -= Time.deltaTime;

        // หมดเวลา → สุ่มทิศใหม่
        if (directionTimer <= 0f)
        {
            ChangeRandomDirection();
        }

        // ตรวจ Wall
        CheckObstacle();
    }

    private void ChangeRandomDirection()
    {
        int randomDirection = Random.Range(0, 4);

        switch (randomDirection)
        {
            case 0:
                movementDirection = Vector2.left;
                break;

            case 1:
                movementDirection = Vector2.right;
                break;

            case 2:
                movementDirection = Vector2.up;
                break;

            case 3:
                movementDirection = Vector2.down;
                break;
        }

        directionTimer = Random.Range(
            changeDirectionTimeMin,
            changeDirectionTimeMax
        );
    }

    // =========================================================
    // WALL DETECTION (ตรวจล่วงหน้าตามทิศที่กำลังเดิน)
    // =========================================================

    private void CheckObstacle()
    {
        if (movementDirection == Vector2.zero)
            return;

        if (col == null)
            return;

        // เริ่มจากขอบ Collider ของ Enemy ตามทิศที่กำลังเดิน
        Vector2 origin = col.bounds.center;

        if (movementDirection == Vector2.left)
        {
            origin.x = col.bounds.min.x;
        }
        else if (movementDirection == Vector2.right)
        {
            origin.x = col.bounds.max.x;
        }
        else if (movementDirection == Vector2.up)
        {
            origin.y = col.bounds.max.y;
        }
        else if (movementDirection == Vector2.down)
        {
            origin.y = col.bounds.min.y;
        }

        // ระยะตรวจล่วงหน้า = ระยะที่ตั้งไว้ + ระยะที่จะเดินจริงในเฟรมถัดไป
        // เพื่อให้เปลี่ยนทิศ "ก่อน" ตัวจะเดินไปชน Wall จริง ๆ
        // (ถ้าปล่อยให้ชนก่อนค่อยเปลี่ยนทิศ Rigidbody จะไปทับกับ Wall แล้วฟิสิกส์
        //  จะดันตัวกลับทุกเฟรม ทำให้เกิดอาการสั่น/กระพริบ)
        float currentSpeed = isChasing ? chaseSpeed : moveSpeed;
        float lookAheadDistance = wallCheckDistance +
            (currentSpeed * Time.fixedDeltaTime);

        Vector2 checkPoint = origin + movementDirection * lookAheadDistance;

        Collider2D hitCollider = Physics2D.OverlapCircle(
            checkPoint,
            wallCheckRadius
        );

        if (hitCollider == null)
            return;

        if (hitCollider == col)
            return;

        if (hitCollider.CompareTag("Wall"))
        {
            ChangeDirectionAwayFromWall();
        }
    }

    // =========================================================
    // เปลี่ยนทิศใหม่ (ห้ามเป็นทิศเดิมที่ชน Wall)
    // =========================================================

    private void ChangeDirectionAwayFromWall()
    {
        Vector2 blockedDirection = movementDirection;
        Vector2 newDirection;

        int safetyCounter = 0;

        do
        {
            int randomDirection = Random.Range(0, 4);

            switch (randomDirection)
            {
                case 0:
                    newDirection = Vector2.left;
                    break;

                case 1:
                    newDirection = Vector2.right;
                    break;

                case 2:
                    newDirection = Vector2.up;
                    break;

                default:
                    newDirection = Vector2.down;
                    break;
            }

            safetyCounter++;
        }
        while (newDirection == blockedDirection && safetyCounter < 10);

        movementDirection = newDirection;

        directionTimer = Random.Range(
            changeDirectionTimeMin,
            changeDirectionTimeMax
        );
    }

    // =========================================================
    // CHASE PLAYER
    // =========================================================

    private void ChasePlayer(float distanceToPlayer)
    {
        // =============================================
        // เข้าใกล้ Player → โจมตี
        // =============================================

        if (distanceToPlayer <= attackRange)
        {
            // หยุดเดิน
            movementDirection = Vector2.zero;

            AttackPlayer();

            return;
        }

        // =============================================
        // เดินเข้าหา Player
        // =============================================

        Vector2 direction = (
            player.position -
            transform.position
        ).normalized;

        movementDirection = direction;

        // ตรวจ Wall ระหว่างทาง
        CheckObstacle();
    }

    // =========================================================
    // ATTACK PLAYER
    // =========================================================

    private void AttackPlayer()
    {
        if (playerHealth == null)
            return;

        if (attackTimer > 0f)
            return;

        playerHealth.TakeDamage(damage);

        Debug.Log(
            $"{gameObject.name} โจมตี Player " +
            $"Damage: {damage}"
        );

        attackTimer = attackCooldown;
    }

    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.transform;

        playerHealth =
            playerObject.GetComponent<PlayerController>();
    }

    // =========================================================
    // TAKE DAMAGE
    // =========================================================

    public void TakeDamage(float damageAmount)
    {
        if (CurrentHealth <= 0f)
            return;

        damageAmount = Mathf.Max(0f, damageAmount);

        CurrentHealth -= damageAmount;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            MaxHealth
        );

        Debug.Log(
            $"{gameObject.name} HP: " +
            $"{CurrentHealth} / {MaxHealth}"
        );

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    // =========================================================
    // HEAL
    // =========================================================

    public void Heal(float amount)
    {
        if (CurrentHealth <= 0f)
            return;

        amount = Mathf.Max(0f, amount);

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            MaxHealth
        );

        Debug.Log(
            $"{gameObject.name} HP: " +
            $"{CurrentHealth} / {MaxHealth}"
        );
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        Debug.Log(
            $"{gameObject.name} Dead"
        );

        Destroy(gameObject);
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // ระยะมองเห็น Player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        // ระยะโจมตี
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        // จุดตรวจ Wall ข้างหน้าตามทิศที่เดิน
        if (col != null && movementDirection != Vector2.zero)
        {
            Vector2 origin = col.bounds.center;

            if (movementDirection == Vector2.left)
                origin.x = col.bounds.min.x;
            else if (movementDirection == Vector2.right)
                origin.x = col.bounds.max.x;
            else if (movementDirection == Vector2.up)
                origin.y = col.bounds.max.y;
            else if (movementDirection == Vector2.down)
                origin.y = col.bounds.min.y;

            Vector2 checkPoint = origin + movementDirection * wallCheckDistance;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(checkPoint, wallCheckRadius);
            Gizmos.DrawLine(origin, checkPoint);
        }
    }
}