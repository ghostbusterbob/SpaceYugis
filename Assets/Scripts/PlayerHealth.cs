using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int playerNumber = 1;
    [Header("REVIVE")]
    [Min(0f)] [SerializeField] private float reviveInvulnerabilitySeconds = 2f;

    public event Action<PlayerHealth, int> LivesChanged;
    public int CurrentLives { get; private set; }
    public int MaxLives => maxLives;
    public int PlayerNumber => playerNumber;
    public bool IsDead => CurrentLives <= 0;
    public Sprite LifeSprite => sprite != null ? sprite.sprite : null;
    public Color ShipColor => normalColor;
    public int ShieldHits { get; private set; }
    public bool IsInvulnerable => Time.unscaledTime < invulnerableUntil;

    private SpriteRenderer sprite;
    private Color normalColor;
    private Coroutine flashRoutine;
    private Coroutine hideRoutine;
    private LineRenderer shieldRing;
    private float invulnerableUntil;

    private void Awake()
    {
        CurrentLives = Mathf.Max(1, maxLives);
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) normalColor = sprite.color;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f || IsInvulnerable) return;

        if (ShieldHits > 0)
        {
            ShieldHits--;
            UpdateShieldRing();
            GameFlow.Instance?.PlayHit();
            return;
        }

        int lost = Mathf.Min(CurrentLives, Mathf.Max(1, Mathf.RoundToInt(damage)));
        CurrentLives -= lost;
        LivesChanged?.Invoke(this, lost);
        GameFlow.Instance?.PlayHit();

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRed());

        if (IsDead)
        {
            if (shieldRing != null) shieldRing.enabled = false;
            foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
                collider.enabled = false;
            hideRoutine = StartCoroutine(HideAfterFlash());
            GameFlow.Instance?.PlayerDied();
        }
    }

    public void AddLife()
    {
        if (IsDead)
            return;

        CurrentLives++;
        maxLives = Mathf.Max(maxLives, CurrentLives);
        LivesChanged?.Invoke(this, -1);
    }

    public void AddShieldHit()
    {
        if (IsDead)
            return;

        ShieldHits++;
        UpdateShieldRing();
    }

    private void UpdateShieldRing()
    {
        if (shieldRing == null && ShieldHits > 0)
            CreateShieldRing();
        if (shieldRing == null)
            return;

        shieldRing.enabled = ShieldHits > 0 && !IsDead;
        shieldRing.widthMultiplier = 0.055f + Mathf.Min(ShieldHits - 1, 5) * 0.012f;
    }

    private void CreateShieldRing()
    {
        GameObject ringObject = new GameObject("White shield ring");
        ringObject.transform.SetParent(transform, false);
        shieldRing = ringObject.AddComponent<LineRenderer>();
        shieldRing.useWorldSpace = false;
        shieldRing.loop = true;
        shieldRing.positionCount = 48;
        shieldRing.startColor = Color.white;
        shieldRing.endColor = Color.white;
        shieldRing.sortingOrder = sprite != null ? sprite.sortingOrder + 3 : 25;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            shieldRing.material = new Material(shader);
        float radius = sprite != null && sprite.sprite != null
            ? Mathf.Max(sprite.sprite.bounds.extents.x, sprite.sprite.bounds.extents.y) * 1.35f
            : 0.72f;
        for (int i = 0; i < shieldRing.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / shieldRing.positionCount;
            shieldRing.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    public void Revive(int lives)
    {
        if (!IsDead)
            return;

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        CurrentLives = Mathf.Max(1, lives);
        maxLives = Mathf.Max(maxLives, CurrentLives);
        invulnerableUntil = Time.unscaledTime + reviveInvulnerabilitySeconds;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = true;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            renderer.enabled = true;
        if (sprite != null)
            sprite.color = normalColor;
        LivesChanged?.Invoke(this, -1);
        flashRoutine = StartCoroutine(FlashReviveInvulnerability());
    }

    private IEnumerator FlashReviveInvulnerability()
    {
        if (sprite == null)
            yield break;

        bool dimmed = false;
        while (IsInvulnerable)
        {
            dimmed = !dimmed;
            sprite.color = dimmed
                ? new Color(normalColor.r, normalColor.g, normalColor.b, 0.35f)
                : normalColor;
            yield return new WaitForSecondsRealtime(0.1f);
        }
        sprite.color = normalColor;
        flashRoutine = null;
    }

    private IEnumerator FlashRed()
    {
        if (sprite == null) yield break;
        for (int i = 0; i < 4; i++)
        {
            sprite.color = i % 2 == 0 ? Color.red : normalColor;
            yield return new WaitForSecondsRealtime(0.12f);
        }
        sprite.color = normalColor;
    }

    private IEnumerator HideAfterFlash()
    {
        yield return new WaitForSecondsRealtime(0.48f);
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            renderer.enabled = false;
        hideRoutine = null;
    }
}
