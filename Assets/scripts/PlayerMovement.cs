using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float leftLimit = -8f;
    public float rightLimit = 8f;

    private Rigidbody2D rb;
    private float move;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        move = 0f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            move = -1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            move = 1f;
        }
    }

    void FixedUpdate()
    {
        float newX = rb.position.x + move * speed * Time.fixedDeltaTime;

        newX = Mathf.Clamp(newX, leftLimit, rightLimit);

        rb.MovePosition(new Vector2(newX, rb.position.y));
    }
}