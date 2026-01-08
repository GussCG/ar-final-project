// GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void IncreasePointCount()
    {
        score++;
        Debug.Log("Score: " + score);
        // Hook for Goal 2: Reset Timer logic here later
    }

    public void DecreasePointCount()
    {
        score--;
        Debug.Log("Score: " + score);
        // Hook for Goal 2: Accelerate Timer decrease here later
    }
}