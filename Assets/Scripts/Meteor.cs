
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class Meteor : MonoBehaviour
{
    [Tooltip("Tag used to identify the player. Default: Player")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If true, the player's GameObject will be destroyed on hit. If false, this script will call SendMessage(\"Die\") and leave handling to the player script.")]
    [SerializeField] private bool destroyPlayerOnHit = true;

    [Tooltip("How long after impact the meteor is removed.")]
    [SerializeField] private float destroyOnImpactDelay = 0.1f;

    [Header("Fall behaviour")]
    [Tooltip("Vertical fall speed (positive number). Meteor will move downward at -fallSpeed.")]
    [SerializeField] private float fallSpeed = 8f;
    [Tooltip("Maximum absolute horizontal drift speed. Actual horizontal velocity is random in [-horizontalDrift, horizontalDrift].")]
    [SerializeField] private float horizontalDrift = 1.5f;

    private Rigidbody2D _rb2d;
    private Vector2 fallVelocity;
    private bool impacted;

    private void Reset()
    {
        // A trigger prevents collisions from knocking or rotating the meteor.
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        // Ensure Rigidbody2D exists and set sensible defaults.
        Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        ConfigureBody(rb2d);
        _rb2d = rb2d;
    }

    private void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null)
            _rb2d = gameObject.AddComponent<Rigidbody2D>();

        ConfigureBody(_rb2d);
        transform.rotation = Quaternion.identity;
    }

    private void Start()
    {
        // Give a slight random horizontal velocity so the meteor falls diagonally.
        fallVelocity = new Vector2(Random.Range(-horizontalDrift, horizontalDrift), -Mathf.Abs(fallSpeed));
        transform.rotation = Quaternion.identity;
    }

    private void FixedUpdate()
    {
        if (_rb2d == null || impacted)
            return;

        _rb2d.MovePosition(_rb2d.position + fallVelocity * Time.fixedDeltaTime);
        _rb2d.SetRotation(0f);
    }

    private void LateUpdate()
    {
        // Keep the flame pointing upward regardless of the diagonal movement.
        transform.rotation = Quaternion.identity;
    }

    private static void ConfigureBody(Rigidbody2D body)
    {
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnValidate()
    {
        if (destroyOnImpactDelay < 0f) destroyOnImpactDelay = 0f;
        if (string.IsNullOrEmpty(playerTag)) playerTag = "Player";
        if (fallSpeed < 0f) fallSpeed = Mathf.Abs(fallSpeed);
        if (horizontalDrift < 0f) horizontalDrift = Mathf.Abs(horizontalDrift);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacted || other == null || !other.CompareTag(playerTag))
            return;

        impacted = true;
        CompanionShip companion = other.GetComponentInParent<CompanionShip>();
        if (companion != null)
        {
            companion.TakeMeteorHit();
            Destroy(gameObject, destroyOnImpactDelay);
            return;
        }

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.TakeMeteorHit();

        Destroy(gameObject, destroyOnImpactDelay);
    }
}
