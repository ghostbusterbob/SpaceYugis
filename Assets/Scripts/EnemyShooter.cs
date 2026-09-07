using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    private GameObject bulletPrefab;
    private Transform firePoint;

    private float bulletSpeed;
    private float bulletDamage;
    private float bulletRange;

    private float minShootDelay;
    private float maxShootDelay;

    private float nextShootTime;

    public void Configure(
        GameObject newBulletPrefab,
        Transform newFirePoint,
        float newBulletSpeed,
        float newBulletDamage,
        float newBulletRange,
        float newMinShootDelay,
        float newMaxShootDelay)
    {
        bulletPrefab = newBulletPrefab;
        firePoint = newFirePoint;

        bulletSpeed = newBulletSpeed;
        bulletDamage = newBulletDamage;
        bulletRange = newBulletRange;

        minShootDelay = newMinShootDelay;
        maxShootDelay = newMaxShootDelay;

        SetNextShootTime();
    }

    private void Start()
    {
        // LevelManager configureert de stats.
        // Daarom hoeft hier niets ingesteld te worden.
    }

    private void Update()
    {
        if (Time.time >= nextShootTime)
        {
            Shoot();
            SetNextShootTime();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        GameObject bulletObject = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyBullet bullet =
            bulletObject.GetComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.Initialize(
                bulletSpeed,
                bulletDamage,
                bulletRange,
                Vector3.down
            );
        }
    }

    private void SetNextShootTime()
    {
        nextShootTime = Time.time + Random.Range(
            minShootDelay,
            maxShootDelay
        );
    }
}