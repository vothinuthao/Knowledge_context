using System;
using UnityEngine;
using Core.DI;
using Core.ECS;
using Core.Grid;
using Managers;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private DiInitializer diInitializer;
    
    private ServiceContainer _serviceContainer;
    private World _world;
    
    [Obsolete("Obsolete")]
    private void Awake()
    {
        Debug.Log("[1] GameBootstrap - Initializing...");
        _serviceContainer = new ServiceContainer();
        ServiceLocator.Initialize(_serviceContainer);
        Debug.Log("[2] ServiceContainer initialized");
        _world = new World();
        _serviceContainer.RegisterSingleton<World>(_world);
        Debug.Log("[3] World created and registered with ServiceLocator");
        
        // 3. Khởi tạo WorldManager
        if (worldManager != null)
        {
            worldManager.SetWorld(_world);
            Debug.Log("[4] World assigned to WorldManager");
        }
        else
        {
            Debug.LogError("WorldManager not assigned!");
        }
        
        if (gridManager != null)
        {
            StartCoroutine(InitializeGridAfterWorld());
            Debug.Log("[5] Scheduled GridManager initialization");
        }

        if (diInitializer != null)
        {
            diInitializer.Initialize();
        }
    }
    
    private System.Collections.IEnumerator InitializeGridAfterWorld()
    {
        yield return null;
        
        _serviceContainer.RegisterSingleton<GridManager>(gridManager);
        
        gridManager.InitializeGrid();
        Debug.Log("[6] GridManager initialized");
    }
}