using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float maxRange = 15f;

    private Vector3 startPosition;
    private Vector3 direction = Vector3.down;

    public float Damage => damage;

    public void Initialize(
        float bulletSpeed,
        float bulletDamage,
        float range,
        Vector3 bulletDirection)
    {
        speed = bulletSpeed;
        damage = bulletDamage;
        maxRange = range;

        direction = bulletDirection.normalized;

        startPosition = transform.position;
    }

    private void Update()
    {
        transform.position +=
            direction *
            speed *
            Time.deltaTime;

        if (Vector3.Distance(
                startPosition,
                transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        CompanionShip companion = other.GetComponentInParent<CompanionShip>();
        if (companion != null)
        {
            companion.TakeHit();
            Destroy(gameObject);
            return;
        }

        PlayerHealth player =
            other.GetComponent<PlayerHealth>();

        if (player == null)
        {
            player =
                other.GetComponentInParent<PlayerHealth>();
        }

        if (player != null)
        {
            player.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
