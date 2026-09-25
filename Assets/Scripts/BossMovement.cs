using UnityEngine;

public class BossMovement : MonoBehaviour
{
    private float moveSpeed;
    private float leftLimit;
    private float rightLimit;
    private float dashChancePercent;
    private float dashSpeed;
    private float dashCheckInterval;
    private float dashCooldown;
    private float nextDashCheckTime;
    private float dashTargetX;
    private bool isDashing;

    private int direction = 1;

    public void Configure(
        float newMoveSpeed,
        float newLeftLimit,
        float newRightLimit,
        float newDashChancePercent,
        float newDashSpeed,
        float newDashCheckInterval,
        float newDashCooldown
    )
    {
        moveSpeed = newMoveSpeed;
        leftLimit = newLeftLimit;
        rightLimit = newRightLimit;
        dashChancePercent = Mathf.Clamp(newDashChancePercent, 0f, 100f);
        dashSpeed = Mathf.Max(1f, newDashSpeed);
        dashCheckInterval = Mathf.Max(0.1f, newDashCheckInterval);
        dashCooldown = Mathf.Max(0f, newDashCooldown);
        nextDashCheckTime = Time.time + dashCheckInterval;
        isDashing = false;
    }

    private void Update()
    {
        Vector3 position = transform.position;

        if (isDashing)
        {
            position.x = Mathf.MoveTowards(position.x, dashTargetX, dashSpeed * Time.deltaTime);
            transform.position = position;
            if (Mathf.Approximately(position.x, dashTargetX))
            {
                isDashing = false;
                direction = dashTargetX >= rightLimit ? -1 : 1;
                nextDashCheckTime = Time.time + dashCooldown;
            }
            return;
        }

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

        if (Time.time >= nextDashCheckTime)
        {
            nextDashCheckTime = Time.time + dashCheckInterval;
            if (Random.value * 100f < dashChancePercent)
                StartDash();
        }
    }

    private void StartDash()
    {
        float middle = (leftLimit + rightLimit) * 0.5f;
        if (transform.position.x < middle)
            dashTargetX = rightLimit;
        else if (transform.position.x > middle)
            dashTargetX = leftLimit;
        else
            dashTargetX = direction >= 0 ? rightLimit : leftLimit;
        isDashing = true;
    }
}
