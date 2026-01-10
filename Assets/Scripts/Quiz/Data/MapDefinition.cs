using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "MapDefinition",
    menuName = "Quiz/Map Definition"
)]
public class MapDefinition : ScriptableObject
{
    public string mapId;
    public string displayName;

    public List<ProvinceDefinition> provinces;
}
