using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        Vector2 move = Vector2.zero;

        if (Keyboard.current.aKey.isPressed)
        {
            move = Vector2.left;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            move = Vector2.right;
        }

        transform.position += (Vector3)(move * speed * Time.deltaTime);
    }
}