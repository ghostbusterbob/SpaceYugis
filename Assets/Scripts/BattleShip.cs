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

    [Header("COMPANION SETTINGS")]
    [Range(0.25f, 1f)] [SerializeField] private float companionScale = 0.58f;
    [Min(0.5f)] [SerializeField] private float companionFirstSideDistance = 1.35f;
    [Min(0.2f)] [SerializeField] private float companionExtraSpacing = 0.78f;
    [SerializeField] private float companionVerticalOffset = 0.12f;
    [Min(0.1f)] [SerializeField] private float companionFireCooldown = 0.7f;
    [Min(1f)] [SerializeField] private float companionFollowSpeed = 10f;
    [Min(1)] [SerializeField] private int companionStartingLives = 2;

    [Header("EXPLOSIVE BULLET SETTINGS")]
    [Min(0.1f)] [SerializeField] private float explosionRadiusPerOrangeOrb = 0.75f;
    [Min(0.1f)] [SerializeField] private float maximumExplosionRadius = 4f;

    private float player1NextFireTime;
    private float player2NextFireTime;
    private readonly float[] speedMultipliers = { 1f, 1f };
    private readonly float[] fireRateMultipliers = { 1f, 1f };
    private readonly float[] explosionRadii = { 0f, 0f };
    private bool twoPlayerMode = true;
    private int soloPhysicalPlayer = 1;

    public void ConfigureGameMode(bool useTwoPlayers, int selectedSoloPlayer)
    {
        twoPlayerMode = useTwoPlayers;
        soloPhysicalPlayer = Mathf.Clamp(selectedSoloPlayer, 1, 2);
    }

    public void ApplySpeedBoost(int playerNumber)
    {
        int index = playerNumber - 1;
        if (index >= 0 && index < speedMultipliers.Length)
            speedMultipliers[index] = Mathf.Min(2f, speedMultipliers[index] + 0.25f);
    }

    public void ApplyFireRateBoost(int playerNumber)
    {
        int index = playerNumber - 1;
        if (index >= 0 && index < fireRateMultipliers.Length)
            fireRateMultipliers[index] = Mathf.Min(3f, fireRateMultipliers[index] + 0.35f);
    }

    public void ApplyExplosionBoost(int playerNumber)
    {
        int index = playerNumber - 1;
        if (index >= 0 && index < explosionRadii.Length)
            explosionRadii[index] = Mathf.Min(maximumExplosionRadius,
                explosionRadii[index] + explosionRadiusPerOrangeOrb);
    }

    public void SpawnCompanion(PlayerHealth owner)
    {
        if (owner == null || owner.IsDead || bulletPrefab == null)
            return;

        CompanionShip.Spawn(owner, bulletPrefab, bulletSpeed, bulletDamage, bulletRange,
            companionScale, companionFirstSideDistance, companionExtraSpacing,
            companionVerticalOffset, companionFireCooldown, companionFollowSpeed,
            companionStartingLives, twoPlayerMode ? owner.PlayerNumber : 1,
            explosionRadii[owner.PlayerNumber - 1],
            explosionRadiusPerOrangeOrb, maximumExplosionRadius);
    }

    private bool IsAlive(Transform ship)
    {
        return ship != null && ship.gameObject.activeInHierarchy && ship.GetComponent<PlayerHealth>() != null &&
               !ship.GetComponent<PlayerHealth>().IsDead;
    }

    private void Update()
    {
        if (!twoPlayerMode)
        {
            MoveSoloPlayer();
            ShootSoloPlayer();
            return;
        }

        MovePlayer1();
        MovePlayer2();

        ShootPlayer1();
        ShootPlayer2();
    }

    private void MoveSoloPlayer()
    {
        Transform ship = soloPhysicalPlayer == 1 ? player1 : player2;
        if (!IsAlive(ship) || Keyboard.current == null)
            return;

        float movement = 0f;
        if (Keyboard.current.aKey.isPressed) movement = -1f;
        if (Keyboard.current.dKey.isPressed) movement = 1f;

        Vector3 position = ship.position;
        position.x += movement * moveSpeed * speedMultipliers[soloPhysicalPlayer - 1] * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, leftLimit, rightLimit);
        ship.position = position;
    }

    private void ShootSoloPlayer()
    {
        Transform ship = soloPhysicalPlayer == 1 ? player1 : player2;
        Transform firePoint = soloPhysicalPlayer == 1 ? player1FirePoint : player2FirePoint;
        if (!IsAlive(ship) || firePoint == null || Keyboard.current == null || !Keyboard.current.wKey.isPressed)
            return;

        float nextFire = soloPhysicalPlayer == 1 ? player1NextFireTime : player2NextFireTime;
        if (Time.time < nextFire)
            return;

        nextFire = Time.time + fireCooldown / fireRateMultipliers[soloPhysicalPlayer - 1];
        if (soloPhysicalPlayer == 1) player1NextFireTime = nextFire;
        else player2NextFireTime = nextFire;
        CreateBullet(firePoint, 1, soloPhysicalPlayer - 1);
    }

    private void MovePlayer1()
    {
        if (!IsAlive(player1) || Keyboard.current == null)
            return;

        float movement = 0f;

        if (Keyboard.current.aKey.isPressed)
            movement = -1f;

        if (Keyboard.current.dKey.isPressed)
            movement = 1f;

        Vector3 position = player1.position;

        position.x += movement * moveSpeed * speedMultipliers[0] * Time.deltaTime;

        position.x = Mathf.Clamp(
            position.x,
            leftLimit,
            rightLimit
        );

        player1.position = position;
    }

    private void MovePlayer2()
    {
        if (!IsAlive(player2) || Keyboard.current == null)
            return;

        float movement = 0f;

        if (Keyboard.current.leftArrowKey.isPressed)
            movement = -1f;

        if (Keyboard.current.rightArrowKey.isPressed)
            movement = 1f;

        Vector3 position = player2.position;

        position.x += movement * moveSpeed * speedMultipliers[1] * Time.deltaTime;

        position.x = Mathf.Clamp(
            position.x,
            leftLimit,
            rightLimit
        );

        player2.position = position;
    }

    private void ShootPlayer1()
    {
        if (!IsAlive(player1) || player1FirePoint == null || Keyboard.current == null)
            return;

        if (!Keyboard.current.wKey.isPressed)
            return;

        if (Time.time < player1NextFireTime)
            return;

        player1NextFireTime = Time.time + fireCooldown / fireRateMultipliers[0];

        CreateBullet(player1FirePoint, 1, 0);
    }

    private void ShootPlayer2()
    {
        if (!IsAlive(player2) || player2FirePoint == null || Keyboard.current == null)
            return;

        if (!Keyboard.current.upArrowKey.isPressed)
            return;

        if (Time.time < player2NextFireTime)
            return;

        player2NextFireTime = Time.time + fireCooldown / fireRateMultipliers[1];

        CreateBullet(player2FirePoint, 2, 1);
    }

    private void CreateBullet(Transform firePoint, int playerNumber, int statsIndex)
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
        bullet.OwnerPlayer = playerNumber;
        bullet.ExplosionRadius = explosionRadii[Mathf.Clamp(statsIndex, 0, 1)];
        GameFlow.Instance?.PlayShot();
    }
}
