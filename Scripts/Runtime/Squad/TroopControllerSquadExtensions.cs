using System.Collections.Generic;
using Core.Patterns.Singleton;
using Troop;
using UnityEngine;

public class TroopControllerSquadExtensions : Singleton<TroopControllerSquadExtensions>
{
    private Dictionary<TroopController, SquadData> _squadDataMap = new Dictionary<TroopController, SquadData>();
    
    private struct SquadData
    {
        public SquadSystem squad;
        public Vector2Int position;
    }
    public void SetSquadPosition(TroopController troop, SquadSystem squad, Vector2Int position)
    {
        _squadDataMap[troop] = new SquadData { squad = squad, position = position };
    }
    public Vector2Int GetSquadPosition(TroopController troop)
    {
        if (_squadDataMap.TryGetValue(troop, out SquadData data))
        {
            return data.position;
        }
        return new Vector2Int(-1, -1);
    }
    
    public SquadSystem GetSquad(TroopController troop)
    {
        if (_squadDataMap.TryGetValue(troop, out SquadData data))
        {
            return data.squad;
        }
        return null;
    }
}