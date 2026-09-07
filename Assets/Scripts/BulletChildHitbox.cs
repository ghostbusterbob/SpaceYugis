using UnityEngine;

public class BulletChildHitbox : MonoBehaviour
{
    private Bullet bullet;

    private void Awake()
    {
        bullet = GetComponentInParent<Bullet>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (bullet != null)
        {
            bullet.HitEnemy(other);
        }
    }
}