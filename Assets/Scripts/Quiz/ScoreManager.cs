using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int CurrentScore {get; private set; }

    public void ResetScore()
    {
        CurrentScore = 0;
    }

    public void AddPoints(int points)
    {
        CurrentScore += points;
    }
}
