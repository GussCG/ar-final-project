// using System.Diagnostics;
using UnityEngine;

public class QuizSelectionManager : MonoBehaviour
{
    public static QuizSelectionManager Instance;

    string selectedProvinceId;
    GameObject selectedButton;

    void Awake()
    {
        Instance = this;
    }

    public void SelectProvince(string provinceId, GameObject button)
    {
        selectedProvinceId = provinceId;
        selectedButton = button;

        Debug.Log("Selected province: " + provinceId);
    }

    public void ProvinceClicked(MapProvince province)
    {
        if (string.IsNullOrEmpty(selectedProvinceId))
        {
            return;
        }

        if (province.TrySolve(selectedProvinceId))
        {
            GameManagerQuiz.Instance.OnProvincePlacedCorrectly();
            Destroy(selectedButton);
            selectedProvinceId = null;
        }
        else
        {
            HUDManager.Instance.ShowWrongMark();
        }
    }

}
