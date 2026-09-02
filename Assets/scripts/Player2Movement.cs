using UnityEngine;
using UnityEngine.InputSystem;

public class Player2Movement : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        Vector2 move = Vector2.zero;

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            move = Vector2.left;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            move = Vector2.right;
        }

        transform.position += (Vector3)(move * speed * Time.deltaTime);
    }
}