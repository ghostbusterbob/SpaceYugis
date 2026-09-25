using System.Collections.Generic;
using UnityEngine;

public class SpaceBackground : MonoBehaviour
{
    private sealed class Floater
    {
        public Transform Transform;
        public Vector3 Origin;
        public float Phase;
        public float Speed;
        public float Distance;
    }

    [Header("REFERENCES")]
    [SerializeField] private Camera sceneCamera;

    [Header("STARS AND PLANETS")]
    [Range(20, 160)] [SerializeField] private int starCount = 85;
    [Range(1, 6)] [SerializeField] private int planetCount = 3;
    [SerializeField] private float planetFloatDistance = 0.18f;

    [Header("GALAXY COLOUR")]
    [Range(0f, 1f)] [SerializeField] private float baseHue = 0.61f;
    [Range(0f, 0.25f)] [SerializeField] private float hueShiftAmount = 0.055f;
    [Range(0f, 0.5f)] [SerializeField] private float colourChangeSpeed = 0.055f;
    [Range(0.02f, 0.25f)] [SerializeField] private float backgroundBrightness = 0.075f;
    [Range(0f, 0.4f)] [SerializeField] private float nebulaOpacity = 0.11f;

    [Header("BACKGROUND MOVEMENT")]
    [SerializeField] private Vector2 starDriftDistance = new Vector2(0.22f, 0.14f);
    [SerializeField] private Vector2 starDriftSpeed = new Vector2(0.08f, 0.065f);
    [SerializeField] private Vector2 nebulaDriftDistance = new Vector2(1.15f, 0.7f);

    private readonly List<Floater> floaters = new List<Floater>();
    private readonly List<SpriteRenderer> nebulae = new List<SpriteRenderer>();
    private readonly List<Vector3> nebulaOrigins = new List<Vector3>();
    private Transform backgroundRoot;
    private Sprite starSprite;

    private void Awake()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            Debug.LogError("SpaceBackground: assign a camera in the Inspector.", this);
            enabled = false;
            return;
        }
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.003f, 0.005f, 0.018f);
        BuildBackground();
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float hue = Mathf.Repeat(baseHue + Mathf.Sin(time * colourChangeSpeed) * hueShiftAmount, 1f);
        sceneCamera.backgroundColor = Color.HSVToRGB(hue, 0.76f, backgroundBrightness);
        backgroundRoot.position = new Vector3(
            Mathf.Sin(time * starDriftSpeed.x) * starDriftDistance.x,
            Mathf.Cos(time * starDriftSpeed.y) * starDriftDistance.y, 0f);

        for (int i = 0; i < nebulae.Count; i++)
        {
            SpriteRenderer nebula = nebulae[i];
            Vector3 origin = nebulaOrigins[i];
            float phase = i * 2.15f;
            nebula.transform.position = origin + new Vector3(
                Mathf.Sin(time * (0.025f + i * 0.008f) + phase) * nebulaDriftDistance.x,
                Mathf.Cos(time * (0.02f + i * 0.006f) + phase) * nebulaDriftDistance.y,
                0f);
            Color color = Color.HSVToRGB(Mathf.Repeat(hue + i * 0.17f, 1f), 0.72f, 0.48f);
            color.a = Mathf.Max(0f, nebulaOpacity + Mathf.Sin(time * 0.18f + phase) * 0.025f);
            nebula.color = color;
        }

        foreach (Floater floater in floaters)
        {
            float y = Mathf.Sin(time * floater.Speed + floater.Phase) * floater.Distance;
            float x = Mathf.Cos(time * floater.Speed * 0.55f + floater.Phase) * floater.Distance * 0.35f;
            floater.Transform.position = floater.Origin + new Vector3(x, y, 0f);
        }
    }

    private void BuildBackground()
    {
        GameObject root = new GameObject("Cartoon space background");
        backgroundRoot = root.transform;

        float halfHeight = sceneCamera.orthographicSize;
        float halfWidth = halfHeight * sceneCamera.aspect;
        Vector2 cameraCenter = sceneCamera.transform.position;
        starSprite = CreateStarSprite();

        Random.State previousState = Random.state;
        Random.InitState(90210);

        CreateNebula(new Vector3(cameraCenter.x - halfWidth * 0.35f, cameraCenter.y + halfHeight * 0.25f, 3f), new Vector3(4.8f, 2.5f, 1f));
        CreateNebula(new Vector3(cameraCenter.x + halfWidth * 0.42f, cameraCenter.y - halfHeight * 0.18f, 3f), new Vector3(5.4f, 2.8f, 1f));
        CreateNebula(new Vector3(cameraCenter.x, cameraCenter.y + halfHeight * 0.62f, 3f), new Vector3(3.8f, 2.1f, 1f));

        for (int i = 0; i < starCount; i++)
        {
            GameObject star = new GameObject("Star");
            star.transform.SetParent(backgroundRoot);
            star.transform.position = new Vector3(cameraCenter.x + Random.Range(-halfWidth, halfWidth), cameraCenter.y + Random.Range(-halfHeight, halfHeight), 2f);
            float size = Random.Range(0.045f, 0.13f);
            star.transform.localScale = Vector3.one * size;
            SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
            renderer.sprite = starSprite;
            renderer.color = Random.value < 0.16f ? new Color(1f, 0.86f, 0.4f, 0.9f) : new Color(0.75f, 0.9f, 1f, Random.Range(0.55f, 0.95f));
            renderer.sortingOrder = -120;
        }

        Color[] planetColors =
        {
            new Color(0.22f, 0.65f, 1f),
            new Color(0.76f, 0.3f, 0.95f),
            new Color(1f, 0.48f, 0.16f),
            new Color(0.2f, 0.85f, 0.62f)
        };
        Vector2[] positions =
        {
            new Vector2(-halfWidth * 0.72f, halfHeight * 0.45f),
            new Vector2(halfWidth * 0.72f, halfHeight * 0.08f),
            new Vector2(-halfWidth * 0.52f, -halfHeight * 0.34f),
            new Vector2(halfWidth * 0.48f, halfHeight * 0.62f)
        };

        for (int i = 0; i < planetCount; i++)
        {
            Color primary = planetColors[i % planetColors.Length];
            Color band = Color.Lerp(primary, Color.white, 0.35f);
            Vector3 position = new Vector3(cameraCenter.x + positions[i % positions.Length].x, cameraCenter.y + positions[i % positions.Length].y, 2f);
            CreatePlanet(position, Random.Range(0.55f, 1.05f), primary, band, i % 2 == 0, i);
        }

        Random.state = previousState;
    }

    private void CreateNebula(Vector3 position, Vector3 scale)
    {
        GameObject cloud = new GameObject("Moving galaxy cloud");
        cloud.transform.SetParent(backgroundRoot);
        cloud.transform.position = position;
        cloud.transform.localScale = scale;
        SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateNebulaSprite();
        renderer.color = new Color(0.18f, 0.35f, 0.9f, 0.12f);
        renderer.sortingOrder = -130;
        nebulae.Add(renderer);
        nebulaOrigins.Add(position);
    }

    private void CreatePlanet(Vector3 position, float scale, Color primary, Color band, bool hasRing, int index)
    {
        GameObject holder = new GameObject("Floating cartoon planet");
        holder.transform.SetParent(backgroundRoot);
        holder.transform.position = position;

        if (hasRing)
        {
            GameObject ringObject = new GameObject("Planet ring");
            ringObject.transform.SetParent(holder.transform, false);
            ringObject.transform.localScale = new Vector3(scale * 1.55f, scale * 0.65f, 1f);
            SpriteRenderer ring = ringObject.AddComponent<SpriteRenderer>();
            ring.sprite = CreateRingSprite(Color.Lerp(primary, Color.white, 0.55f));
            ring.sortingOrder = -112;
        }

        GameObject planetObject = new GameObject("Planet");
        planetObject.transform.SetParent(holder.transform, false);
        planetObject.transform.localScale = Vector3.one * scale;
        SpriteRenderer planet = planetObject.AddComponent<SpriteRenderer>();
        planet.sprite = CreatePlanetSprite(primary, band, index);
        planet.sortingOrder = -111;

        floaters.Add(new Floater
        {
            Transform = holder.transform,
            Origin = position,
            Phase = index * 1.7f,
            Speed = 0.35f + index * 0.08f,
            Distance = planetFloatDistance
        });
    }

    private static Sprite CreateStarSprite()
    {
        const int size = 9;
        Texture2D texture = NewTexture(size, size, "Cartoon star");
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int dx = Mathf.Abs(x - size / 2);
            int dy = Mathf.Abs(y - size / 2);
            bool visible = dx == 0 || dy == 0 || dx + dy <= 3;
            texture.SetPixel(x, y, visible ? Color.white : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
    }

    private static Sprite CreatePlanetSprite(Color primary, Color band, int seed)
    {
        const int size = 64;
        Texture2D texture = NewTexture(size, size, "Cartoon planet");
        Vector2 center = Vector2.one * (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x, y) - center;
            float distance = point.magnitude;
            if (distance > 30f)
            {
                texture.SetPixel(x, y, Color.clear);
                continue;
            }

            Color pixel = distance > 27f ? new Color(0.03f, 0.04f, 0.12f) : primary;
            if (distance <= 27f && ((y + seed * 7) % 17 < 5))
                pixel = Color.Lerp(pixel, band, 0.55f);
            if (distance <= 27f && Vector2.Distance(point, new Vector2(-10f, 10f)) < 5f)
                pixel = Color.Lerp(pixel, Color.white, 0.6f);
            if (distance <= 25f && Vector2.Distance(point, new Vector2(11f, -7f)) < 4f)
                pixel = Color.Lerp(pixel, Color.black, 0.16f);
            texture.SetPixel(x, y, pixel);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
    }

    private static Sprite CreateRingSprite(Color color)
    {
        const int width = 96;
        const int height = 48;
        Texture2D texture = NewTexture(width, height, "Cartoon planet ring");
        Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = (x - center.x) / 44f;
            float dy = (y - center.y) / 15f;
            float ellipse = dx * dx + dy * dy;
            bool ring = ellipse > 0.72f && ellipse < 1.08f;
            texture.SetPixel(x, y, ring ? color : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f, width);
    }

    private static Sprite CreateNebulaSprite()
    {
        const int size = 128;
        Texture2D texture = NewTexture(size, size, "Soft galaxy cloud");
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = Vector2.one * (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 delta = new Vector2((x - center.x) / 1.25f, y - center.y);
            float radial = Mathf.Clamp01(1f - delta.magnitude / (size * 0.48f));
            float wisps = 0.65f + Mathf.Sin(x * 0.12f + y * 0.055f) * 0.2f + Mathf.Sin(y * 0.18f) * 0.15f;
            float alpha = radial * radial * Mathf.Clamp01(wisps);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 32f);
    }

    private static Texture2D NewTexture(int width, int height, string textureName)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = textureName;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }
}
