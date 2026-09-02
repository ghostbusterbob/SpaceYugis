using UnityEngine;
using UnityEngine.InputSystem;

public class BattleshipShooter : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint; // if null will use this.transform

    [Header("Bullet settings")]
    [SerializeField] private float bulletRange = 50f;
    [SerializeField] private float damage = 10f;

    [Header("Firing")]
    [Tooltip("Bullets per second")]
    [SerializeField] private float fireRate = 1f;

    private const float BulletSpeed = 15f;
    private float _nextFireTime;

    private void Reset()
    {
        firePoint = transform;
    }

    private void OnEnable()
    {
        if (firePoint == null)
        {
            firePoint = transform;
        }

        _nextFireTime = 0f;
    }

    private void Update()
    {
        if (!IsFireHeld())
        {
            return;
        }

        if (Time.time < _nextFireTime)
        {
            return;
        }

        FireOnce();
        _nextFireTime = Time.time + (1f / Mathf.Max(0.0001f, fireRate));
    }

    private bool IsFireHeld()
    {
        return (Mouse.current != null && Mouse.current.leftButton.isPressed)
            || (Keyboard.current != null && Keyboard.current.wKey.isPressed);
    }

    private void FireOnce()
    {
        if (bulletPrefab == null)
        {
            return;
        }

        var spawn = firePoint != null ? firePoint : transform;
        var bulletGO = Instantiate(bulletPrefab, spawn.position, spawn.rotation);

        var bullet = bulletGO.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = bulletGO.AddComponent<Bullet>();
        }

                bullet.Initialize(BulletSpeed, damage, bulletRange, spawn.right);
    }
}