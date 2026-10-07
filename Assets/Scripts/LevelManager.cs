using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public int CurrentWave => currentLevel;

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
    [Tooltip("Lowest Y position where a normal enemy may initially spawn.")]
    [SerializeField] private float lowestEnemySpawnY = 1.5f;

    [Header("FORMATION GROWTH")]
    [Min(1)] [SerializeField] private int widerFormationStartWave = 10;
    [Min(1)] [SerializeField] private int wavesPerExtraColumn = 2;
    [Min(1)] [SerializeField] private int maximumFormationColumns = 10;
    [Min(1)] [SerializeField] private int stackedFormationStartWave = 20;
    [Min(0.05f)] [SerializeField] private float reinforcementEntryDuration = 0.45f;
    [Range(0.1f, 1f)] [SerializeField] private float reinforcementStartScale = 0.35f;

    [Header("DYNAMIC CAMERA")]
    [SerializeField] private Camera gameplayCamera;
    [Min(1)] [SerializeField] private int cameraZoomStartEnemyCount = 36;
    [Min(1f)] [SerializeField] private float enemiesPerCameraSizeUnit = 10f;
    [Min(1f)] [SerializeField] private float maximumCameraSize = 10.5f;
    [Min(0.1f)] [SerializeField] private float cameraZoomSpeed = 1.5f;

    [Header("================================")]
    [Header("FORMATION MOVEMENT")]
    [Header("================================")]

    [SerializeField] private float normalMovementSpeed = 1.5f;
    [Min(0.1f)] [SerializeField] private float maximumNormalMovementSpeed = 4.5f;

    [SerializeField] private float leftBound = -7.5f;
    [SerializeField] private float rightBound = 7.5f;

    [SerializeField] private float descentAmount = 0.6f;
    [Tooltip("Minimum total horizontal travel space kept for wide formations.")]
    [Min(0f)] [SerializeField] private float minimumHorizontalTravelDistance = 3f;
    [Tooltip("Prevents rapid repeated drops when a formation is very wide.")]
    [Min(0f)] [SerializeField] private float minimumDescentInterval = 2.5f;
    [Tooltip("Lowest world-space Y an enemy in the formation may reach.")]
    [SerializeField] private float lowestEnemyMovementY = -1f;

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
    private float nextAllowedDescentTime;

    private readonly List<Enemy> activeEnemies = new List<Enemy>();
    private readonly Dictionary<Enemy, int> formationSlotByEnemy =
        new Dictionary<Enemy, int>();
    private readonly Dictionary<int, Vector3> formationSlotPositions =
        new Dictionary<int, Vector3>();
    private readonly Dictionary<int, int> reserveEnemiesBySlot =
        new Dictionary<int, int>();
    private int nextReservePrefabIndex;
    private float baseCameraSize;
    private float targetCameraSize;

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
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (gameplayCamera != null)
        {
            baseCameraSize = gameplayCamera.orthographicSize;
            targetCameraSize = baseCameraSize;
        }
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
        UpdateCameraZoom();
        if (formationContainer == null)
            return;

        MoveFormation();
    }

    private void ApplyLevelConfigScaling()
    {
        effectiveNormalMovementSpeed = Mathf.Min(maximumNormalMovementSpeed,
            normalMovementSpeed * (1f + (currentLevel - 1) * enemyMovementSpeedIncreasePercent));

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
        GetEffectiveHorizontalBounds(out float effectiveLeftBound, out float effectiveRightBound);

        if (direction > 0 && right >= effectiveRightBound)
        {
            formationContainer.position += Vector3.left * Mathf.Max(0f, right - effectiveRightBound);
            ReverseAndMaybeStepDown();
        }
        else if (direction < 0 && left <= effectiveLeftBound)
        {
            formationContainer.position += Vector3.right * Mathf.Max(0f, effectiveLeftBound - left);
            ReverseAndMaybeStepDown();
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

    private float GetFormationBottom()
    {
        float bottom = float.MaxValue;
        foreach (Transform child in formationContainer)
            bottom = Mathf.Min(bottom, child.position.y);
        return bottom;
    }

    private void ReverseAndMaybeStepDown()
    {
        direction *= -1;
        if (Time.time < nextAllowedDescentTime)
            return;

        float safeDescent = Mathf.Max(0f, descentAmount);
        if (GetFormationBottom() - safeDescent < lowestEnemyMovementY)
            return;

        formationContainer.position += Vector3.down * safeDescent;
        nextAllowedDescentTime = Time.time + minimumDescentInterval;
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

        int safeBaseRows = Mathf.Max(1, baseRows);
        int cols = GetFormationColumnCount();

        // Enemy growth remains based on the original formation size, not on the
        // added width. Width creates safer placement instead of multiplying growth.
        int originalBaseCount = safeBaseRows * Mathf.Max(1, baseCols);
        float countMultiplier = 1f + (currentLevel - 1) * enemyCountIncreasePercent;
        int requestedEnemies = Mathf.Max(1, Mathf.RoundToInt(originalBaseCount * countMultiplier));
        SetCameraTargetForEnemyCount(requestedEnemies);

        float safeVerticalSpacing = Mathf.Max(0.1f, verticalSpacing);
        int safeRows = Mathf.Max(1,
            Mathf.FloorToInt((startPosition.y - lowestEnemySpawnY) / safeVerticalSpacing) + 1);
        int slotsPerLayer = Mathf.Max(1, safeRows * cols);
        int initialEnemyCount = Mathf.Min(requestedEnemies, slotsPerLayer);
        int reserveEnemyCount = currentLevel >= stackedFormationStartWave
            ? Mathf.Max(0, requestedEnemies - initialEnemyCount)
            : 0;
        GetEffectiveHorizontalBounds(out float effectiveLeftBound, out float effectiveRightBound);
        float availableWidth = Mathf.Max(0.1f, effectiveRightBound - effectiveLeftBound);
        float reservedTravel = Mathf.Clamp(minimumHorizontalTravelDistance, 0f,
            Mathf.Max(0f, availableWidth - 0.1f));
        float safeHorizontalSpacing = cols <= 1
            ? 0f
            : Mathf.Min(Mathf.Max(0.1f, horizontalSpacing),
                (availableWidth - reservedTravel) / (cols - 1));
        float centeredStartX = (effectiveLeftBound + effectiveRightBound) * 0.5f -
                               (cols - 1) * safeHorizontalSpacing * 0.5f;

        GameObject formationObject = new GameObject("Formation_Level_" + currentLevel);

        formationContainer = formationObject.transform;

        if (formationParent != null)
        {
            formationContainer.SetParent(formationParent, false);
        }

        formationContainer.position = Vector3.zero;

        direction = 1;
        nextAllowedDescentTime = Time.time;

        for (int spawnIndex = 0; spawnIndex < initialEnemyCount; spawnIndex++)
        {
            GameObject prefab = SelectEnemyPrefab(spawnIndex);
            if (prefab == null)
                continue;

            int slotInLayer = spawnIndex;
            int row = slotInLayer / cols;
            int col = slotInLayer % cols;
            Vector3 position = new Vector3(
                centeredStartX + col * safeHorizontalSpacing,
                startPosition.y - row * safeVerticalSpacing,
                0f);

            GameObject enemyObject = Instantiate(prefab, position, Quaternion.identity, formationContainer);
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy == null)
                enemy = enemyObject.AddComponent<Enemy>();

            ConfigureNormalEnemy(enemy);
            activeEnemies.Add(enemy);
            RegisterFormationSlot(slotInLayer, enemy);
            formationSlotPositions[slotInLayer] = enemyObject.transform.localPosition;
        }

        for (int reserveIndex = 0; reserveIndex < reserveEnemyCount; reserveIndex++)
        {
            int slot = reserveIndex % Mathf.Max(1, initialEnemyCount);
            reserveEnemiesBySlot.TryGetValue(slot, out int currentReserves);
            reserveEnemiesBySlot[slot] = currentReserves + 1;
        }
        nextReservePrefabIndex = initialEnemyCount;

        Debug.Log("Level " + currentLevel + " gestart met " + initialEnemyCount +
                  " actieve enemies en " + reserveEnemyCount + " reserves.");
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

        GetEffectiveHorizontalBounds(out float effectiveLeftBound, out float effectiveRightBound);
        Vector3 spawnPosition = new Vector3((effectiveLeftBound + effectiveRightBound) / 2f, startPosition.y, 0f);

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
        bossMovement.Configure(currentBossMovementSpeed, effectiveLeftBound, effectiveRightBound,
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

        SpawnNextReserveForSlot(enemy);

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
        formationSlotByEnemy.Clear();
        formationSlotPositions.Clear();
        reserveEnemiesBySlot.Clear();
        nextReservePrefabIndex = 0;

        if (formationContainer != null)
        {
            Destroy(formationContainer.gameObject);
            formationContainer = null;
        }
    }

    private int GetFormationColumnCount()
    {
        int columns = Mathf.Max(1, baseCols);
        if (currentLevel >= widerFormationStartWave)
        {
            int extraColumns = 1 + (currentLevel - widerFormationStartWave) /
                Mathf.Max(1, wavesPerExtraColumn);
            columns += extraColumns;
        }
        return Mathf.Min(columns, Mathf.Max(1, maximumFormationColumns));
    }

    private void SetCameraTargetForEnemyCount(int enemyCount)
    {
        if (gameplayCamera == null)
            return;
        float extraEnemies = Mathf.Max(0, enemyCount - cameraZoomStartEnemyCount);
        float requestedSize = baseCameraSize + extraEnemies / Mathf.Max(1f, enemiesPerCameraSizeUnit);
        targetCameraSize = Mathf.Clamp(requestedSize, baseCameraSize,
            Mathf.Max(baseCameraSize, maximumCameraSize));
    }

    private void GetEffectiveHorizontalBounds(out float effectiveLeft, out float effectiveRight)
    {
        float center = (leftBound + rightBound) * 0.5f;
        float zoomRatio = baseCameraSize > 0f
            ? Mathf.Max(1f, targetCameraSize / baseCameraSize)
            : 1f;
        effectiveLeft = center + (leftBound - center) * zoomRatio;
        effectiveRight = center + (rightBound - center) * zoomRatio;
    }

    private void UpdateCameraZoom()
    {
        if (gameplayCamera == null || !gameplayCamera.orthographic)
            return;
        gameplayCamera.orthographicSize = Mathf.MoveTowards(gameplayCamera.orthographicSize,
            targetCameraSize, cameraZoomSpeed * Time.unscaledDeltaTime);
    }

    private void RegisterFormationSlot(int slot, Enemy enemy)
    {
        formationSlotByEnemy[enemy] = slot;
    }

    private void SpawnNextReserveForSlot(Enemy destroyedEnemy)
    {
        if (!formationSlotByEnemy.TryGetValue(destroyedEnemy, out int slot))
            return;

        formationSlotByEnemy.Remove(destroyedEnemy);
        if (!reserveEnemiesBySlot.TryGetValue(slot, out int reserveCount) || reserveCount <= 0)
            return;
        if (!formationSlotPositions.TryGetValue(slot, out Vector3 localPosition))
            return;

        reserveEnemiesBySlot[slot] = reserveCount - 1;
        GameObject prefab = SelectEnemyPrefab(nextReservePrefabIndex++);
        if (prefab == null || formationContainer == null)
            return;

        GameObject enemyObject = Instantiate(prefab, formationContainer);
        enemyObject.transform.localPosition = localPosition;
        enemyObject.transform.localRotation = Quaternion.identity;
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy == null)
            enemy = enemyObject.AddComponent<Enemy>();

        ConfigureNormalEnemy(enemy);
        activeEnemies.Add(enemy);
        RegisterFormationSlot(slot, enemy);
        StartCoroutine(ReinforcementEntry(enemy));
    }

    private IEnumerator ReinforcementEntry(Enemy enemy)
    {
        if (enemy == null)
            yield break;

        foreach (Collider2D collider in enemy.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();
        if (shooter != null)
            shooter.enabled = false;

        Transform enemyTransform = enemy.transform;
        Vector3 finalScale = enemyTransform.localScale;
        enemyTransform.localScale = finalScale * reinforcementStartScale;
        SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>();
        Color[] finalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            finalColors[i] = renderers[i].color;
            Color hidden = finalColors[i];
            hidden.a = 0f;
            renderers[i].color = hidden;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, reinforcementEntryDuration);
        while (elapsed < duration)
        {
            if (enemy == null)
                yield break;
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            enemyTransform.localScale = Vector3.Lerp(finalScale * reinforcementStartScale, finalScale, progress);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color color = finalColors[i];
                color.a *= progress;
                renderers[i].color = color;
            }
            yield return null;
        }

        if (enemy == null)
            yield break;
        enemyTransform.localScale = finalScale;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = finalColors[i];
        foreach (Collider2D collider in enemy.GetComponentsInChildren<Collider2D>())
            collider.enabled = true;
        if (shooter != null)
            shooter.enabled = true;
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
