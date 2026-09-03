using System.Collections;
using UnityEngine;

/// <summary>
/// 2D-focused bullet. Exposes damage so enemies can read it and fixes Rigidbody API usage.
/// </summary>
public class Bullet : MonoBehaviour
{
    private float _speed;
    private float _damage;
    private float _range;
    private Vector3 _direction;
    private Vector3 _startPosition;

    // Cached rigidbodies so velocity can be changed later
    private Rigidbody _rb3D;
    private Rigidbody2D _rb2D;

    // Public read-only accessor used by Enemy.cs (fixes CS1061)
    public float Damage => _damage;

    public void Initialize(float speed, float damage, float range, Vector3 direction)
    {
        _speed = speed;
        _damage = damage;
        _range = Mathf.Max(0f, range);
        _direction = direction.normalized;
        _startPosition = transform.position;

        // cache references for later velocity updates
        _rb3D = GetComponent<Rigidbody>();
        if (_rb3D != null)
        {
            _rb3D.linearVelocity = _direction * _speed;
            return;
        }

        _rb2D = GetComponent<Rigidbody2D>();
        if (_rb2D != null)
        {
            var dir2D = new Vector2(_direction.x, _direction.y);
            _rb2D.linearVelocity = dir2D.normalized * _speed;
            return;
        }

        // otherwise move manually
        StartCoroutine(ManualMove());
    }

    private void Update()
    {
        if (Vector3.Distance(_startPosition, transform.position) >= _range)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ManualMove()
    {
        while (true)
        {
            transform.position += _direction * _speed * Time.deltaTime;
            yield return null;
        }
    }

    // 3D trigger collision
    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    // 2D trigger collision
    private void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(gameObject);
    }

    // --- New velocity API ---

    /// <summary>
    /// Set velocity as a 2D vector (x,y). Updates internal direction and speed.
    /// If a Rigidbody2D exists it will be applied; if a Rigidbody (3D) exists the Z will be 0.
    /// If no rigidbody exists the manual movement will use the new direction/speed.
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude <= 0f)
        {
            // stop movement
            _speed = 0f;
            return;
        }

        _direction = new Vector3(velocity.x, velocity.y, 0f).normalized;
        _speed = velocity.magnitude;

        if (_rb2D != null)
        {
            _rb2D.linearVelocity = velocity;
            return;
        }

        if (_rb3D != null)
        {
            _rb3D.linearVelocity = new Vector3(velocity.x, velocity.y, 0f);
            return;
        }

        // no rigidbody: ManualMove will use updated _direction and _speed
    }

    /// <summary>
    /// Set speed while keeping current direction. If a rigidbody exists its velocity is updated.
    /// </summary>
    public void SetSpeed(float speed)
    {
        _speed = Mathf.Max(0f, speed);

        if (_direction.sqrMagnitude <= 0f) return;

        if (_rb2D != null)
        {
            var v2 = new Vector2(_direction.x, _direction.y).normalized * _speed;
            _rb2D.linearVelocity = v2;
            return;
        }

        if (_rb3D != null)
        {
            _rb3D.linearVelocity = _direction.normalized * _speed;
            return;
        }

        // manual movement will use updated _speed
    }
}