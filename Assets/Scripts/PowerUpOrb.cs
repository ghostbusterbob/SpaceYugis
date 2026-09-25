using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class PowerUpOrb : MonoBehaviour
{
    public enum Kind { Speed, FireRate, Life, Rainbow, Companion, Revive, Galaxy, Explosive, Shield }

    private static Sprite orbSprite;
    private Kind kind;
    private float fallSpeed;
    private SpriteRenderer spriteRenderer;
    private float age;
    private float rotationSpeed;
    private float rainbowCycleSpeed;
    private float reviveSideSpeed;
    private float reviveFrequency;
    private float reviveSpinSpeed;
    private float reviveFlashSpeed;
    private Transform galaxyParticles;

    public static void Spawn(Vector3 position, Kind type, float speed, float normalRotation,
        float rainbowSpeed, float reviveHorizontal, float reviveWeave, float reviveRotation,
        float reviveBlink)
    {
        GameObject orb = new GameObject(type + " orb");
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * 0.55f;

        PowerUpOrb powerUp = orb.AddComponent<PowerUpOrb>();
        powerUp.kind = type;
        powerUp.fallSpeed = Mathf.Max(0.1f, speed);
        powerUp.rotationSpeed = normalRotation;
        powerUp.rainbowCycleSpeed = rainbowSpeed;
        powerUp.reviveSideSpeed = reviveHorizontal;
        powerUp.reviveFrequency = reviveWeave;
        powerUp.reviveSpinSpeed = reviveRotation;
        powerUp.reviveFlashSpeed = reviveBlink;

        SpriteRenderer renderer = orb.GetComponent<SpriteRenderer>();
        powerUp.spriteRenderer = renderer;
        renderer.sprite = GetOrbSprite();
        renderer.color = type == Kind.Speed ? new Color(0.15f, 0.55f, 1f) :
            type == Kind.FireRate ? new Color(1f, 0.18f, 0.12f) :
            type == Kind.Life ? new Color(0.2f, 1f, 0.35f) :
            type == Kind.Companion ? new Color(1f, 0.85f, 0.08f) :
            type == Kind.Revive ? new Color(0.01f, 0.28f, 0.04f) : Color.white;
        if (type == Kind.Galaxy)
            renderer.color = new Color(0.55f, 0.18f, 1f);
        else if (type == Kind.Explosive)
            renderer.color = new Color(1f, 0.34f, 0.03f);
        else if (type == Kind.Shield)
            renderer.color = Color.white;
        renderer.sortingOrder = 8;

        if (type == Kind.Galaxy)
            powerUp.CreateGalaxyParticles();

        CircleCollider2D collider = orb.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        Rigidbody2D body = orb.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (kind == Kind.Revive)
        {
            Vector3 movement = new Vector3(Mathf.Sin(age * reviveFrequency) * reviveSideSpeed, -fallSpeed, 0f);
            transform.position += movement * Time.deltaTime;
            transform.Rotate(0f, 0f, reviveSpinSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
        float pulse = 0.52f + Mathf.Sin(Time.time * 7f) * 0.06f;
        transform.localScale = Vector3.one * pulse;

        if (kind == Kind.Rainbow && spriteRenderer != null)
            spriteRenderer.color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * rainbowCycleSpeed, 1f), 0.85f, 1f);
        else if (kind == Kind.Galaxy && spriteRenderer != null)
        {
            float cycle = Mathf.Repeat(Time.unscaledTime * rainbowCycleSpeed, 1f) * 3f;
            Color darkBlue = new Color(0.025f, 0.07f, 0.38f);
            Color darkPurple = new Color(0.22f, 0.015f, 0.42f);
            Color magenta = new Color(0.72f, 0.015f, 0.5f);
            spriteRenderer.color = cycle < 1f
                ? Color.Lerp(darkBlue, darkPurple, cycle)
                : cycle < 2f
                    ? Color.Lerp(darkPurple, magenta, cycle - 1f)
                    : Color.Lerp(magenta, darkBlue, cycle - 2f);
            if (galaxyParticles != null)
                galaxyParticles.Rotate(0f, 0f, 150f * Time.deltaTime);
        }
        else if (kind == Kind.Revive && spriteRenderer != null)
        {
            float blink = Mathf.Sin(Time.unscaledTime * reviveFlashSpeed) > 0f ? 1f : 0.36f;
            spriteRenderer.color = new Color(0.02f, 0.75f * blink, 0.08f, 1f);
        }

        Camera camera = Camera.main;
        if (camera != null && transform.position.y < camera.ViewportToWorldPoint(Vector3.zero).y - 1f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && !player.IsDead)
        {
            GameFlow.Instance?.ApplyPowerUp(player, kind);
            Destroy(gameObject);
            return;
        }

        CompanionShip companion = other.GetComponentInParent<CompanionShip>();
        if (companion != null && !companion.IsDead)
        {
            GameFlow.Instance?.ApplyPowerUp(companion, kind);
            Destroy(gameObject);
        }
    }

    private static Sprite GetOrbSprite()
    {
        if (orbSprite != null)
            return orbSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Power-up orb";
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = Vector2.one * (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
            float alpha = Mathf.Clamp01((1f - distance) * 5f);
            float glow = Mathf.Clamp01(1.15f - distance);
            texture.SetPixel(x, y, new Color(0.65f + glow * 0.35f, 0.65f + glow * 0.35f, 0.65f + glow * 0.35f, alpha));
        }
        texture.Apply();
        orbSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 32f);
        orbSprite.name = "Power-up orb";
        return orbSprite;
    }

    private void CreateGalaxyParticles()
    {
        GameObject root = new GameObject("Galaxy white particles");
        root.transform.SetParent(transform, false);
        galaxyParticles = root.transform;
        for (int i = 0; i < 9; i++)
        {
            GameObject particle = new GameObject("White particle " + (i + 1));
            particle.transform.SetParent(galaxyParticles, false);
            float angle = i * Mathf.PI * 2f / 9f;
            float radius = i % 2 == 0 ? 0.78f : 0.62f;
            particle.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            particle.transform.localScale = Vector3.one * (i % 3 == 0 ? 0.15f : 0.1f);
            SpriteRenderer particleRenderer = particle.AddComponent<SpriteRenderer>();
            particleRenderer.sprite = GetOrbSprite();
            particleRenderer.color = Color.white;
            particleRenderer.sortingOrder = 9;
        }
    }
}
