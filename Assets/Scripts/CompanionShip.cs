using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class CompanionShip : MonoBehaviour
{
    private PlayerHealth owner;
    private GameObject bulletPrefab;
    private float bulletSpeed;
    private float bulletDamage;
    private float bulletRange;
    private float nextShotTime;
    private Vector3 followOffset;
    private bool dead;
    private int slotIndex;
    private float fireCooldown;
    private float followSpeed;
    private float speedMultiplier = 1f;
    private float fireRateMultiplier = 1f;
    private int lives = 2;
    private int scorePlayerNumber;
    private float explosionRadius;
    private float explosionRadiusIncrement;
    private float maximumExplosionRadius;
    private int shieldHits;
    private LineRenderer shieldRing;

    public PlayerHealth Owner => owner;
    public bool IsDead => dead;

    public static void Spawn(PlayerHealth player, GameObject projectile, float speed, float damage, float range,
        float shipScale, float firstSideDistance, float extraSpacing, float verticalOffset, float shotCooldown,
        float movementFollowSpeed, int startingLives, int logicalPlayerNumber, float startingExplosionRadius,
        float explosionIncrement, float maxExplosionRadius)
    {
        Sprite ownerSprite = player.LifeSprite;
        if (ownerSprite == null)
            return;

        GameObject ship = new GameObject("Player " + player.PlayerNumber + " companion");
        ship.tag = "Player";
        ship.transform.position = player.transform.position;
        ship.transform.localScale = player.transform.lossyScale * shipScale;

        CompanionShip companion = ship.AddComponent<CompanionShip>();
        companion.owner = player;
        companion.bulletPrefab = projectile;
        companion.bulletSpeed = speed;
        companion.bulletDamage = damage;
        companion.bulletRange = range;
        companion.fireCooldown = shotCooldown;
        companion.followSpeed = movementFollowSpeed;
        companion.lives = Mathf.Max(1, startingLives);
        companion.scorePlayerNumber = logicalPlayerNumber;
        companion.explosionRadius = startingExplosionRadius;
        companion.explosionRadiusIncrement = explosionIncrement;
        companion.maximumExplosionRadius = maxExplosionRadius;
        companion.slotIndex = FindFreeSlot(player);
        int side = companion.slotIndex % 2 == 0 ? 1 : -1;
        int row = companion.slotIndex / 2;
        companion.followOffset = new Vector3(side * (firstSideDistance + row * extraSpacing), verticalOffset, 0f);
        companion.nextShotTime = Time.time + 0.35f;

        SpriteRenderer renderer = ship.GetComponent<SpriteRenderer>();
        renderer.sprite = ownerSprite;
        renderer.color = player.ShipColor;
        SpriteRenderer ownerRenderer = player.GetComponent<SpriteRenderer>();
        if (ownerRenderer != null)
            renderer.sharedMaterial = ownerRenderer.sharedMaterial;
        renderer.sortingOrder = 22;

        CircleCollider2D collider = ship.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.42f;

        Rigidbody2D body = ship.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        GameObject outlineObject = new GameObject("Companion outline");
        outlineObject.transform.SetParent(ship.transform, false);
        outlineObject.transform.localScale = Vector3.one * 1.18f;
        SpriteRenderer outline = outlineObject.AddComponent<SpriteRenderer>();
        outline.sprite = ownerSprite;
        outline.sharedMaterial = renderer.sharedMaterial;
        outline.color = new Color(1f, 0.82f, 0.08f);
        outline.sortingOrder = 21;
    }

    private static int FindFreeSlot(PlayerHealth player)
    {
        CompanionShip[] companions = Object.FindObjectsByType<CompanionShip>(FindObjectsSortMode.None);
        for (int slot = 0; slot < 20; slot++)
        {
            bool occupied = false;
            foreach (CompanionShip companion in companions)
            {
                if (!companion.dead && companion.owner == player && companion.slotIndex == slot)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
                return slot;
        }

        return companions.Length;
    }

    private void Update()
    {
        if (dead)
            return;

        if (owner == null || owner.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 target = owner.transform.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, target, followSpeed * speedMultiplier * Time.deltaTime);
        transform.rotation = Quaternion.identity;

        if (Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireCooldown / fireRateMultiplier;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
            return;

        GameObject bulletObject = Instantiate(bulletPrefab, transform.position + Vector3.up * 0.45f, Quaternion.identity);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
        {
            Destroy(bulletObject);
            return;
        }

        bullet.Initialize(bulletSpeed, bulletDamage, bulletRange, Vector3.up);
        bullet.OwnerPlayer = scorePlayerNumber;
        bullet.ExplosionRadius = explosionRadius;
        GameFlow.Instance?.PlayShot();
    }

    public void TakeHit()
    {
        if (dead)
            return;

        if (shieldHits > 0)
        {
            shieldHits--;
            UpdateShieldRing();
            GameFlow.Instance?.PlayHit();
            return;
        }

        lives--;
        if (lives > 0)
        {
            GameFlow.Instance?.PlayHit();
            return;
        }

        dead = true;
        GameFlow.Instance?.EnemyExplosion(transform.position);
        Destroy(gameObject);
    }

    public void ApplySpeedBoost()
    {
        if (!dead)
            speedMultiplier = Mathf.Min(2f, speedMultiplier + 0.25f);
    }

    public void ApplyFireRateBoost()
    {
        if (!dead)
            fireRateMultiplier = Mathf.Min(3f, fireRateMultiplier + 0.35f);
    }

    public void AddLife()
    {
        if (!dead)
            lives++;
    }

    public void ApplyExplosionBoost()
    {
        if (!dead)
            explosionRadius = Mathf.Min(maximumExplosionRadius, explosionRadius + explosionRadiusIncrement);
    }

    public void AddShieldHit()
    {
        if (dead)
            return;
        shieldHits++;
        UpdateShieldRing();
    }

    private void UpdateShieldRing()
    {
        if (shieldRing == null && shieldHits > 0)
        {
            GameObject ringObject = new GameObject("White companion shield ring");
            ringObject.transform.SetParent(transform, false);
            shieldRing = ringObject.AddComponent<LineRenderer>();
            shieldRing.useWorldSpace = false;
            shieldRing.loop = true;
            shieldRing.positionCount = 40;
            shieldRing.startColor = Color.white;
            shieldRing.endColor = Color.white;
            shieldRing.sortingOrder = 26;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) shieldRing.material = new Material(shader);
            for (int i = 0; i < shieldRing.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / shieldRing.positionCount;
                shieldRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.72f);
            }
        }
        if (shieldRing != null)
        {
            shieldRing.enabled = shieldHits > 0 && !dead;
            shieldRing.widthMultiplier = 0.055f + Mathf.Min(shieldHits - 1, 5) * 0.012f;
        }
    }
}
