using TMPro;
using UnityEngine;

public class Score : MonoBehaviour
{
    public int score;
    public int amounttoadd;
    public TextMeshProUGUI scoretext;
    public int highScore;
    public TextMeshProUGUI Highscoretext;



    public void addscore()
    {
        if (score > highScore)
        {
            highScore = score;

           
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();

           
            Highscoretext.text = highScore.ToString();
        }
    }

    private void Reset()
    {
        score = 0;
    }

    public void Update()
    {
        scoretext.text = "score : " + $"{score}";
    }


}

