using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    // ทิศทางที่หันล่าสุด
    public Vector2 LastMoveDirection { get; private set; } = Vector2.right;

    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        movement = Vector2.zero;

        // WASD
        if (Keyboard.current.aKey.isPressed)
            movement.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            movement.x += 1;

        if (Keyboard.current.sKey.isPressed)
            movement.y -= 1;

        if (Keyboard.current.wKey.isPressed)
            movement.y += 1;

        // ป้องกันเดินทแยงเร็วกว่าเดินตรง
        movement = movement.normalized;

        // จำทิศซ้าย / ขวาล่าสุด
        // เดินขึ้นลงจะไม่เปลี่ยนทิศ
        if (movement.x != 0)
        {
            LastMoveDirection = new Vector2(movement.x, 0);
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + movement * moveSpeed * Time.fixedDeltaTime
        );
    }

    public bool IsMoving()
    {
        return movement != Vector2.zero;
    }

    public Vector2 GetMovement()
    {
        return movement;
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }
}