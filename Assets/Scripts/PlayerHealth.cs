using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxLives = 3;

    [Header("Player")]
    [SerializeField] private int playerNumber = 1;

    private int currentLives;

    public int CurrentLives => currentLives;
    public bool IsDead => currentLives <= 0;

    private static PlayerHealth player1;
    private static PlayerHealth player2;

    private void Awake()
    {
        if (playerNumber == 1)
        {
            player1 = this;
        }
        else if (playerNumber == 2)
        {
            player2 = this;
        }
    }

    private void Start()
    {
        currentLives = maxLives;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        int damageAmount =
            Mathf.Max(1, Mathf.RoundToInt(damage));

        currentLives -= damageAmount;

        Debug.Log(
            "Player " +
            playerNumber +
            " geraakt! Lives over: " +
            currentLives
        );

        if (currentLives <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
        Debug.Log(
            "Player " +
            playerNumber +
            " is dood!"
        );

        CheckGameOver();
    }

    private static void CheckGameOver()
    {
        // Wacht totdat beide spelers bestaan
        if (player1 == null || player2 == null)
            return;

        // Alleen Game Over als BEIDE spelers dood zijn
        if (player1.IsDead && player2.IsDead)
        {
            GameOver();
        }
    }

    private static void GameOver()
    {
        Debug.Log("GAME OVER - Beide spelers zijn dood!");

        Time.timeScale = 0f;
    }
}