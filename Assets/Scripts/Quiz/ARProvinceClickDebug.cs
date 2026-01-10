using UnityEngine;
using UnityEngine.InputSystem;

public class ARProvinceClickDebug : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            MapProvince province = hit.collider.GetComponent<MapProvince>();

            if (province != null)
            {
                QuizSelectionManager.Instance.ProvinceClicked(province);
            }
        }
    }
}
