using UnityEngine;

public class MapProvince : MonoBehaviour
{
    [SerializeField] private string provinceId;
    public string ProvinceId => provinceId;

    public Color correctColor = Color.green;

    MeshRenderer mesh;
    Color originalColor;
    bool solved;

    void Awake()
    {
        mesh = GetComponent<MeshRenderer>();
        originalColor = mesh.material.color;
    }

    public bool TrySolve(string selectedId)
    {
        if (solved) return false;

        if (selectedId == provinceId)
        {
            solved = true;
            mesh.material.color = correctColor;
            return true;
        }

        return false;
    }
}
