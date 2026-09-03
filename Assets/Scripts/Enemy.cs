using UnityEngine;
using System;

/// <summary>
/// Basic 2D enemy that receives damage from bullets and notifies when destroyed.
/// Add this component to enemy prefabs or leave it to be added at spawn time.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Enemy stats (base)")]
    [Tooltip("Base health for this enemy prefab. Scaled per level by LevelManager.")]
    public float baseHealth = 10f;

    [Tooltip("Score awarded when this enemy dies.")]
    public int scoreValue = 10;

    private float _currentHealth;

    public static event Action<Enemy> OnEnemyDestroyed;

    [Header("Shooting")]
    [Tooltip("Prefab for the bullet (must have `Bullet` component).")]
    public GameObject bulletPrefab;

    [Tooltip("Optional spawn point for bullets. If null, enemy position is used.")]
    public Transform firePoint;

    [Tooltip("Seconds between shots.")]
    public float fireRate = 1f;

    [Tooltip("Bullet speed (units/sec).")]
    public float bulletSpeed = 10f;

    [Tooltip("Bullet damage.")]
    public float bulletDamage = 5f;

    [Tooltip("Bullet range.")]
    public float bulletRange = 20f;

    private float _nextFireTime = 0f;

    /// <summary>
    /// Called by LevelManager after instantiation so the enemy knows its scaled health.
    /// </summary>
    public void InitializeHealth(float health)
    {
        _currentHealth = Mathf.Max(0f, health);
    }

    public bool IsAlive => _currentHealth > 0f;

    private void Update()
    {
        // Simple automatic shooting behavior: fire when cooldown allows.
        if (bulletPrefab != null && Time.time >= _nextFireTime && IsAlive)
        {
            Shoot();
            _nextFireTime = Time.time + Mathf.Max(0.0001f, fireRate);
        }
    }

    private void Shoot()
    {
        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 direction;

        // Prefer shooting at a GameObject tagged "Player" if present; otherwise shoot downwards.
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            direction = (player.transform.position - spawnPos).normalized;
        }
        else
        {
            direction = Vector3.down;
        }

        var go = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        if (go == null) return;

        var bullet = go.GetComponent<Bullet>() ?? go.GetComponentInChildren<Bullet>();
        if (bullet == null)
        {
            Debug.LogWarning("Instantiated bullet prefab does not contain a Bullet component.");
            return;
        }

        // Use Bullet API: initialize and set velocity for 2D movement.
        bullet.Initialize(bulletSpeed, bulletDamage, bulletRange, direction);
        try
        {
            bullet.SetVelocity((Vector2)(direction * bulletSpeed));
        }
        catch
        {
            // If SetVelocity not applicable (e.g., 3D bullet), ignore; Initialize already set speed/direction.
        }
    }

    private void TakeDamage(float amount)
    {
        if (amount <= 0f || !IsAlive) return;

        _currentHealth -= amount;

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        OnEnemyDestroyed?.Invoke(this);
        Destroy(gameObject);
    }

    // Collide with bullets (2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        var bullet = other.GetComponent<Bullet>();
        if (bullet == null)
        {
            // sometimes the Bullet might be on the parent; try that
            bullet = other.GetComponentInParent<Bullet>();
        }

        if (bullet != null)
        {
            float damage = bullet.Damage;
            TakeDamage(damage);
            Destroy(bullet.gameObject);
            return;
        }

        // optional: handle collisions with player bullets implemented differently
    }

    // also accept collisions by collision events
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var bullet = collision.collider.GetComponent<Bullet>() ?? collision.collider.GetComponentInParent<Bullet>();
        if (bullet != null)
        {
            TakeDamage(bullet.Damage);
            Destroy(bullet.gameObject);
        }
    }
}