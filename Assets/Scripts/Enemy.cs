using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static event Action<Enemy> OnEnemyDestroyed;

    private float currentHealth;
    private float maxHealth;
    private bool isDead;
    private Coroutine impactRoutine;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public int LastHitPlayer { get; private set; }

    public void InitializeHealth(float health)
    {
        maxHealth = Mathf.Max(1f, health);
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, 0);
    }

    public void TakeDamage(float damage, int playerNumber)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        if (playerNumber == 1 || playerNumber == 2)
            LastHitPlayer = playerNumber;

        Debug.Log(
            gameObject.name +
            " geraakt voor " +
            damage +
            " damage. HP: " +
            currentHealth
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            GameFlow.Instance?.EnemyHit(transform.position);
            if (impactRoutine != null)
                StopCoroutine(impactRoutine);
            impactRoutine = StartCoroutine(ImpactShake());
        }
    }

    private IEnumerator ImpactShake()
    {
        Vector3 origin = transform.localPosition;
        const float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(0.08f, 0f, elapsed / duration);
            transform.localPosition = origin + (Vector3)UnityEngine.Random.insideUnitCircle * strength;
            yield return null;
        }

        transform.localPosition = origin;
        impactRoutine = null;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GameFlow.Instance?.EnemyExplosion(transform.position);

        OnEnemyDestroyed?.Invoke(this);

        Destroy(gameObject);
    }
}
