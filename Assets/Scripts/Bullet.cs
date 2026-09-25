using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float range = 15f;

    private Vector3 direction = Vector3.up;
    private Vector3 startPosition;

    public float Damage => damage;
    public int OwnerPlayer { get; set; }
    public float ExplosionRadius { get; set; }

    private void Start()
    {
        startPosition = transform.position;
    }

    public void Initialize(
        float bulletSpeed,
        float bulletDamage,
        float bulletRange,
        Vector3 bulletDirection)
    {
        speed = bulletSpeed;
        damage = bulletDamage;
        range = bulletRange;

        direction = bulletDirection.normalized;
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.position +=
            direction * speed * Time.deltaTime;

        if (Vector3.Distance(startPosition, transform.position) >= range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HitEnemy(other);
    }

    public void HitEnemy(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        if (ExplosionRadius > 0f)
            Explode(enemy.transform.position);
        else
            enemy.TakeDamage(damage, OwnerPlayer);

        Destroy(gameObject);
    }

    private void Explode(Vector3 position)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, ExplosionRadius);
        System.Collections.Generic.HashSet<Enemy> damagedEnemies =
            new System.Collections.Generic.HashSet<Enemy>();
        foreach (Collider2D hit in colliders)
        {
            Enemy hitEnemy = hit.GetComponent<Enemy>();
            if (hitEnemy == null)
                hitEnemy = hit.GetComponentInParent<Enemy>();
            if (hitEnemy != null && damagedEnemies.Add(hitEnemy))
                hitEnemy.TakeDamage(damage, OwnerPlayer);
        }

        GameFlow.Instance?.ExplosiveBulletImpact(position, ExplosionRadius);
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude <= 0f)
        {
            speed = 0f;
            return;
        }

        direction = new Vector3(
            velocity.x,
            velocity.y,
            0f
        ).normalized;

        speed = velocity.magnitude;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }
}
