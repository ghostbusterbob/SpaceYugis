using System.Collections;
using UnityEngine;

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
    private Coroutine _firingRoutine;

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
    }

    private void OnDisable()
    {
        StopFiring();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            StartFiring();
        }
        else
        {
            StopFiring();
        }
    }

    public void StartFiring()
    {
        if (_firingRoutine != null) return;
        _firingRoutine = StartCoroutine(FireLoop());
    }

    public void StopFiring()
    {
        if (_firingRoutine == null) return;

        StopCoroutine(_firingRoutine);
        _firingRoutine = null;
    }

    private IEnumerator FireLoop()
    {
        var interval = 1f / Mathf.Max(0.0001f, fireRate);

        while (true)
        {
            FireOnce();
            yield return new WaitForSeconds(interval);
        }
    }

    private void FireOnce()
    {
        if (bulletPrefab == null) return;

        var spawn = firePoint != null ? firePoint : transform;
        var bulletGO = Instantiate(bulletPrefab, spawn.position, spawn.rotation);

        // If bullet has the Bullet component use it for initialization
        var bulletComp = bulletGO.GetComponent<Bullet>();
        if (bulletComp == null)
        {
            bulletComp = bulletGO.AddComponent<Bullet>();
        }

        // direction uses forward for 3D; if your prefab is 2D oriented differently adjust accordingly
        bulletComp.Initialize(BulletSpeed, damage, bulletRange, spawn.forward);
    }
}