using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Space-Invaders style level manager and spawner.
/// Configure enemy prefabs, boss prefab and tuning parameters in the Inspector.
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Enemy prefabs to pick from when spawning formation. Can be empty; a Bullet-compatible Enemy component will be added.")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [Tooltip("Boss prefab spawned at the end of each level.")]
    [SerializeField] private GameObject bossPrefab;

    [Header("Formation (base)")]
    [SerializeField] private int baseRows = 3;
    [SerializeField] private int baseCols = 6;
    [SerializeField] private Vector2 startPosition = new Vector2(-6f, 4f);
    [SerializeField] private float horizontalSpacing = 1.6f;
    [SerializeField] private float verticalSpacing = 1.1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float leftBound = -7.5f;
    [SerializeField] private float rightBound = 7.5f;
    [SerializeField] private float descentAmount = 0.6f;

    [Header("Level scaling")]
    [Tooltip("Percent more enemies per level (0.1 = 10%)")]
    [SerializeField] private float enemyCountIncreasePercent = 0.1f;
    [Tooltip("Percent more health per level (0.1 = 10%)")]
    [SerializeField] private float enemyHealthIncreasePercent = 0.1f;
    [SerializeField] private float baseEnemyHealth = 10f;

    [Header("General")]
    [Tooltip("Parent transform for spawned formations. If null the manager creates a container GameObject.")]
    [SerializeField] private Transform formationParent;

    [Tooltip("Start level index (1 = first level)")]
    [SerializeField] private int startingLevel = 1;

    private int _currentLevel;
    private GameObject _formationContainer;
    private int _direction = 1;
    private List<Enemy> _activeEnemies = new List<Enemy>();
    private bool _bossAlive;

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
        _currentLevel = Mathf.Max(1, startingLevel);
        StartLevel();
    }

    private void Update()
    {
        if (_formationContainer == null) return;

        // move formation horizontally
        var dt = Time.deltaTime;
        _formationContainer.transform.position += Vector3.right * _direction * moveSpeed * dt;

        // check bounds based on container extents
        var left = GetFormationLeft();
        var right = GetFormationRight();

        if (right >= rightBound && _direction > 0)
        {
            StepDownAndReverse();
        }
        else if (left <= leftBound && _direction < 0)
        {
            StepDownAndReverse();
        }
    }

    private float GetFormationLeft()
    {
        return _formationContainer.transform.position.x;
    }

    private float GetFormationRight()
    {
        // approximate width using baseCols and spacing
        var width = (baseCols - 1) * horizontalSpacing;
        return _formationContainer.transform.position.x + width;
    }

    private void StepDownAndReverse()
    {
        _direction *= -1;
        _formationContainer.transform.position += Vector3.down * descentAmount;
    }

    private void StartLevel()
    {
        _bossAlive = false;
        ClearExistingFormation();

        // compute scaled enemy count
        var baseCount = baseRows * baseCols;
        var levelMultiplierCount = 1f + (_currentLevel - 1) * enemyCountIncreasePercent;
        var totalToSpawn = Mathf.Max(1, Mathf.RoundToInt(baseCount * levelMultiplierCount));

        var cols = baseCols;
        var rows = Mathf.CeilToInt((float)totalToSpawn / cols);

        // create container
        _formationContainer = new GameObject($"Formation_Level_{_currentLevel}").transform.gameObject;
        if (formationParent != null)
        {
            _formationContainer.transform.SetParent(formationParent, false);
        }

        _formationContainer.transform.position = startPosition;

        // spawn grid, center formation around startPosition.x
        var spawnIndex = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (spawnIndex >= totalToSpawn) break;

                var prefab = SelectEnemyPrefab(spawnIndex);
                var pos = startPosition + new Vector2(c * horizontalSpacing, -r * verticalSpacing);

                var go = Instantiate(prefab, pos, Quaternion.identity, _formationContainer.transform);

                var enemy = go.GetComponent<Enemy>();
                if (enemy == null)
                {
                    enemy = go.AddComponent<Enemy>();
                }

                // apply scaled health
                var levelHealthMultiplier = 1f + (_currentLevel - 1) * enemyHealthIncreasePercent;
                enemy.InitializeHealth(baseEnemyHealth * levelHealthMultiplier);

                _activeEnemies.Add(enemy);
                spawnIndex++;
            }
        }
    }

    private GameObject SelectEnemyPrefab(int index)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("LevelManager: No enemyPrefabs assigned. Assign at least one enemy prefab in the Inspector.");
            return null;
        }

        return enemyPrefabs[index % enemyPrefabs.Length];
    }

    private void ClearExistingFormation()
    {
        _activeEnemies.Clear();
        if (_formationContainer != null)
        {
            Destroy(_formationContainer);
            _formationContainer = null;
        }
    }

    private void HandleEnemyDestroyed(Enemy enemy)
    {
        if (_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Remove(enemy);
        }

        // if no more regular enemies, spawn boss (if available) or progress immediately
        if (_activeEnemies.Count == 0 && !_bossAlive)
        {
            if (bossPrefab != null)
            {
                SpawnBoss();
            }
            else
            {
                NextLevel();
            }
        }
        else if (_activeEnemies.Count == 0 && _bossAlive == true)
        {
            // waiting for boss to die, do nothing
        }
    }

    private void SpawnBoss()
    {
        _bossAlive = true;

        // boss centered at top of screen (you can change position in inspector)
        var spawnPos = new Vector3((leftBound + rightBound) / 2f, startPosition.y, 0f);
        var bossGO = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

        var bossEnemy = bossGO.GetComponent<Enemy>();
        if (bossEnemy == null)
        {
            bossEnemy = bossGO.AddComponent<Enemy>();
        }

        // boss gets amplified health (example: 2x base * level multiplier)
        var levelHealthMultiplier = 1f + (_currentLevel - 1) * enemyHealthIncreasePercent;
        bossEnemy.InitializeHealth(baseEnemyHealth * 5f * levelHealthMultiplier);

        // subscribe separately so we can detect boss death
        Enemy.OnEnemyDestroyed += HandleBossDestroyed;
    }

    private void HandleBossDestroyed(Enemy boss)
    {
        // boss death handler only; ignore if boss was a normal enemy
        if (!_bossAlive) return;

        // unsubscribe this handler
        Enemy.OnEnemyDestroyed -= HandleBossDestroyed;

        _bossAlive = false;
        NextLevel();
    }

    private void NextLevel()
    {
        _currentLevel++;
        StartLevel();
    }
}