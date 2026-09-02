using UnityEngine;

public class enemyHealth : MonoBehaviour
{
    public float Enemyhp;

    public void Update()
    {
        if (Enemyhp <= 0)
        {
            die();
        }
    }

    private void die()
    {
        gameObject.SetActive(false);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullets"))
        {
            Enemyhp--;
        }
    }

}
