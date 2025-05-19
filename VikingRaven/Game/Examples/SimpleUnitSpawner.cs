// using UnityEngine;
// using VikingRaven.Core.Factory;
// using VikingRaven.Units.Components;
//
// namespace VikingRaven.Game.Examples
// {
//     public class SimpleUnitSpawner : MonoBehaviour
//     {
//         [SerializeField] private UnitFactory _unitFactory;
//         [SerializeField] private SquadFactory _squadFactory;
//         [SerializeField] private Transform[] _playerSpawnPoints;
//         [SerializeField] private Transform[] _enemySpawnPoints;
//         
//         [SerializeField] private bool _spawnOnStart = true;
//         [SerializeField] private int _infantryPerSquad = 4;
//         [SerializeField] private int _archerPerSquad = 2;
//         [SerializeField] private int _pikePerSquad = 2;
//         
//         private void Start()
//         {
//             if (_spawnOnStart)
//             {
//                 SpawnAllUnits();
//             }
//         }
//
//         public void SpawnAllUnits()
//         {
//             SpawnPlayerUnits();
//             SpawnEnemyUnits();
//         }
//
//         public void SpawnPlayerUnits()
//         {
//             if (_playerSpawnPoints.Length == 0)
//             {
//                 Debug.LogError("No player spawn points defined");
//                 return;
//             }
//             
//             if (_playerSpawnPoints.Length > 0)
//             {
//                 var playerSquad = CreateMixedSquad(_playerSpawnPoints[0].position, 1);
//                 Debug.Log($"Created player squad with {playerSquad.Count} units");
//             }
//             
//             // Create a second player squad if there's another spawn point
//             if (_playerSpawnPoints.Length > 1)
//             {
//                 var playerSquad2 = CreateSpecializedSquad(UnitType.Pike, _playerSpawnPoints[1].position, 2);
//                 Debug.Log($"Created secondary player squad with {playerSquad2.Count} units");
//             }
//         }
//
//         public void SpawnEnemyUnits()
//         {
//             if (_enemySpawnPoints.Length == 0)
//             {
//                 Debug.LogError("No enemy spawn points defined");
//                 return;
//             }
//             
//             // Create an enemy squad at the first spawn point
//             if (_enemySpawnPoints.Length > 0)
//             {
//                 var enemySquad = CreateSpecializedSquad(UnitType.Infantry, _enemySpawnPoints[0].position, 101);
//                 Debug.Log($"Created enemy squad with {enemySquad.Count} units");
//             }
//             
//             // Create a second enemy squad if there's another spawn point
//             if (_enemySpawnPoints.Length > 1)
//             {
//                 var enemySquad2 = CreateSpecializedSquad(UnitType.Archer, _enemySpawnPoints[1].position, 102);
//                 Debug.Log($"Created secondary enemy squad with {enemySquad2.Count} units");
//             }
//         }
//
//         private System.Collections.Generic.List<Core.ECS.IEntity> CreateMixedSquad(Vector3 position, int squadId)
//         {
//             System.Collections.Generic.Dictionary<UnitType, int> unitCounts = new System.Collections.Generic.Dictionary<UnitType, int>
//             {
//                 { UnitType.Infantry, _infantryPerSquad },
//                 { UnitType.Archer, _archerPerSquad },
//                 { UnitType.Pike, _pikePerSquad }
//             };
//             
//             return _squadFactory.CreateMixedSquad(unitCounts, position, Quaternion.identity);
//         }
//
//         private System.Collections.Generic.List<Core.ECS.IEntity> CreateSpecializedSquad(UnitType unitType, Vector3 position, int squadId)
//         {
//             // Create a squad with only one type of unit
//             int count = 0;
//             
//             switch (unitType)
//             {
//                 case UnitType.Infantry:
//                     count = _infantryPerSquad;
//                     break;
//                 case UnitType.Archer:
//                     count = _archerPerSquad;
//                     break;
//                 case UnitType.Pike:
//                     count = _pikePerSquad;
//                     break;
//             }
//             
//             return _squadFactory.CreateSquad(unitType, count, position, Quaternion.identity);
//         }
//     }
// }