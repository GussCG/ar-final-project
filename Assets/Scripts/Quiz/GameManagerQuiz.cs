using UnityEngine;
using System.Collections.Generic;

public class GameManagerQuiz : MonoBehaviour
{
    public static GameManagerQuiz Instance;

    public MapDefinition CurrentMap { get; private set; }

    public TimerManager timer;
    public ScoreManager score;
    public HUDManager hud;

    private int totalProvinces;
    private int correctPlaced;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        var context = FindObjectOfType<MapContext>();
        LoadMap(context.mapDefinition);
    }

    void LoadMap(MapDefinition map)
    {
        CurrentMap = map;

        totalProvinces = map.provinces.Count;
        correctPlaced = 0;

        score.ResetScore();
        timer.StartTimer();
        hud.LoadHUD(map.provinces);
    }

    public void OnProvincePlacedCorrectly()
    {
        correctPlaced++;
        score.AddPoints(10);
        hud.UpdateScore(score.CurrentScore);

        if (correctPlaced >= totalProvinces)
        {
            FinishGame();
        }
    }

    void FinishGame()
    {
        timer.StopTimer();
        SaveManager.SaveResult(
            CurrentMap.mapId,
            score.CurrentScore,
            timer.ElapsedTime
        );

        hud.ShowEndScreen(
            score.CurrentScore,
            timer.ElapsedTime
        );
    }
}
