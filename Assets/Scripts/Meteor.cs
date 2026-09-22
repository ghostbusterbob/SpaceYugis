
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

    private void Reset()
    {
        // Ensure CircleCollider2D exists and is not a trigger.
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = false;

        // Ensure Rigidbody2D exists and set sensible defaults.
        Rigidbody2D rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.gravityScale = 0f;    // we control the fall vector directly for a straight diagonal path
        rb2d.freezeRotation = false;
        _rb2d = rb2d;
    }

    private void Awake()
    {
        _rb2d = GetComponent<Rigidbody2D>();
        if (_rb2d == null)
        {
            _rb2d = gameObject.AddComponent<Rigidbody2D>();
            _rb2d.bodyType = RigidbodyType2D.Dynamic;
            _rb2d.gravityScale = 0f;
        }
    }

    private void Start()
    {
        // Give a slight random horizontal velocity so the meteor falls diagonally.
        float hx = Random.Range(-horizontalDrift, horizontalDrift);
        float vy = -Mathf.Abs(fallSpeed);
        if (_rb2d != null)
        {
            _rb2d.linearVelocity = new Vector2(hx, vy);
        }

        // Optionally rotate the sprite to point along travel direction (visual tweak)
        float angle = Mathf.Atan2(vy, hx) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void OnValidate()
    {
        if (destroyOnImpactDelay < 0f) destroyOnImpactDelay = 0f;
        if (string.IsNullOrEmpty(playerTag)) playerTag = "Player";
        if (fallSpeed < 0f) fallSpeed = Mathf.Abs(fallSpeed);
        if (horizontalDrift < 0f) horizontalDrift = Mathf.Abs(horizontalDrift);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null && collision.collider.CompareTag(playerTag))
        {
            GameObject playerObj = collision.collider.gameObject;

            // Notify player script if it has a Die method.
            playerObj.SendMessage("Die", SendMessageOptions.DontRequireReceiver);

            if (destroyPlayerOnHit)
            {
                Destroy(playerObj);
            }
        }

        // Remove the meteor after impact to avoid multiple hits.
        Destroy(gameObject, destroyOnImpactDelay);
    }
}