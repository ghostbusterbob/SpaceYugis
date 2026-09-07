using UnityEngine;
using UnityEngine.InputSystem;

public class Battleship : MonoBehaviour
{
    [Header("Player 1")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player1FirePoint;

    [Header("Player 2")]
    [SerializeField] private Transform player2;
    [SerializeField] private Transform player2FirePoint;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float leftLimit = -7.5f;
    [SerializeField] private float rightLimit = 7.5f;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float bulletRange = 15f;

    private float player1NextFireTime;
    private float player2NextFireTime;

    private void Update()
    {
        MovePlayer1();
        MovePlayer2();

        ShootPlayer1();
        ShootPlayer2();
    }

    private void MovePlayer1()
    {
        if (player1 == null)
            return;

        float movement = 0f;

        if (Keyboard.current.aKey.isPressed)
            movement = -1f;

        if (Keyboard.current.dKey.isPressed)
            movement = 1f;

        Vector3 position = player1.position;

        position.x += movement * moveSpeed * Time.deltaTime;

        position.x = Mathf.Clamp(
            position.x,
            leftLimit,
            rightLimit
        );

        player1.position = position;
    }

    private void MovePlayer2()
    {
        if (player2 == null)
            return;

        float movement = 0f;

        if (Keyboard.current.leftArrowKey.isPressed)
            movement = -1f;

        if (Keyboard.current.rightArrowKey.isPressed)
            movement = 1f;

        Vector3 position = player2.position;

        position.x += movement * moveSpeed * Time.deltaTime;

        position.x = Mathf.Clamp(
            position.x,
            leftLimit,
            rightLimit
        );

        player2.position = position;
    }

    private void ShootPlayer1()
    {
        if (player1FirePoint == null)
            return;

        if (!Keyboard.current.wKey.isPressed)
            return;

        if (Time.time < player1NextFireTime)
            return;

        player1NextFireTime = Time.time + fireCooldown;

        CreateBullet(player1FirePoint);
    }

    private void ShootPlayer2()
    {
        if (player2FirePoint == null)
            return;

        if (!Keyboard.current.upArrowKey.isPressed)
            return;

        if (Time.time < player2NextFireTime)
            return;

        player2NextFireTime = Time.time + fireCooldown;

        CreateBullet(player2FirePoint);
    }

    private void CreateBullet(Transform firePoint)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Battleship: Bullet Prefab ontbreekt!");
            return;
        }

        GameObject bulletObject = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        Bullet bullet = bulletObject.GetComponent<Bullet>();

        if (bullet == null)
        {
            Debug.LogError("Bullet prefab heeft geen Bullet.cs!");
            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(
            bulletSpeed,
            bulletDamage,
            bulletRange,
            Vector3.up
        );
    }
}