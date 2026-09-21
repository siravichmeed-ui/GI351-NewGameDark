using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // =========================
    // MOVEMENT
    // =========================

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1f;

    public bool FacingLeft
    {
        get { return facingLeft; }
        set { facingLeft = value; }
    }

    private PlayerControls playerControls;
    private Vector2 movement;

    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;

    private bool facingLeft = false;


    // =========================
    // HEALTH
    // =========================

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float startingHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0f;


    // =========================
    // HIT FLASH
    // =========================

    [Header("Hit Flash")]
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.1f;

    private Color originalColor;
    private Coroutine hitFlashCoroutine;


    // =========================
    // AWAKE
    // =========================

    private void Awake()
    {
        playerControls = new PlayerControls();

        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();

        startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        CurrentHealth = startingHealth;

        if (mySpriteRender != null)
            originalColor = mySpriteRender.color;
    }


    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }


    // =========================
    // UPDATE
    // =========================

    private void Update()
    {
        PlayerInput();
    }


    private void FixedUpdate()
    {
        AdjustPlayerFacingDirection();
        Move();
    }


    // =========================
    // INPUT
    // =========================

    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();

        if (myAnimator != null)
        {
            myAnimator.SetFloat("moveX", movement.x);
            myAnimator.SetFloat("moveY", movement.y);
        }
    }


    // =========================
    // MOVE
    // =========================

    private void Move()
    {
        rb.MovePosition(
            rb.position +
            movement * (moveSpeed * Time.fixedDeltaTime)
        );
    }


    // =========================
    // FACE
    // =========================

    private void AdjustPlayerFacingDirection()
    {
        Vector3 mousePos = Input.mousePosition;

        Vector3 playerScreenPoint =
            Camera.main.WorldToScreenPoint(transform.position);

        if (mousePos.x < playerScreenPoint.x)
        {
            mySpriteRender.flipX = true;
            FacingLeft = true;
        }
        else
        {
            mySpriteRender.flipX = false;
            FacingLeft = false;
        }
    }


    // =========================
    // DAMAGE
    // =========================

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        damage = Mathf.Max(0f, damage);

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            MaxHealth
        );

        StartHitFlash();

        if (IsDead)
            Die();
    }


    // =========================
    // HIT FLASH
    // =========================

    private void StartHitFlash()
    {
        if (mySpriteRender == null)
            return;

        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);

        hitFlashCoroutine = StartCoroutine(HitFlash());
    }


    private IEnumerator HitFlash()
    {
        mySpriteRender.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        mySpriteRender.color = originalColor;

        hitFlashCoroutine = null;
    }


    // =========================
    // HEAL
    // =========================

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (CurrentHealth >= MaxHealth)
            return;

        amount = Mathf.Max(0f, amount);

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            MaxHealth
        );
    }


    // =========================
    // HEALTH PICKUP
    // =========================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("HealthPickup"))
            return;

        HealthPickupData pickup =
            other.GetComponent<HealthPickupData>();

        if (pickup == null)
            return;

        if (CurrentHealth >= MaxHealth)
            return;

        Heal(pickup.HealAmount);

        Destroy(other.gameObject);
    }


    // =========================
    // MAX HEALTH
    // =========================

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            MaxHealth
        );
    }


    // =========================
    // DEATH
    // =========================

    private void Die()
    {
        Debug.Log("Player Dead");
    }
}


// ใช้เก็บค่าของ Health Pickup
// ไม่ต้องมี HealthPickup.cs แยกแล้ว
public class HealthPickupData : MonoBehaviour
{
    [SerializeField] private float healAmount = 20f;

    public float HealAmount => healAmount;
}