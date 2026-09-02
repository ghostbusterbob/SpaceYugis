using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight bullet component that sets motion.
/// Works with both 3D (Rigidbody) and 2D (Rigidbody2D) setups.
/// </summary>
public class Bullet : MonoBehaviour
{
    private float _speed;
    private float _damage;
    private float _range;
    private Vector3 _direction;
    private Vector3 _startPosition;

    public void Initialize(float speed, float damage, float range, Vector3 direction)
    {
        _speed = speed;
        _damage = damage;
        _range = Mathf.Max(0f, range);
        _direction = direction.normalized;
        _startPosition = transform.position;

        // prefer existing rigidbodies
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = _direction * _speed;
            return;
        }

        var rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.gravityScale = 0f;
            var dir2D = new Vector2(_direction.x, _direction.y);
            rb2d.linearVelocity = dir2D.normalized * _speed;
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
}