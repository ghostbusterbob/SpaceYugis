using UnityEngine;

public class BossMovement : MonoBehaviour
{
    private float moveSpeed;
    private float leftLimit;
    private float rightLimit;

    private int direction = 1;

    public void Configure(
        float newMoveSpeed,
        float newLeftLimit,
        float newRightLimit
    )
    {
        moveSpeed = newMoveSpeed;
        leftLimit = newLeftLimit;
        rightLimit = newRightLimit;
    }

    private void Update()
    {
        Vector3 position = transform.position;

        position.x +=
            direction *
            moveSpeed *
            Time.deltaTime;

        if (position.x >= rightLimit)
        {
            position.x = rightLimit;
            direction = -1;
        }
        else if (position.x <= leftLimit)
        {
            position.x = leftLimit;
            direction = 1;
        }

        transform.position = position;
    }
}