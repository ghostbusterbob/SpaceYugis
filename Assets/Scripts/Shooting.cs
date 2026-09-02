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

    [Header("Player input selector")]
    [Tooltip("0 = use 'W' key (player 1). 1 = use 'UpArrow' key (player 2). Values in-between choose by <=0.5 or >0.5.")]
    [Range(0f, 1f)]
    [SerializeField] private float inputSelector = 0f;

    private const float BulletSpeed = 15f;
    private float _nextFireTime;

    private void Reset()
    {
        // helpful defaults when adding component
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
        if (!IsFirePressed())
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

    private bool IsFirePressed()
    {
        if (Keyboard.current == null) return false;

        // selector: 0 -> W, 1 -> UpArrow. Use <=0.5 to choose W for player1, >0.5 for player2.
        if (inputSelector <= 0.5f)
        {
            return Keyboard.current.wKey.isPressed;
        }
        else
        {
            return Keyboard.current.upArrowKey.isPressed;
        }
    }

    private void FireOnce()
    {
        if (bulletPrefab == null) return;

        var spawn = firePoint != null ? firePoint : transform;
        var bulletGO = Instantiate(bulletPrefab, spawn.position, spawn.rotation);

        // Ensure 2D movement component exists and initialize; prefab need not have a Bullet component
        var bulletComp = bulletGO.GetComponent<Bullet>();
        if (bulletComp == null)
        {
            bulletComp = bulletGO.AddComponent<Bullet>();
        }

        // For 2D, commonly 'right' is forward; adjust if your sprites use a different forward.
        bulletComp.Initialize(BulletSpeed, damage, bulletRange, spawn.up);
    }
}