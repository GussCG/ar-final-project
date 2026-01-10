using UnityEngine;

public class ProvinceClickable : MonoBehaviour
{
    MapProvince province;

    void Awake()
    {
        province = GetComponent<MapProvince>();
    }

    void OnMouseDown()
    {
        Debug.Log("Province clicked: " + province.name);
        QuizSelectionManager.Instance.ProvinceClicked(province);
    }
}
