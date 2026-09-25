using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("================================")]
    [Header("ENEMY PREFABS")]
    [Header("================================")]

    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject bossPrefab;

    [Header("================================")]
    [Header("NORMAL ENEMY STATS")]
    [Header("================================")]

    [SerializeField] private float normalEnemyHealth = 10f;

    [SerializeField] private float normalEnemyBulletDamage = 1f;
    [SerializeField] private float normalEnemyBulletSpeed = 5f;
    [SerializeField] private float normalEnemyBulletRange = 15f;

    [SerializeField] private float normalEnemyMinShootDelay = 1.5f;
    [SerializeField] private float normalEnemyMaxShootDelay = 4f;

    [Header("================================")]
    [Header("BOSS STATS")]
    [Header("================================")]

    [SerializeField] private float bossHealth = 50f;

    [SerializeField] private float bossMovementSpeed = 3f;

    [SerializeField] private float bossBulletDamage = 2f;
    [SerializeField] private float bossBulletSpeed = 6f;
    [SerializeField] private float bossBulletRange = 20f;

    [SerializeField] private float bossMinShootDelay = 0.75f;
    [SerializeField] private float bossMaxShootDelay = 2f;

    [Header("BOSS DASH")]
    [Tooltip("Dash chance on wave 1, in percent per dash check.")]
    [Range(0f, 100f)] [SerializeField] private float bossDashStartingChance = 5f;
    [Tooltip("Extra dash chance added for every new wave.")]
    [Min(0f)] [SerializeField] private float bossDashChanceIncreasePerWave = 2.5f;
    [Range(0f, 100f)] [SerializeField] private float bossDashMaximumChance = 100f;
    [Min(1f)] [SerializeField] private float bossDashSpeed = 22f;
    [Min(0.1f)] [SerializeField] private float bossDashCheckInterval = 1.25f;
    [Min(0f)] [SerializeField] private float bossDashCooldown = 2.5f;

    // New boss bullet and special projectile prefabs
    [SerializeField] private GameObject bossBulletPrefab;
    [SerializeField] private GameObject bossSpecialProjectilePrefab;

    // Special projectile parameters (can be tuned in inspector)
    [SerializeField] private int bossSpecialSpawnCount = 3;
    [SerializeField] private float bossSpecialProjectileSpeed = 4f;
    [SerializeField] private float bossSpecialProjectileDamage = 0f; // damage of special projectile itself (optional)
    [SerializeField] private float bossSpecialProjectileRange = 30f;
    [SerializeField] private float bossSpecialMinDelay = 6f;
    [SerializeField] private float bossSpecialMaxDelay = 12f;

    [Header("================================")]
    [Header("FORMATION")]
    [Header("================================")]

    [SerializeField] private int baseRows = 3;
    [SerializeField] private int baseCols = 6;

    [SerializeField]
    private Vector2 startPosition =
        new Vector2(-6f, 4f);

    [SerializeField] private float horizontalSpacing = 1.6f;
    [SerializeField] private float verticalSpacing = 1.1f;

    [Header("================================")]
    [Header("FORMATION MOVEMENT")]
    [Header("================================")]

    [SerializeField] private float normalMovementSpeed = 1.5f;

    [SerializeField] private float leftBound = -7.5f;
    [SerializeField] private float rightBound = 7.5f;

    [SerializeField] private float descentAmount = 0.6f;

    [Header("================================")]
    [Header("LEVEL SCALING")]
    [Header("================================")]

    [SerializeField] private float enemyCountIncreasePercent = 0.1f;

    [SerializeField] private float bossHealthIncreasePercent = 0.1f;

    [SerializeField] private float enemyMovementSpeedIncreasePercent = 0.05f;
    [SerializeField] private float enemyBulletDamageIncreasePercent = 0.05f;
    [SerializeField] private float enemyFireRateIncreasePercent = 0.05f;

    [SerializeField] private float bossMovementSpeedIncreasePercent = 0.05f;
    [SerializeField] private float bossBulletDamageIncreasePercent = 0.05f;
    [SerializeField] private float bossFireRateIncreasePercent = 0.05f;

    [Header("================================")]
    [Header("GENERAL")]
    [Header("================================")]

    [SerializeField] private Transform formationParent;

    [SerializeField] private GameObject enemyBulletPrefab;

    [SerializeField] private int startingLevel = 1;

    private int currentLevel;

    private Transform formationContainer;

    private int direction = 1;

    private readonly List<Enemy> activeEnemies = new List<Enemy>();

    private Enemy currentBoss;

    private bool bossAlive;

    // Effective values computed per level
    private float effectiveNormalMovementSpeed;
    private float effectiveNormalEnemyBulletDamage;
    private float effectiveNormalEnemyBulletSpeed;
    private float effectiveNormalEnemyBulletRange;
    private float effectiveNormalEnemyMinShootDelay;
    private float effectiveNormalEnemyMaxShootDelay;
    private float effectiveNormalEnemyHealth;

    private float currentBossHealth;
    private float currentBossMovementSpeed;
    private float currentBossBulletDamage;
    private float currentBossBulletSpeed;
    private float currentBossBulletRange;
    private float currentBossMinShootDelay;
    private float currentBossMaxShootDelay;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed;
    }

    private void Start()
    {
        currentLevel = Mathf.Max(1, startingLevel);
        StartLevel();
    }

    private void Update()
    {
        if (formationContainer == null)
            return;

        MoveFormation();
    }

    private void ApplyLevelConfigScaling()
    {
        effectiveNormalMovementSpeed = normalMovementSpeed * (1f + (currentLevel - 1) * enemyMovementSpeedIncreasePercent);

        effectiveNormalEnemyBulletDamage = normalEnemyBulletDamage * (1f + (currentLevel - 1) * enemyBulletDamageIncreasePercent);
        effectiveNormalEnemyBulletSpeed = normalEnemyBulletSpeed;
        effectiveNormalEnemyBulletRange = normalEnemyBulletRange;

        float fireRateScaleEnemy = 1f + (currentLevel - 1) * enemyFireRateIncreasePercent;
        effectiveNormalEnemyMinShootDelay = Mathf.Max(0.05f, normalEnemyMinShootDelay / fireRateScaleEnemy);
        effectiveNormalEnemyMaxShootDelay = Mathf.Max(0.05f, normalEnemyMaxShootDelay / fireRateScaleEnemy);

        float bossFireRateScale = 1f + (currentLevel - 1) * bossFireRateIncreasePercent;
        currentBossBulletDamage = bossBulletDamage * (1f + (currentLevel - 1) * bossBulletDamageIncreasePercent);
        currentBossBulletSpeed = bossBulletSpeed;
        currentBossBulletRange = bossBulletRange;
        currentBossMinShootDelay = Mathf.Max(0.05f, bossMinShootDelay / bossFireRateScale);
        currentBossMaxShootDelay = Mathf.Max(0.05f, bossMaxShootDelay / bossFireRateScale);

        currentBossMovementSpeed = bossMovementSpeed * (1f + (currentLevel - 1) * bossMovementSpeedIncreasePercent);

        // Kleine enemies houden op elk level dezelfde health, zodat ze met de
        // standaard bullet damage altijd een one-shot blijven.
        effectiveNormalEnemyHealth = normalEnemyHealth;

        currentBossHealth = bossHealth * (1f + (currentLevel - 1) * bossHealthIncreasePercent);
    }

    // ============================================
    // FORMATION MOVEMENT
    // ============================================
    private void MoveFormation()
    {
        formationContainer.position += Vector3.right * direction * effectiveNormalMovementSpeed * Time.deltaTime;

        float left = GetFormationLeft();
        float right = GetFormationRight();

        if (direction > 0 && right >= rightBound)
        {
            StepDownAndReverse();
        }
        else if (direction < 0 && left <= leftBound)
        {
            StepDownAndReverse();
        }
    }

    private float GetFormationLeft()
    {
        float left = float.MaxValue;

        foreach (Transform child in formationContainer)
        {
            left = Mathf.Min(left, child.position.x);
        }

        return left;
    }

    private float GetFormationRight()
    {
        float right = float.MinValue;

        foreach (Transform child in formationContainer)
        {
            right = Mathf.Max(right, child.position.x);
        }

        return right;
    }

    private void StepDownAndReverse()
    {
        direction *= -1;
        formationContainer.position += Vector3.down * descentAmount;
    }

    // ============================================
    // START LEVEL
    // ============================================
    private void StartLevel()
    {
        bossAlive = false;
        currentBoss = null;

        ClearExistingFormation();

        ApplyLevelConfigScaling();

        int baseCount = baseRows * baseCols;

        float countMultiplier = 1f + (currentLevel - 1) * enemyCountIncreasePercent;

        int totalEnemies = Mathf.Max(1, Mathf.RoundToInt(baseCount * countMultiplier));

        int cols = baseCols;

        int rows = Mathf.CeilToInt((float)totalEnemies / cols);

        GameObject formationObject = new GameObject("Formation_Level_" + currentLevel);

        formationContainer = formationObject.transform;

        if (formationParent != null)
        {
            formationContainer.SetParent(formationParent, false);
        }

        formationContainer.position = Vector3.zero;

        direction = 1;

        int spawnIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (spawnIndex >= totalEnemies)
                    break;

                GameObject prefab = SelectEnemyPrefab(spawnIndex);

                if (prefab == null)
                    continue;

                Vector3 position = new Vector3(startPosition.x + col * horizontalSpacing,
                                               startPosition.y - row * verticalSpacing,
                                               0f);

                GameObject enemyObject = Instantiate(prefab, position, Quaternion.identity, formationContainer);

                Enemy enemy = enemyObject.GetComponent<Enemy>();
                if (enemy == null)
                {
                    enemy = enemyObject.AddComponent<Enemy>();
                }

                ConfigureNormalEnemy(enemy);
                activeEnemies.Add(enemy);

                spawnIndex++;
            }
        }

        Debug.Log("Level " + currentLevel + " gestart met " + totalEnemies + " enemies.");
    }

    // ============================================
    // NORMAL ENEMY CONFIGURATION
    // ============================================
    private void ConfigureNormalEnemy(Enemy enemy)
    {
        // Use effective health (scaled)
        float finalHealth = effectiveNormalEnemyHealth;

        enemy.InitializeHealth(finalHealth);

        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();

        if (shooter != null)
        {
            Transform firePoint = FindFirePoint(enemy.transform);

            shooter.Configure(
                enemyBulletPrefab,
                firePoint,
                effectiveNormalEnemyBulletSpeed,
                effectiveNormalEnemyBulletDamage,
                effectiveNormalEnemyBulletRange,
                effectiveNormalEnemyMinShootDelay,
                effectiveNormalEnemyMaxShootDelay
            );
        }
    }

    // ============================================
    // BOSS
    // ============================================
    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            NextLevel();
            return;
        }

        bossAlive = true;

        Vector3 spawnPosition = new Vector3((leftBound + rightBound) / 2f, startPosition.y, 0f);

        GameObject bossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);

        currentBoss = bossObject.GetComponent<Enemy>();
        if (currentBoss == null)
        {
            currentBoss = bossObject.AddComponent<Enemy>();
        }

        currentBoss.InitializeHealth(currentBossHealth);

        // Configure boss shooter (use bossBulletPrefab if set, otherwise fallback to enemyBulletPrefab)
        EnemyShooter shooter = bossObject.GetComponent<EnemyShooter>();
        GameObject bulletForBoss = bossBulletPrefab != null ? bossBulletPrefab : enemyBulletPrefab;

        if (shooter != null)
        {
            Transform firePoint = FindFirePoint(bossObject.transform);

            shooter.Configure(
                bulletForBoss,
                firePoint,
                currentBossBulletSpeed,
                currentBossBulletDamage,
                currentBossBulletRange,
                currentBossMinShootDelay,
                currentBossMaxShootDelay
            );
        }

        // Configure boss movement
        BossMovement bossMovement = bossObject.GetComponent<BossMovement>();
        if (bossMovement == null)
        {
            bossMovement = bossObject.AddComponent<BossMovement>();
        }

        float dashChance = Mathf.Min(bossDashMaximumChance,
            bossDashStartingChance + (currentLevel - 1) * bossDashChanceIncreasePerWave);
        bossMovement.Configure(currentBossMovementSpeed, leftBound, rightBound,
            dashChance, bossDashSpeed, bossDashCheckInterval, bossDashCooldown);

    }

    // ============================================
    // ENEMY DESTROYED
    // ============================================
    private void HandleEnemyDestroyed(Enemy enemy)
    {
        if (enemy == currentBoss)
        {
            HandleBossDestroyed();
            return;
        }

        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }

        if (activeEnemies.Count <= 0 && !bossAlive)
        {
            SpawnBoss();
        }
    }

    private void HandleBossDestroyed()
    {
        if (!bossAlive)
            return;

        bossAlive = false;
        currentBoss = null;

        NextLevel();
    }

    // ============================================
    // FIRE POINT
    // ============================================
    private Transform FindFirePoint(Transform enemy)
    {
        Transform firePoint = enemy.Find("FirePoint");

        if (firePoint != null)
            return firePoint;

        return enemy;
    }

    // ============================================
    // PREFAB SELECT
    // ============================================
    private GameObject SelectEnemyPrefab(int index)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("LevelManager: Geen enemy prefabs ingesteld!");
            return null;
        }

        return enemyPrefabs[index % enemyPrefabs.Length];
    }

    // ============================================
    // CLEAR
    // ============================================
    private void ClearExistingFormation()
    {
        activeEnemies.Clear();

        if (formationContainer != null)
        {
            Destroy(formationContainer.gameObject);
            formationContainer = null;
        }
    }

    // ============================================
    // NEXT LEVEL
    // ============================================
    private void NextLevel()
    {
        currentLevel++;
        Debug.Log("Level " + currentLevel + " begint!");
        StartLevel();
    }
}
