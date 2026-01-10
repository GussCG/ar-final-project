using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("ScrollView")]
    public Transform content;
    public Button provinceButtonPrefab;

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject wrongMark; 

    void Awake()
    {
        Instance = this;
    }

    public void LoadHUD(List<ProvinceDefinition> provinces)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var province in provinces)
        {
            Button btn = Instantiate(provinceButtonPrefab, content);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = province.displayName;

            btn.onClick.AddListener(() =>
            {
                QuizSelectionManager.Instance.SelectProvince(
                    province.provinceId,
                    btn.gameObject
                );
            });
        }
    }

    public void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
    }

    void Update()
    {
        if (GameManagerQuiz.Instance == null) return;
        timerText.text = FormatTime(GameManagerQuiz.Instance.timer.ElapsedTime);
    }

    string FormatTime(float time)
    {
        int m = Mathf.FloorToInt(time / 60f);
        int s = Mathf.FloorToInt(time % 60f);
        int ms = Mathf.FloorToInt((time * 1000f) % 1000f);
        return $"{m:00}:{s:00}:{ms:000}";
    }

    public void ShowWrongMark()
    {
        wrongMark.SetActive(true);
        CancelInvoke();
        Invoke(nameof(HideWrongMark), 1.2f);
    }

    void HideWrongMark()
    {
        wrongMark.SetActive(false);
    }

    public void ShowEndScreen(int finalScore, float finalTime)
    {
        // Implementation for showing end screen with final score and time
        Debug.Log($"Game Over! Final Score: {finalScore}, Final Time: {FormatTime(finalTime)}");
    }
}
