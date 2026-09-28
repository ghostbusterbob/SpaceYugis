using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance { get; private set; }

    [SerializeField] private Battleship battleship;
    [SerializeField] private LevelManager levels;
    [SerializeField] private MeteorSpawner meteors;
    [SerializeField] private PlayerHealth player1;
    [SerializeField] private PlayerHealth player2;
    [Header("Arcade UI")]
    [SerializeField] private Font arcadeFont;

    [Header("ORB SPAWN TIMING")]
    [Tooltip("Minimum seconds before the next orb can spawn.")]
    [Min(1f)] [SerializeField] private float minimumPowerUpInterval = 7f;
    [Tooltip("Maximum seconds before the next orb spawns.")]
    [Min(1f)] [SerializeField] private float maximumPowerUpInterval = 13f;

    [Header("ORB POSITION AND SPEED")]
    [SerializeField] private Vector2 orbSpawnXRange = new Vector2(-6.8f, 6.8f);
    [Min(0f)] [SerializeField] private float orbSpawnAboveScreen = 0.5f;
    [SerializeField] private Vector2 orbFallSpeedRange = new Vector2(1.4f, 2.2f);

    [Header("ORB FALL SPEED MULTIPLIERS")]
    [Min(0.1f)] [SerializeField] private float blueOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float redOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float greenOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float yellowOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float galaxyOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float orangeOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float whiteOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float purpleOrbSpeed = 1f;
    [Min(0.1f)] [SerializeField] private float rainbowOrbSpeed = 1.15f;
    [Min(0.1f)] [SerializeField] private float reviveOrbSpeed = 2.25f;

    [Header("ORB ANIMATION SPEEDS")]
    [Min(0f)] [SerializeField] private float normalOrbRotationSpeed = 100f;
    [Min(0f)] [SerializeField] private float rainbowColourCycleSpeed = 0.45f;
    [Min(0f)] [SerializeField] private float reviveHorizontalSpeed = 4.5f;
    [Min(0f)] [SerializeField] private float reviveWeaveFrequency = 8f;
    [Min(0f)] [SerializeField] private float reviveRotationSpeed = 620f;
    [Min(0f)] [SerializeField] private float reviveBlinkSpeed = 18f;

    [Header("ORB TYPE SPAWN WEIGHTS")]
    [Tooltip("Blue movement-speed orb chance.")]
    [Range(0f, 100f)] [SerializeField] private float blueSpeedChance = 37f;
    [Tooltip("Red fire-rate orb chance.")]
    [Range(0f, 100f)] [SerializeField] private float redFireRateChance = 30f;
    [Tooltip("Green extra-life orb chance.")]
    [Range(0f, 100f)] [SerializeField] private float greenLifeChance = 18f;
    [Tooltip("Yellow companion-ship orb chance.")]
    [Range(0f, 100f)] [SerializeField] private float yellowCompanionChance = 12f;
    [Tooltip("Dark-green blinking orb that revives the other player.")]
    [Range(0f, 100f)] [SerializeField] private float darkGreenReviveChance = 1f;
    [Tooltip("Galaxy orb weight. Default 1; grants a configurable orb-drop multiplier.")]
    [Range(0f, 100f)] [SerializeField] private float galaxyOrbChance = 1f;
    [Tooltip("Orange explosive-bullet orb weight. Default 2; extra pickups increase blast range.")]
    [Range(0f, 100f)] [SerializeField] private float orangeExplosiveChance = 2f;
    [Tooltip("White shield-orb spawn weight. Each pickup blocks one extra hit.")]
    [Range(0f, 100f)] [SerializeField] private float whiteShieldChance = 5f;
    [Tooltip("Purple piercing-orb spawn weight. Each pickup adds one enemy pierce per bullet.")]
    [Range(0f, 100f)] [SerializeField] private float purplePierceChance = 5f;
    [Tooltip("Rainbow all-perks orb chance.")]
    [Range(0f, 100f)] [SerializeField] private float rainbowChance = 3f;

    [Header("GALAXY DROP MULTIPLIER WEIGHTS")]
    [Min(0f)] [SerializeField] private float galaxy2xWeight = 60f;
    [Min(0f)] [SerializeField] private float galaxy3xWeight = 20f;
    [Min(0f)] [SerializeField] private float galaxy5xWeight = 10f;
    [Min(0.1f)] [SerializeField] private float galaxyMultiplierDuration = 30f;

    [Header("AUDIO VOLUMES")]
    [Range(0f, 1f)] [SerializeField] private float menuMusicVolume = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float gameplayMusicVolume = 0.16f;
    [Range(0f, 1f)] [SerializeField] private float soundEffectsVolume = 1f;

    private enum ScreenState { MainMenu, ModeSelect, ShipSelect, Ready, Playing, GameOver }
    private ScreenState state = ScreenState.MainMenu;
    private bool twoPlayerMode = true;
    private PlayerHealth soloPlayer;
    private string selectedShipName = "BLACK SHIP";
    private readonly int[] scores = new int[2];
    private readonly int[] kills = new int[2];
    private readonly int[] hits = new int[2];
    private readonly float[] lostFlashUntil = new float[2];
    private readonly int[] flashingLife = new int[2];
    private int highScore;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle centeredStyle;
    private GUIStyle rightStyle;
    private GUIStyle buttonStyle;
    private GUIStyle statStyle;
    private AudioSource audioSource;
    private AudioSource musicSource;
    private AudioClip shotSound;
    private AudioClip hitSound;
    private AudioClip explosionSound;
    private AudioClip enemyDeathSound;
    private AudioClip enemyHitSound;
    private AudioClip pickupSound;
    private AudioClip menuMusic;
    private AudioClip gameMusic;
    private Material particleMaterial;
    private float nextPowerUpTime;
    private int orbDropMultiplier = 1;
    private readonly List<GalaxyDropBoost> activeGalaxyBoosts = new List<GalaxyDropBoost>();

    private sealed class GalaxyDropBoost
    {
        public int Multiplier;
        public float ExpiresAt;
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (battleship != null) battleship.enabled = false;
        if (levels != null) levels.enabled = false;
        if (meteors != null) meteors.enabled = false;
        highScore = PlayerPrefs.GetInt("SpaceYugisCombinedHighScore", 0);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        shotSound = MakeSound(0.09f, 780f, 450f, false);
        hitSound = MakeSound(0.22f, 290f, 90f, true);
        explosionSound = MakeSound(0.28f, 190f, 45f, true);
        enemyDeathSound = MakeBloodSpatterSound();
        enemyHitSound = MakeSound(0.08f, 520f, 260f, true);
        pickupSound = MakeSound(0.18f, 480f, 960f, false);
        menuMusic = MakeMusic(true);
        gameMusic = MakeMusic(false);
        SwitchMusic(menuMusic);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null) particleMaterial = new Material(shader);
    }

    private void OnEnable()
    {
        Enemy.OnEnemyDestroyed += EnemyDestroyed;
        if (player1 != null) player1.LivesChanged += LifeLost;
        if (player2 != null) player2.LivesChanged += LifeLost;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDestroyed -= EnemyDestroyed;
        if (player1 != null) player1.LivesChanged -= LifeLost;
        if (player2 != null) player2.LivesChanged -= LifeLost;
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        if (shotSound != null) Destroy(shotSound);
        if (hitSound != null) Destroy(hitSound);
        if (explosionSound != null) Destroy(explosionSound);
        if (enemyDeathSound != null) Destroy(enemyDeathSound);
        if (enemyHitSound != null) Destroy(enemyHitSound);
        if (pickupSound != null) Destroy(pickupSound);
        if (menuMusic != null) Destroy(menuMusic);
        if (gameMusic != null) Destroy(gameMusic);
        if (particleMaterial != null) Destroy(particleMaterial);
    }

    private void StartGame()
    {
        if (!twoPlayerMode && soloPlayer == null)
            return;

        if (player1 != null) player1.gameObject.SetActive(twoPlayerMode || soloPlayer == player1);
        if (player2 != null) player2.gameObject.SetActive(twoPlayerMode || soloPlayer == player2);
        if (battleship != null)
            battleship.ConfigureGameMode(twoPlayerMode, soloPlayer != null ? soloPlayer.PlayerNumber : 1);
        state = ScreenState.Playing;
        Time.timeScale = 1f;
        if (battleship != null) battleship.enabled = true;
        if (levels != null) levels.enabled = true;
        if (meteors != null) meteors.enabled = true;
        SwitchMusic(gameMusic);
        SchedulePowerUp();
    }

    private void Update()
    {
        UpdateGalaxyBoosts();
        if (state == ScreenState.Playing && Time.time >= nextPowerUpTime)
        {
            SpawnPowerUp();
            SchedulePowerUp();
        }
    }

    private void SchedulePowerUp()
    {
        float low = Mathf.Min(minimumPowerUpInterval, maximumPowerUpInterval);
        float high = Mathf.Max(minimumPowerUpInterval, maximumPowerUpInterval);
        nextPowerUpTime = Time.time + Random.Range(low, high);
    }

    private void SpawnPowerUp()
    {
        Camera camera = Camera.main;
        float y = camera != null ? camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, -camera.transform.position.z)).y + orbSpawnAboveScreen : 6f;
        int dropCount = Mathf.Max(1, orbDropMultiplier);
        for (int i = 0; i < dropCount; i++)
        {
            float x = Random.Range(Mathf.Min(orbSpawnXRange.x, orbSpawnXRange.y), Mathf.Max(orbSpawnXRange.x, orbSpawnXRange.y));
            PowerUpOrb.Kind type = SelectPowerUp();
            float fallSpeed = Random.Range(Mathf.Min(orbFallSpeedRange.x, orbFallSpeedRange.y), Mathf.Max(orbFallSpeedRange.x, orbFallSpeedRange.y));
            fallSpeed *= GetOrbSpeedMultiplier(type);
            PowerUpOrb.Spawn(new Vector3(x, y + i * 0.12f, 0f), type, fallSpeed, normalOrbRotationSpeed,
                rainbowColourCycleSpeed, reviveHorizontalSpeed, reviveWeaveFrequency,
                reviveRotationSpeed, reviveBlinkSpeed);
        }
    }

    private float GetOrbSpeedMultiplier(PowerUpOrb.Kind type)
    {
        switch (type)
        {
            case PowerUpOrb.Kind.Speed: return blueOrbSpeed;
            case PowerUpOrb.Kind.FireRate: return redOrbSpeed;
            case PowerUpOrb.Kind.Life: return greenOrbSpeed;
            case PowerUpOrb.Kind.Companion: return yellowOrbSpeed;
            case PowerUpOrb.Kind.Galaxy: return galaxyOrbSpeed;
            case PowerUpOrb.Kind.Explosive: return orangeOrbSpeed;
            case PowerUpOrb.Kind.Shield: return whiteOrbSpeed;
            case PowerUpOrb.Kind.Pierce: return purpleOrbSpeed;
            case PowerUpOrb.Kind.Rainbow: return rainbowOrbSpeed;
            case PowerUpOrb.Kind.Revive: return reviveOrbSpeed;
            default: return 1f;
        }
    }

    private PowerUpOrb.Kind SelectPowerUp()
    {
        float rareRoll = Random.value * 100f;
        if (rareRoll < galaxyOrbChance)
            return PowerUpOrb.Kind.Galaxy;
        if (rareRoll < Mathf.Min(100f, galaxyOrbChance + orangeExplosiveChance))
            return PowerUpOrb.Kind.Explosive;

        float reviveChance = CanSpawnReviveOrb() ? darkGreenReviveChance : 0f;
        float total = blueSpeedChance + redFireRateChance + greenLifeChance + yellowCompanionChance +
                      reviveChance + whiteShieldChance + purplePierceChance + rainbowChance;
        if (total <= 0f)
            return PowerUpOrb.Kind.Speed;

        float roll = Random.value * total;
        if ((roll -= blueSpeedChance) < 0f) return PowerUpOrb.Kind.Speed;
        if ((roll -= redFireRateChance) < 0f) return PowerUpOrb.Kind.FireRate;
        if ((roll -= greenLifeChance) < 0f) return PowerUpOrb.Kind.Life;
        if ((roll -= yellowCompanionChance) < 0f) return PowerUpOrb.Kind.Companion;
        if (CanSpawnReviveOrb() && (roll -= reviveChance) < 0f) return PowerUpOrb.Kind.Revive;
        if ((roll -= whiteShieldChance) < 0f) return PowerUpOrb.Kind.Shield;
        if ((roll -= purplePierceChance) < 0f) return PowerUpOrb.Kind.Pierce;
        return PowerUpOrb.Kind.Rainbow;
    }

    private bool CanSpawnReviveOrb()
    {
        if (!twoPlayerMode || player1 == null || player2 == null)
            return false;
        return player1.IsDead != player2.IsDead;
    }

    private void OnValidate()
    {
        minimumPowerUpInterval = Mathf.Max(1f, minimumPowerUpInterval);
        maximumPowerUpInterval = Mathf.Max(minimumPowerUpInterval, maximumPowerUpInterval);
        blueSpeedChance = Mathf.Max(0f, blueSpeedChance);
        redFireRateChance = Mathf.Max(0f, redFireRateChance);
        greenLifeChance = Mathf.Max(0f, greenLifeChance);
        yellowCompanionChance = Mathf.Max(0f, yellowCompanionChance);
        darkGreenReviveChance = Mathf.Max(0f, darkGreenReviveChance);
        galaxyOrbChance = Mathf.Max(0f, galaxyOrbChance);
        orangeExplosiveChance = Mathf.Max(0f, orangeExplosiveChance);
        whiteShieldChance = Mathf.Max(0f, whiteShieldChance);
        purplePierceChance = Mathf.Max(0f, purplePierceChance);
        rainbowChance = Mathf.Max(0f, rainbowChance);
        galaxy2xWeight = Mathf.Max(0f, galaxy2xWeight);
        galaxy3xWeight = Mathf.Max(0f, galaxy3xWeight);
        galaxy5xWeight = Mathf.Max(0f, galaxy5xWeight);
        galaxyMultiplierDuration = Mathf.Max(0.1f, galaxyMultiplierDuration);
        orbSpawnAboveScreen = Mathf.Max(0f, orbSpawnAboveScreen);
        orbFallSpeedRange.x = Mathf.Max(0.1f, orbFallSpeedRange.x);
        orbFallSpeedRange.y = Mathf.Max(orbFallSpeedRange.x, orbFallSpeedRange.y);
        blueOrbSpeed = Mathf.Max(0.1f, blueOrbSpeed);
        redOrbSpeed = Mathf.Max(0.1f, redOrbSpeed);
        greenOrbSpeed = Mathf.Max(0.1f, greenOrbSpeed);
        yellowOrbSpeed = Mathf.Max(0.1f, yellowOrbSpeed);
        galaxyOrbSpeed = Mathf.Max(0.1f, galaxyOrbSpeed);
        orangeOrbSpeed = Mathf.Max(0.1f, orangeOrbSpeed);
        whiteOrbSpeed = Mathf.Max(0.1f, whiteOrbSpeed);
        purpleOrbSpeed = Mathf.Max(0.1f, purpleOrbSpeed);
        rainbowOrbSpeed = Mathf.Max(0.1f, rainbowOrbSpeed);
        reviveOrbSpeed = Mathf.Max(0.1f, reviveOrbSpeed);
    }

    public void PlayerDied()
    {
        if (state != ScreenState.Playing)
            return;

        bool gameIsOver = twoPlayerMode
            ? player1 != null && player2 != null && player1.IsDead && player2.IsDead
            : soloPlayer != null && soloPlayer.IsDead;
        if (!gameIsOver)
            return;

        state = ScreenState.GameOver;
        Time.timeScale = 0f;
        SwitchMusic(menuMusic);
        if (battleship != null) battleship.enabled = false;
        int combined = twoPlayerMode ? scores[0] + scores[1] : scores[0];
        if (combined > highScore)
        {
            highScore = combined;
            PlayerPrefs.SetInt("SpaceYugisCombinedHighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    private void EnemyDestroyed(Enemy enemy)
    {
        if (state != ScreenState.Playing || enemy == null) return;
        int index = twoPlayerMode ? enemy.LastHitPlayer - 1 : 0;
        if (index < 0 || index > 1) return;
        scores[index] += 100;
        kills[index]++;
    }

    private void LifeLost(PlayerHealth player, int count)
    {
        int index = twoPlayerMode ? player.PlayerNumber - 1 : 0;
        if (index < 0 || index > 1) return;
        if (count <= 0)
            return;
        hits[index]++;
        flashingLife[index] = player.CurrentLives;
        lostFlashUntil[index] = Time.unscaledTime + 0.65f;
    }

    public void PlayShot()
    {
        if (audioSource != null) audioSource.PlayOneShot(shotSound, 0.24f * soundEffectsVolume);
    }

    public void PlayHit()
    {
        if (audioSource != null) audioSource.PlayOneShot(hitSound, 0.65f * soundEffectsVolume);
    }

    public void PlayPickup()
    {
        if (audioSource != null) audioSource.PlayOneShot(pickupSound, 0.45f * soundEffectsVolume);
    }

    public void ApplyPowerUp(PlayerHealth player, PowerUpOrb.Kind kind)
    {
        if (player == null || state != ScreenState.Playing)
            return;

        if (kind == PowerUpOrb.Kind.Speed)
            battleship?.ApplySpeedBoost(player.PlayerNumber);
        else if (kind == PowerUpOrb.Kind.FireRate)
            battleship?.ApplyFireRateBoost(player.PlayerNumber);
        else if (kind == PowerUpOrb.Kind.Life)
            player.AddLife();
        else if (kind == PowerUpOrb.Kind.Companion)
            battleship?.SpawnCompanion(player);
        else if (kind == PowerUpOrb.Kind.Revive)
            ReviveOtherPlayer(player);
        else if (kind == PowerUpOrb.Kind.Galaxy)
            ApplyGalaxyDropMultiplier();
        else if (kind == PowerUpOrb.Kind.Explosive)
            battleship?.ApplyExplosionBoost(player.PlayerNumber);
        else if (kind == PowerUpOrb.Kind.Shield)
            player.AddShieldHit();
        else if (kind == PowerUpOrb.Kind.Pierce)
            battleship?.ApplyPierceBoost(player.PlayerNumber);
        else
        {
            battleship?.ApplySpeedBoost(player.PlayerNumber);
            battleship?.ApplyFireRateBoost(player.PlayerNumber);
            player.AddLife();
            battleship?.SpawnCompanion(player);
        }

        PlayPickup();
    }

    public void ApplyPowerUp(CompanionShip companion, PowerUpOrb.Kind kind)
    {
        if (companion == null || state != ScreenState.Playing)
            return;

        if (kind == PowerUpOrb.Kind.Speed)
            companion.ApplySpeedBoost();
        else if (kind == PowerUpOrb.Kind.FireRate)
            companion.ApplyFireRateBoost();
        else if (kind == PowerUpOrb.Kind.Life)
            companion.AddLife();
        else if (kind == PowerUpOrb.Kind.Companion)
            battleship?.SpawnCompanion(companion.Owner);
        else if (kind == PowerUpOrb.Kind.Revive)
            ReviveOtherPlayer(companion.Owner);
        else if (kind == PowerUpOrb.Kind.Galaxy)
            ApplyGalaxyDropMultiplier();
        else if (kind == PowerUpOrb.Kind.Explosive)
            companion.ApplyExplosionBoost();
        else if (kind == PowerUpOrb.Kind.Shield)
            companion.AddShieldHit();
        else if (kind == PowerUpOrb.Kind.Pierce)
            companion.ApplyPierceBoost();
        else
        {
            companion.ApplySpeedBoost();
            companion.ApplyFireRateBoost();
            companion.AddLife();
            battleship?.SpawnCompanion(companion.Owner);
        }

        PlayPickup();
    }

    private void ApplyGalaxyDropMultiplier()
    {
        float total = galaxy2xWeight + galaxy3xWeight + galaxy5xWeight;
        int awardedMultiplier;
        if (total <= 0f)
            awardedMultiplier = 2;
        else
        {
            float roll = Random.value * total;
            if ((roll -= galaxy2xWeight) < 0f) awardedMultiplier = 2;
            else if ((roll -= galaxy3xWeight) < 0f) awardedMultiplier = 3;
            else awardedMultiplier = 5;
        }

        activeGalaxyBoosts.Add(new GalaxyDropBoost
        {
            Multiplier = awardedMultiplier,
            ExpiresAt = Time.time + galaxyMultiplierDuration
        });
        RecalculateGalaxyMultiplier();
    }

    private void UpdateGalaxyBoosts()
    {
        bool changed = false;
        for (int i = activeGalaxyBoosts.Count - 1; i >= 0; i--)
        {
            if (Time.time < activeGalaxyBoosts[i].ExpiresAt)
                continue;
            activeGalaxyBoosts.RemoveAt(i);
            changed = true;
        }
        if (changed)
            RecalculateGalaxyMultiplier();
    }

    private void RecalculateGalaxyMultiplier()
    {
        if (activeGalaxyBoosts.Count == 0)
        {
            orbDropMultiplier = 1;
            return;
        }

        int total = 0;
        foreach (GalaxyDropBoost boost in activeGalaxyBoosts)
            total += boost.Multiplier;
        orbDropMultiplier = Mathf.Max(1, total);
    }

    private void ReviveOtherPlayer(PlayerHealth collector)
    {
        if (collector == null || !twoPlayerMode)
            return;

        PlayerHealth otherPlayer = collector.PlayerNumber == 1 ? player2 : player1;
        if (otherPlayer != null && otherPlayer.IsDead)
            otherPlayer.Revive(3);
    }

    public void EnemyHit(Vector3 position)
    {
        if (state != ScreenState.Playing)
            return;

        if (audioSource != null) audioSource.PlayOneShot(enemyHitSound, 0.38f * soundEffectsVolume);
        CreateBurst(position, 7, 0.07f, 1.1f, 0.16f,
            new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.65f, 0.15f)));
    }

    public void EnemyExplosion(Vector3 position)
    {
        if (state != ScreenState.Playing) return;
        if (audioSource != null) audioSource.PlayOneShot(enemyDeathSound, 0.24f * soundEffectsVolume);
        CreateBurst(position, 38, 0.16f, 3.4f, 0.48f,
            new ParticleSystem.MinMaxGradient(new Color(1f, 0.18f, 0.02f), Color.yellow));
        CreateBurst(position, 14, 0.08f, 5.2f, 0.25f,
            new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.5f, 0.05f)));
    }

    public void ExplosiveBulletImpact(Vector3 position, float radius)
    {
        if (state != ScreenState.Playing)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(explosionSound, 0.34f * soundEffectsVolume);
        int count = Mathf.Clamp(Mathf.RoundToInt(14f + radius * 9f), 16, 55);
        CreateBurst(position, count, 0.09f + radius * 0.025f, 2.4f + radius,
            0.24f + radius * 0.035f,
            new ParticleSystem.MinMaxGradient(new Color(1f, 0.18f, 0.01f), new Color(1f, 0.82f, 0.08f)));
    }

    public void BulletPierced(Vector3 position)
    {
        if (state != ScreenState.Playing)
            return;
        CreateBurst(position, 16, 0.075f, 2.1f, 0.24f,
            new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.02f, 0.65f), new Color(0.9f, 0.25f, 1f)));
    }

    private void CreateBurst(Vector3 position, int count, float size, float speed, float lifetime,
        ParticleSystem.MinMaxGradient colors)
    {
        GameObject burst = new GameObject("Impact particles");
        burst.transform.position = position;
        ParticleSystem particles = burst.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = colors;
        main.maxParticles = count;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10;
        if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
        particles.Emit(count);
        Destroy(burst, lifetime + 0.25f);
    }

    private static AudioClip MakeSound(float seconds, float fromHz, float toHz, bool noise)
    {
        const int sampleRate = 22050;
        int count = Mathf.CeilToInt(seconds * sampleRate);
        float[] samples = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float progress = (float)i / count;
            phase += 2f * Mathf.PI * Mathf.Lerp(fromHz, toHz, progress) / sampleRate;
            float tone = Mathf.Sin(phase);
            samples[i] = (noise ? 0.5f * tone + 0.5f * Random.Range(-1f, 1f) : tone) * (1f - progress) * 0.4f;
        }
        AudioClip clip = AudioClip.Create("SpaceYugis SFX", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip MakeBloodSpatterSound()
    {
        const int sampleRate = 22050;
        const float seconds = 0.24f;
        int count = Mathf.CeilToInt(seconds * sampleRate);
        float[] samples = new float[count];
        float filteredNoise = 0f;
        for (int i = 0; i < count; i++)
        {
            float time = (float)i / sampleRate;
            float progress = time / seconds;
            float rawNoise = Mathf.Sin(i * 17.173f + Mathf.Sin(i * 0.071f) * 31.7f);
            filteredNoise = Mathf.Lerp(filteredNoise, rawNoise, 0.16f);
            float wetBody = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(145f, 42f, progress) * time);
            float splatEnvelope = Mathf.Exp(-progress * 5.5f);
            float droplets = Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 31f * time)) *
                             Mathf.Exp(-Mathf.Repeat(time, 0.055f) * 42f);
            samples[i] = Mathf.Clamp((filteredNoise * 0.48f + wetBody * 0.35f) * splatEnvelope +
                                     droplets * 0.12f, -0.65f, 0.65f);
        }
        AudioClip clip = AudioClip.Create("Enemy blood spatter", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip MakeMusic(bool menu)
    {
        const int sampleRate = 22050;
        const float duration = 8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        int[] menuNotes = { 60, 64, 67, 72, 67, 64, 62, 67, 65, 69, 72, 77, 72, 69, 67, 71 };
        int[] battleNotes =
        {
            48, 48, 55, 51, 48, 60, 58, 55,
            50, 50, 57, 53, 50, 62, 60, 57,
            51, 51, 58, 55, 51, 63, 62, 58,
            46, 46, 53, 48, 55, 53, 51, 48
        };
        float menuStepLength = duration / menuNotes.Length;
        float battleStepLength = duration / battleNotes.Length;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            if (menu)
            {
                int step = Mathf.FloorToInt(time / menuStepLength) % menuNotes.Length;
                float stepTime = Mathf.Repeat(time, menuStepLength);
                float melodyFrequency = 440f * Mathf.Pow(2f, (menuNotes[step] - 69) / 12f);
                float menuBassFrequency = 440f * Mathf.Pow(2f, (menuNotes[step] - 24 - 69) / 12f);
                float melodyEnvelope = Mathf.Clamp01(1f - stepTime / menuStepLength);
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * melodyFrequency * time));
                float bass = Mathf.Sin(2f * Mathf.PI * menuBassFrequency * time);
                float pulse = Mathf.Repeat(time * 4f, 1f) < 0.08f ? 0.13f : 0f;
                samples[i] = square * 0.105f * melodyEnvelope + bass * 0.075f + pulse;
                continue;
            }

            int battleStep = Mathf.FloorToInt(time / battleStepLength) % battleNotes.Length;
            float battleStepTime = Mathf.Repeat(time, battleStepLength);
            float noteEnvelope = Mathf.Pow(Mathf.Clamp01(1f - battleStepTime / battleStepLength), 0.45f);
            float leadFrequency = 440f * Mathf.Pow(2f, (battleNotes[battleStep] + 12 - 69) / 12f);
            float bassFrequency = 440f * Mathf.Pow(2f, (battleNotes[battleStep] - 12 - 69) / 12f);

            float leadSquare = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * leadFrequency * time));
            float bassSquare = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * bassFrequency * time));
            float bassDrive = (float)System.Math.Tanh((bassSquare * 0.8f +
                Mathf.Sin(2f * Mathf.PI * bassFrequency * time) * 0.55f) * 1.7f);

            float beatPhase = Mathf.Repeat(time * 4f, 1f);
            int beat = Mathf.FloorToInt(time * 4f) % 4;
            float kickEnvelope = Mathf.Exp(-beatPhase * 10f);
            float kickFrequency = Mathf.Lerp(105f, 46f, beatPhase);
            float kick = Mathf.Sin(2f * Mathf.PI * kickFrequency * beatPhase) * kickEnvelope;

            float noise = Mathf.Sin(i * 12.9898f + Mathf.Sin(i * 0.013f) * 37.719f);
            float snare = (beat == 1 || beat == 3) ? noise * Mathf.Exp(-beatPhase * 15f) : 0f;
            float hatPhase = Mathf.Repeat(time * 8f, 1f);
            float hat = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 5200f * time)) * Mathf.Exp(-hatPhase * 24f);

            float mixed = leadSquare * 0.105f * noteEnvelope + bassDrive * 0.12f +
                          kick * 0.19f + snare * 0.075f + hat * 0.035f;
            samples[i] = Mathf.Clamp(mixed, -0.82f, 0.82f);
        }

        AudioClip clip = AudioClip.Create(menu ? "Menu arcade music" : "Gameplay arcade music", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void SwitchMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null || musicSource.clip == clip)
            return;

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = clip == menuMusic ? menuMusicVolume : gameplayMusicVolume;
        musicSource.Play();
    }

    private void PrepareStyles()
    {
        if (titleStyle != null) return;
        if (arcadeFont == null)
            arcadeFont = Font.CreateDynamicFontFromOSFont(new[] { "Press Start 2P", "Consolas" }, 64);
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, font = arcadeFont };
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, font = arcadeFont };
        centeredStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
        rightStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleRight };
        buttonStyle = new GUIStyle(centeredStyle) { fontSize = 38 };
        statStyle = new GUIStyle(centeredStyle) { fontSize = 30 };
        titleStyle.normal.textColor = Color.white;
        textStyle.normal.textColor = Color.white;
        centeredStyle.normal.textColor = Color.white;
        rightStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        PrepareStyles();
        float width = Screen.width;
        float height = Screen.height;
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.07f), 58, 112);
        buttonStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.032f), 32, 54);
        statStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.023f), 24, 38);
        textStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.019f), 20, 32);
        centeredStyle.fontSize = textStyle.fontSize;
        rightStyle.fontSize = textStyle.fontSize;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        centeredStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        statStyle.alignment = TextAnchor.MiddleCenter;

        if (state != ScreenState.Playing && state != ScreenState.GameOver)
        {
            DrawFullScreenShade(0.82f);
            if (state == ScreenState.MainMenu) DrawMainMenu(width, height);
            else if (state == ScreenState.ModeSelect) DrawModeSelect(width, height);
            else if (state == ScreenState.ShipSelect) DrawShipSelect(width, height);
            else if (state == ScreenState.Ready) DrawReadyScreen(width, height);
            return;
        }

        if (twoPlayerMode)
        {
            DrawPlayerHud(player1, 0, false);
            DrawPlayerHud(player2, 1, true);
        }
        else
        {
            DrawPlayerHud(soloPlayer, 0, false);
        }
        if (orbDropMultiplier > 1 && state == ScreenState.Playing)
            DrawCenteredOutlinedLabel(new Rect(0f, 12f, width, 46f),
                "GALAXY DROPS  " + orbDropMultiplier + "X", statStyle,
                new Color(0.75f, 0.4f, 1f), Color.black, 3);
        if (state != ScreenState.GameOver) return;

        DrawFullScreenShade(0.88f);
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.04f, width, height * 0.17f), "GAME OVER", titleStyle, Color.white, new Color(1f, 0.15f, 0.2f), 5);
        int finalScore = twoPlayerMode ? scores[0] + scores[1] : scores[0];
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.20f, width, 48f),
            (twoPlayerMode ? "COMBINED  " : "SCORE  ") + finalScore + "     HIGH SCORE  " + highScore, centeredStyle, Color.yellow, Color.black, 3);

        if (twoPlayerMode)
        {
            DrawPlayerResults(new Rect(width * 0.07f, height * 0.31f, width * 0.36f, height * 0.32f), 0);
            DrawPlayerResults(new Rect(width * 0.57f, height * 0.31f, width * 0.36f, height * 0.32f), 1);
        }
        else
        {
            DrawPlayerResults(new Rect(width * 0.32f, height * 0.31f, width * 0.36f, height * 0.32f), 0);
        }

        Rect retryRect = new Rect(width * 0.5f - 350f, height * 0.76f, 320f, 80f);
        Rect endQuitRect = new Rect(width * 0.5f + 30f, height * 0.76f, 320f, 80f);
        if (ArcadeButton(retryRect, "PLAY AGAIN", new Color(0.08f, 0.65f, 1f)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (ArcadeButton(endQuitRect, "QUIT", new Color(1f, 0.2f, 0.25f))) Application.Quit();
    }

    private void DrawMainMenu(float width, float height)
    {
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.11f, width, height * 0.2f), "SPACE YUGIS", titleStyle, Color.white, new Color(0.05f, 0.65f, 1f), 5);
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.30f, width, 52f), "ARCADE", centeredStyle, Color.white, Color.black, 3);
        Rect playRect = new Rect(width * 0.5f - 210f, height * 0.47f, 420f, 86f);
        Rect quitRect = new Rect(width * 0.5f - 210f, height * 0.64f, 420f, 86f);
        if (ArcadeButton(playRect, "PLAY", new Color(0.08f, 0.65f, 1f))) state = ScreenState.ModeSelect;
        if (ArcadeButton(quitRect, "QUIT", new Color(1f, 0.2f, 0.25f))) Application.Quit();
    }

    private void DrawModeSelect(float width, float height)
    {
        DrawMenuTitle(width, height, "SELECT MODE");
        Rect onePlayerRect = new Rect(width * 0.5f - 440f, height * 0.44f, 400f, 100f);
        Rect twoPlayerRect = new Rect(width * 0.5f + 40f, height * 0.44f, 400f, 100f);
        if (ArcadeButton(onePlayerRect, "1 PLAYER", new Color(0.1f, 0.75f, 1f)))
        {
            twoPlayerMode = false;
            state = ScreenState.ShipSelect;
        }
        if (ArcadeButton(twoPlayerRect, "2 PLAYERS", new Color(1f, 0.3f, 0.22f)))
        {
            twoPlayerMode = true;
            soloPlayer = null;
            state = ScreenState.Ready;
        }
        DrawBackButton(width, height, ScreenState.MainMenu);
    }

    private void DrawShipSelect(float width, float height)
    {
        DrawMenuTitle(width, height, "SELECT SHIP");
        Rect blackRect = new Rect(width * 0.5f - 440f, height * 0.48f, 400f, 100f);
        Rect whiteRect = new Rect(width * 0.5f + 40f, height * 0.48f, 400f, 100f);
        DrawShipPreview(player1, new Rect(blackRect.center.x - 54f, height * 0.30f, 108f, 108f));
        DrawShipPreview(player2, new Rect(whiteRect.center.x - 54f, height * 0.30f, 108f, 108f));
        if (ArcadeButton(blackRect, "BLACK SHIP", new Color(0.1f, 0.75f, 1f)))
        {
            soloPlayer = player1;
            selectedShipName = "BLACK SHIP";
            state = ScreenState.Ready;
        }
        if (ArcadeButton(whiteRect, "WHITE SHIP", new Color(1f, 0.85f, 0.2f)))
        {
            soloPlayer = player2;
            selectedShipName = "WHITE SHIP";
            state = ScreenState.Ready;
        }
        DrawBackButton(width, height, ScreenState.ModeSelect);
    }

    private void DrawReadyScreen(float width, float height)
    {
        DrawMenuTitle(width, height, "READY?");
        string modeText = twoPlayerMode ? "2 PLAYERS" : "1 PLAYER  -  " + selectedShipName;
        string controls = twoPlayerMode ? "P1  A / D + W       P2  ARROWS" : "A / D TO MOVE       W TO SHOOT";
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.32f, width, 50f), modeText, statStyle, Color.yellow, Color.black, 3);
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.41f, width, 44f), controls, centeredStyle, Color.white, Color.black, 2);
        Rect startRect = new Rect(width * 0.5f - 280f, height * 0.57f, 560f, 112f);
        if (ArcadeButton(startRect, "START", new Color(0.15f, 0.9f, 0.35f))) StartGame();
        DrawBackButton(width, height, twoPlayerMode ? ScreenState.ModeSelect : ScreenState.ShipSelect);
    }

    private void DrawMenuTitle(float width, float height, string title)
    {
        DrawCenteredOutlinedLabel(new Rect(0f, height * 0.1f, width, height * 0.17f), title, titleStyle, Color.white, new Color(0.08f, 0.65f, 1f), 5);
    }

    private void DrawBackButton(float width, float height, ScreenState destination)
    {
        if (ArcadeButton(new Rect(40f, height - 90f, 220f, 58f), "BACK", new Color(0.5f, 0.55f, 0.7f)))
            state = destination;
    }

    private static void DrawShipPreview(PlayerHealth player, Rect rect)
    {
        if (player == null || player.LifeSprite == null)
            return;
        Sprite sprite = player.LifeSprite;
        Rect source = sprite.textureRect;
        Rect uv = new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
            source.width / sprite.texture.width, source.height / sprite.texture.height);
        Color previous = GUI.color;
        GUI.color = player.ShipColor;
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        GUI.color = previous;
    }

    private void DrawFullScreenShade(float opacity)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.015f, 0.02f, 0.08f, opacity);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawPlayerResults(Rect rect, int index)
    {
        Color accent = index == 0 ? new Color(0.1f, 0.7f, 1f) : new Color(1f, 0.2f, 0.25f);
        DrawOutlinedLabel(new Rect(rect.x, rect.y, rect.width, 58f), "PLAYER " + (index + 1), buttonStyle, accent, Color.black, 4);
        DrawOutlinedLabel(new Rect(rect.x, rect.y + 65f, rect.width, 58f), "SCORE  " + scores[index], statStyle, Color.white, Color.black, 3);
        DrawOutlinedLabel(new Rect(rect.x, rect.y + 120f, rect.width, 52f), "KILLS  " + kills[index], statStyle, Color.white, Color.black, 3);
        DrawOutlinedLabel(new Rect(rect.x, rect.y + 175f, rect.width, 52f), "HITS  " + hits[index], statStyle, Color.white, Color.black, 3);
    }

    private bool ArcadeButton(Rect rect, string label, Color accent)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Color previous = GUI.color;
        GUI.color = hover ? Color.white : accent;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.015f, 0.02f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(rect.x + 7f, rect.y + 7f, rect.width - 14f, rect.height - 14f), Texture2D.whiteTexture);
        GUI.color = previous;
        int originalFontSize = buttonStyle.fontSize;
        while (buttonStyle.fontSize > 14)
        {
            Vector2 size = buttonStyle.CalcSize(new GUIContent(label));
            if (size.x <= rect.width - 34f && size.y <= rect.height - 18f)
                break;
            buttonStyle.fontSize -= 2;
        }
        DrawCenteredOutlinedLabel(rect, label, buttonStyle, hover ? accent : Color.white, Color.black, 3);
        buttonStyle.fontSize = originalFontSize;
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private static void DrawCenteredOutlinedLabel(Rect container, string label, GUIStyle style, Color color, Color outline, int size)
    {
        Vector2 measured = style.CalcSize(new GUIContent(label));
        Rect textRect = new Rect(container.center.x - measured.x * 0.5f, container.y,
            measured.x, container.height);
        TextAnchor previousAlignment = style.alignment;
        style.alignment = TextAnchor.MiddleCenter;
        DrawOutlinedLabel(textRect, label, style, color, outline, size);
        style.alignment = previousAlignment;
    }

    private static void DrawOutlinedLabel(Rect rect, string label, GUIStyle style, Color color, Color outline, int size)
    {
        Color previous = style.normal.textColor;
        SetAllTextColors(style, outline);
        for (int x = -size; x <= size; x += size)
        for (int y = -size; y <= size; y += size)
        {
            if (x != 0 || y != 0)
                GUI.Label(new Rect(rect.x + x, rect.y + y, rect.width, rect.height), label, style);
        }
        SetAllTextColors(style, color);
        GUI.Label(rect, label, style);
        SetAllTextColors(style, previous);
    }

    private static void SetAllTextColors(GUIStyle style, Color color)
    {
        style.normal.textColor = color;
        style.hover.textColor = color;
        style.active.textColor = color;
        style.focused.textColor = color;
        style.onNormal.textColor = color;
        style.onHover.textColor = color;
        style.onActive.textColor = color;
        style.onFocused.textColor = color;
    }

    private void DrawPlayerHud(PlayerHealth player, int index, bool right)
    {
        if (player == null) return;
        float margin = Mathf.Max(20f, Screen.width * 0.015f);
        float hudWidth = Mathf.Min(380f, Screen.width * 0.34f);
        float x = right ? Screen.width - margin - hudWidth : margin;
        GUIStyle scoreStyle = right ? rightStyle : textStyle;
        DrawFittedOutlinedLabel(new Rect(x, 12f, hudWidth, 40f), "PLAYER " + (index + 1),
            scoreStyle, index == 0 ? new Color(0.15f, 0.75f, 1f) : new Color(1f, 0.28f, 0.2f), Color.black, 2);
        DrawFittedOutlinedLabel(new Rect(x, 52f, hudWidth, 40f), "SCORE: " + scores[index],
            scoreStyle, Color.white, Color.black, 2);
        Sprite lifeSprite = player.LifeSprite;
        if (lifeSprite == null)
            return;

        Rect spriteRect = lifeSprite.textureRect;
        Rect uv = new Rect(spriteRect.x / lifeSprite.texture.width, spriteRect.y / lifeSprite.texture.height,
            spriteRect.width / lifeSprite.texture.width, spriteRect.height / lifeSprite.texture.height);
        int max = player.MaxLives;
        for (int i = 0; i < max; i++)
        {
            bool alive = i < player.CurrentLives;
            bool flashing = i == flashingLife[index] && Time.unscaledTime < lostFlashUntil[index] &&
                            Mathf.FloorToInt(Time.unscaledTime * 12f) % 2 == 0;
            if (!alive && !flashing) continue;
            GUI.color = flashing ? Color.red : player.ShipColor;
            float iconX = right ? Screen.width - 20f - (i + 1) * 42f : 20f + i * 42f;
            GUI.DrawTextureWithTexCoords(new Rect(iconX, Screen.height - 62f, 36f, 36f), lifeSprite.texture, uv, true);
        }
        GUI.color = Color.white;
    }

    private static void DrawFittedOutlinedLabel(Rect rect, string label, GUIStyle style,
        Color color, Color outline, int outlineSize)
    {
        int originalFontSize = style.fontSize;
        while (style.fontSize > 14 && style.CalcSize(new GUIContent(label)).x > rect.width - outlineSize * 2f)
            style.fontSize--;
        DrawOutlinedLabel(rect, label, style, color, outline, outlineSize);
        style.fontSize = originalFontSize;
    }
}
