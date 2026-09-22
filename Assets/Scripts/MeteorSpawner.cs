

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeteorSpawner : MonoBehaviour
{
    [Header("Meteor")]
    [SerializeField] private GameObject meteorPrefab;
    [Tooltip("Seconds between each chance roll (default 10s).")]
    [SerializeField] private float spawnInterval = 10f;

    [Header("Chance")]
    [Tooltip("Numerator for chance. Default 1 (1 in denominator).")]
    [SerializeField] private int chanceNumerator = 1;
    [Tooltip("Denominator for chance. Default 3 means 1 in 3 chance.")]
    [SerializeField] private int chanceDenominator = 3;

    [Header("Spawn Points (prefer these)")]
    [Tooltip("Assign up to three Transforms where meteors can spawn. If none assigned, fallback to the random area below.")]
    [SerializeField] private Transform[] spawnPoints = new Transform[3];

    [Header("Fallback Spawn Area (used when no spawn points assigned)")]
    [Tooltip("Y position used when spawning from the fallback area.")]
    [SerializeField] private float fallbackSpawnY = 10f;
    [Tooltip("X range (min, max) for fallback spawn position.")]
    [SerializeField] private Vector2 fallbackSpawnXRange = new Vector2(-8f, 8f);

    [Header("Cleanup")]
    [Tooltip("Destroy meteor automatically after this many seconds if it doesn't hit anything.")]
    [SerializeField] private float meteorLifetime = 30f;

    private void Start()
    {
        if (meteorPrefab == null)
        {
            Debug.LogError("[MeteorSpawner] meteorPrefab is not assigned.", this);
            enabled = false;
            return;
        }

        if (chanceDenominator <= 0) chanceDenominator = 1;
        if (chanceNumerator < 0) chanceNumerator = 0;
        if (chanceNumerator > chanceDenominator) chanceNumerator = chanceDenominator;

        // Normalize spawnPoints array length to 3 (inspector will show 3 slots).
        if (spawnPoints == null || spawnPoints.Length != 3)
            spawnPoints = new Transform[3];

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            int roll = Random.Range(1, chanceDenominator + 1);
            if (roll <= chanceNumerator)
            {
                Vector3 spawnPos;

                // Collect non-null spawn points
                var validPoints = new List<Transform>();
                foreach (var t in spawnPoints)
                    if (t != null) validPoints.Add(t);

                if (validPoints.Count > 0)
                {
                    var chosen = validPoints[Random.Range(0, validPoints.Count)];
                    spawnPos = chosen.position;
                }
                else
                {
                    // Fallback: spawn at random x within range and fixed Y
                    spawnPos = new Vector3(
                        Random.Range(fallbackSpawnXRange.x, fallbackSpawnXRange.y),
                        fallbackSpawnY,
                        0f
                    );
                }

                GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
                Destroy(meteor, meteorLifetime);
            }
        }
    }
}