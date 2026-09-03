using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bouwt de complete Space Yugis-game op wanneer de speelscene opent.
/// Er zijn geen prefabs of handmatige Inspector-koppelingen nodig.
/// </summary>
public class SpaceInvadersGame : MonoBehaviour
{
    private enum GameState { WaitingToStart, Playing, Won, GameOver }

    private sealed class Actor
    {
        public GameObject gameObject;
        public int playerNumber;
        public int lives;
        public float nextShotTime;
        public bool Alive => lives > 0 && gameObject != null;
    }

    private sealed class Shot
    {
        public GameObject gameObject;
        public float speed;
        public int owner;
    }

    private sealed class Meteor
    {
        public GameObject gameObject;
        public float speed;
        public float rotationSpeed;
    }

    private const float LeftEdge = -8.35f;
    private const float RightEdge = 8.35f;
    private const float PlayerY = -4.05f;
    private const float PlayerSpeed = 7.5f;
    private const float ShotCooldown = 0.28f;

    private readonly List<GameObject> enemies = new List<GameObject>();
    private readonly List<Shot> shots = new List<Shot>();
    private readonly List<Meteor> meteors = new List<Meteor>();
    private readonly Actor[] players = { new Actor(), new Actor() };

    private GameState state = GameState.WaitingToStart;
    private Transform enemyFormation;
    private Sprite squareSprite;
    private Sprite bulletSprite;
    private Sprite meteorSprite;
    private Sprite player1Sprite;
    private Sprite player2Sprite;
    private float enemyDirection = 1f;
    private float enemySpeed = 1.15f;
    private float nextEnemyShotTime;
    private float nextMeteorTime;
    private int scorePlayer1;
    private int scorePlayer2;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGame()
    {
        if (FindFirstObjectByType<SpaceInvadersGame>() != null)
            return;

        new GameObject("Space Yugis Game").AddComponent<SpaceInvadersGame>();
    }

    private void Awake()
    {
        // Oude menu-canvassen kunnen na een platformwissel het volledige spel afdekken.
        // Space Yugis gebruikt zijn eigen schaalbare interface, dus die canvassen zijn niet nodig.
        Canvas[] oldCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas oldCanvas in oldCanvases)
            oldCanvas.enabled = false;

        ConfigureCamera();
        squareSprite = CreateSquareSprite();
        bulletSprite = LoadPixelSprite("SpaceInvaders/Bullet");
        meteorSprite = LoadPixelSprite("SpaceInvaders/Meteor");
        player1Sprite = LoadPixelSprite("SpaceInvaders/Player1");
        player2Sprite = LoadPixelSprite("SpaceInvaders/Player2");
        CreateStars();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (state != GameState.Playing)
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
                StartGame();
            return;
        }

        UpdatePlayer(players[0], keyboard.aKey.isPressed, keyboard.dKey.isPressed,
            keyboard.wKey.wasPressedThisFrame);
        UpdatePlayer(players[1], keyboard.leftArrowKey.isPressed, keyboard.rightArrowKey.isPressed,
            keyboard.upArrowKey.wasPressedThisFrame);

        UpdateEnemies();
        UpdateShots();
        UpdateMeteors();
        CheckEndConditions();
    }

    private void StartGame()
    {
        ClearRound();
        scorePlayer1 = 0;
        scorePlayer2 = 0;
        enemyDirection = 1f;
        enemySpeed = 1.15f;

        players[0].gameObject = CreatePlayer("Speler 1", new Vector2(-3.2f, PlayerY), player1Sprite);
        players[0].playerNumber = 1;
        players[0].lives = 3;
        players[1].gameObject = CreatePlayer("Speler 2", new Vector2(3.2f, PlayerY), player2Sprite);
        players[1].playerNumber = 2;
        players[1].lives = 3;

        CreateEnemyFormation();
        nextEnemyShotTime = Time.time + 1.2f;
        nextMeteorTime = Time.time + 2.5f;
        state = GameState.Playing;
    }

    private void UpdatePlayer(Actor player, bool movingLeft, bool movingRight, bool firing)
    {
        if (!player.Alive)
            return;

        float direction = (movingRight ? 1f : 0f) - (movingLeft ? 1f : 0f);
        Vector3 position = player.gameObject.transform.position;
        position.x = Mathf.Clamp(position.x + direction * PlayerSpeed * Time.deltaTime, LeftEdge, RightEdge);
        player.gameObject.transform.position = position;

        if (firing && Time.time >= player.nextShotTime)
        {
            CreateShot(position + Vector3.up * 0.52f, 10.5f, player.playerNumber);
            player.nextShotTime = Time.time + ShotCooldown;
        }
    }

    private void UpdateEnemies()
    {
        if (enemyFormation == null || enemies.Count == 0)
            return;

        enemyFormation.position += Vector3.right * (enemyDirection * enemySpeed * Time.deltaTime);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            minX = Mathf.Min(minX, enemy.transform.position.x);
            maxX = Mathf.Max(maxX, enemy.transform.position.x);
        }

        if ((enemyDirection > 0f && maxX >= RightEdge) || (enemyDirection < 0f && minX <= LeftEdge))
        {
            enemyDirection *= -1f;
            enemyFormation.position += Vector3.down * 0.38f;
            enemySpeed += 0.07f;
        }

        if (Time.time >= nextEnemyShotTime)
        {
            GameObject shooter = enemies[Random.Range(0, enemies.Count)];
            if (shooter != null)
                CreateShot(shooter.transform.position + Vector3.down * 0.35f, -5.2f, 0);

            nextEnemyShotTime = Time.time + Random.Range(0.45f, 1.05f);
        }
    }

    private void UpdateShots()
    {
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            Shot shot = shots[i];
            if (shot.gameObject == null)
            {
                shots.RemoveAt(i);
                continue;
            }

            shot.gameObject.transform.position += Vector3.up * (shot.speed * Time.deltaTime);
            Vector3 shotPosition = shot.gameObject.transform.position;

            bool hit = shot.owner == 0 ? HitPlayer(shotPosition, 1) : HitEnemy(shotPosition, shot.owner);
            if (hit || shotPosition.y > 5.6f || shotPosition.y < -5.6f)
            {
                Destroy(shot.gameObject);
                shots.RemoveAt(i);
            }
        }
    }

    private bool HitEnemy(Vector3 shotPosition, int playerNumber)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = enemies[i];
            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            Vector3 enemyPosition = enemy.transform.position;
            if (Mathf.Abs(shotPosition.x - enemyPosition.x) < 0.47f &&
                Mathf.Abs(shotPosition.y - enemyPosition.y) < 0.38f)
            {
                Destroy(enemy);
                enemies.RemoveAt(i);
                if (playerNumber == 1) scorePlayer1 += 100;
                else scorePlayer2 += 100;
                enemySpeed += 0.035f;
                return true;
            }
        }
        return false;
    }

    private bool HitPlayer(Vector3 hitPosition, int damage)
    {
        foreach (Actor player in players)
        {
            if (!player.Alive) continue;
            Vector3 playerPosition = player.gameObject.transform.position;
            if (Mathf.Abs(hitPosition.x - playerPosition.x) < 0.55f &&
                Mathf.Abs(hitPosition.y - playerPosition.y) < 0.42f)
            {
                player.lives = Mathf.Max(0, player.lives - damage);
                if (player.lives <= 0)
                {
                    Destroy(player.gameObject);
                    player.gameObject = null;
                }
                return true;
            }
        }
        return false;
    }

    private void UpdateMeteors()
    {
        if (Time.time >= nextMeteorTime)
        {
            CreateMeteor();
            nextMeteorTime = Time.time + Random.Range(2.4f, 4.2f);
        }

        for (int i = meteors.Count - 1; i >= 0; i--)
        {
            Meteor meteor = meteors[i];
            if (meteor.gameObject == null)
            {
                meteors.RemoveAt(i);
                continue;
            }

            meteor.gameObject.transform.position += Vector3.down * (meteor.speed * Time.deltaTime);
            meteor.gameObject.transform.Rotate(0f, 0f, meteor.rotationSpeed * Time.deltaTime);
            Vector3 meteorPosition = meteor.gameObject.transform.position;

            // Een meteoriet doet twee schade; een normale vijandelijke kogel doet één schade.
            if (HitPlayer(meteorPosition, 2) || meteorPosition.y < -5.7f)
            {
                Destroy(meteor.gameObject);
                meteors.RemoveAt(i);
            }
        }
    }

    private void CreateMeteor()
    {
        GameObject meteorObject = CreateSpriteObject("Meteoriet", new Vector2(Random.Range(LeftEdge, RightEdge), 5.35f),
            new Vector2(1.15f, 1.15f), meteorSprite, Color.white, 6);
        meteors.Add(new Meteor
        {
            gameObject = meteorObject,
            speed = Random.Range(3.8f, 5.4f),
            rotationSpeed = Random.Range(-95f, 95f)
        });
    }

    private void CheckEndConditions()
    {
        if (enemies.Count == 0)
        {
            state = GameState.Won;
            return;
        }

        bool invadersLanded = false;
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.transform.position.y <= PlayerY + 0.65f)
            {
                invadersLanded = true;
                break;
            }
        }

        if ((!players[0].Alive && !players[1].Alive) || invadersLanded)
            state = GameState.GameOver;
    }

    private void CreateEnemyFormation()
    {
        enemyFormation = new GameObject("Vijandenformatie").transform;
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                Color color = Color.Lerp(new Color(0.55f, 1f, 0.3f), new Color(1f, 0.8f, 0.2f), row / 3f);
                GameObject enemy = CreateBlock("Invader", new Vector2(-5.25f + column * 1.5f, 3.75f - row * 0.78f),
                    new Vector2(0.78f, 0.48f), color, 5);
                enemy.transform.SetParent(enemyFormation, true);

                GameObject eyeLeft = CreateBlock("Oog", enemy.transform.position + new Vector3(-0.2f, 0.05f, -0.1f),
                    new Vector2(0.1f, 0.1f), new Color(0.03f, 0.04f, 0.1f), 6);
                GameObject eyeRight = CreateBlock("Oog", enemy.transform.position + new Vector3(0.2f, 0.05f, -0.1f),
                    new Vector2(0.1f, 0.1f), new Color(0.03f, 0.04f, 0.1f), 6);
                eyeLeft.transform.SetParent(enemy.transform, true);
                eyeRight.transform.SetParent(enemy.transform, true);
                enemies.Add(enemy);
            }
        }
    }

    private GameObject CreatePlayer(string name, Vector2 position, Sprite playerSprite)
    {
        return CreateSpriteObject(name, position, new Vector2(1.35f, 1.35f), playerSprite, Color.white, 5);
    }

    private void CreateShot(Vector3 position, float speed, int owner)
    {
        GameObject shotObject = CreateSpriteObject(owner == 0 ? "Vijandelijke kogel" : "Spelerkogel", position,
            new Vector2(0.72f, 0.72f), bulletSprite, Color.white, 7);
        if (owner == 0)
            shotObject.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        shots.Add(new Shot { gameObject = shotObject, speed = speed, owner = owner });
    }

    private static GameObject CreateSpriteObject(string objectName, Vector2 position, Vector2 scale,
        Sprite sprite, Color color, int sortingOrder)
    {
        GameObject spriteObject = new GameObject(objectName);
        spriteObject.transform.position = position;
        spriteObject.transform.localScale = scale;
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return spriteObject;
    }

    private GameObject CreateBlock(string objectName, Vector2 position, Vector2 scale, Color color, int sortingOrder)
    {
        GameObject block = new GameObject(objectName);
        block.transform.position = position;
        block.transform.localScale = scale;
        SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return block;
    }

    private void CreateStars()
    {
        Random.State oldState = Random.state;
        Random.InitState(7284);
        for (int i = 0; i < 75; i++)
        {
            float size = Random.Range(0.018f, 0.065f);
            Color color = i % 7 == 0 ? new Color(0.45f, 0.7f, 1f, 0.8f) : new Color(1f, 1f, 1f, 0.65f);
            CreateBlock("Ster", new Vector2(Random.Range(-8.8f, 8.8f), Random.Range(-4.8f, 4.8f)),
                new Vector2(size, size), color, -5);
        }
        Random.state = oldState;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "Runtime witte pixel";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite LoadPixelSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogError($"Sprite niet gevonden in Resources: {resourcePath}");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 16f);
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.025f, 0.085f);
    }

    private void ClearRound()
    {
        foreach (Shot shot in shots)
            if (shot.gameObject != null) Destroy(shot.gameObject);
        shots.Clear();

        foreach (Meteor meteor in meteors)
            if (meteor.gameObject != null) Destroy(meteor.gameObject);
        meteors.Clear();

        foreach (GameObject enemy in enemies)
            if (enemy != null) Destroy(enemy);
        enemies.Clear();

        if (enemyFormation != null) Destroy(enemyFormation.gameObject);
        foreach (Actor player in players)
            if (player.gameObject != null) Destroy(player.gameObject);
    }

    private void BuildGuiStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.45f, 1f, 0.75f) }
        };
        textStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            normal = { textColor = Color.white }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };
    }

    private void OnGUI()
    {
        BuildGuiStyles();
        float scale = Mathf.Max(0.75f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = Screen.height / scale;

        if (state == GameState.Playing)
        {
            GUI.Label(new Rect(20, 12, 280, 35), $"P1  SCORE {scorePlayer1:0000}   LEVENS {players[0].lives}", textStyle);
            GUI.Label(new Rect(width - 300, 12, 280, 35), $"P2  SCORE {scorePlayer2:0000}   LEVENS {players[1].lives}", textStyle);
        }
        else
        {
            string heading = state == GameState.WaitingToStart ? "SPACE YUGIS" :
                state == GameState.Won ? "GEWONNEN!" : "GAME OVER";
            GUI.Label(new Rect(width / 2f - 300, height / 2f - 185, 600, 60), heading, titleStyle);
            GUI.Label(new Rect(width / 2f - 350, height / 2f - 110, 700, 70),
                "Speler 1: A / D bewegen  •  W schieten\nSpeler 2: ← / → bewegen  •  ↑ schieten", textStyle);

            if (GUI.Button(new Rect(width / 2f - 145, height / 2f + 5, 290, 58),
                state == GameState.WaitingToStart ? "START" : "OPNIEUW", buttonStyle))
            {
                StartGame();
            }
        }

        GUI.matrix = previousMatrix;
    }
}
