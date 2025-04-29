using System;
using UnityEngine;
using Core.DI;
using Core.ECS;
using Core.Grid;
using Managers;

public class DiInitializer : MonoBehaviour
{
    [Obsolete("Obsolete")]
    public void Initialize()
    {
        ServiceContainer container = new ServiceContainer();
        ServiceLocator.Initialize(container);
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
            container.RegisterSingleton<GridManager>(gridManager);
        WorldManager worldManager = FindObjectOfType<WorldManager>();
        if (worldManager != null && worldManager.World != null)
        {
            container.RegisterSingleton<World>(worldManager.World);
            Debug.Log("World registered with ServiceLocator");
        }
        else
        {
            Debug.LogError("WorldManager or World is null, cannot register with ServiceLocator");
        }
            
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            container.RegisterSingleton<GameManager>(gameManager);
        Debug.Log("DI Container initialized");
    }
}