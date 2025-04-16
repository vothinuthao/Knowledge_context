using Core;
using UnityEngine;

public class SquadBase
{
    public string SquadName;
    public GameObject SquadBannerPrefab;

    protected SquadBase(string squadName)
    {
        SquadName = squadName;
    }
}