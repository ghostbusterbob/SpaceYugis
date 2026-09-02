
using TMPro;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float hp = 5f;
    public float maxhp = 5f;
    public float damage;

    public TextMeshProUGUI healthtext;
    public GameObject[] lives;

    private int heartToRemove;

    public void Damage()
    {
       
        hp -= damage;

        
        hp = Mathf.Clamp(hp, 0, maxhp);

        
        healthtext.text = "Health: " + hp;

        
        heartToRemove = Mathf.CeilToInt(hp);

        
        if (heartToRemove < lives.Length)
        {
            lives[heartToRemove].SetActive(false);
        }
    }

    private void Start()
    {
        healthtext.text = "Health: " + hp;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Meteor"))
        {
            damage = 5f;
            Damage();
        }
    }
}

