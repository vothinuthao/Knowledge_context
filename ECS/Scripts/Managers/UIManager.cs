// File: Managers/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using Components.Squad;
using Core.ECS;

namespace Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _hudPanel;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private GameObject _defeatPanel;
        
        [Header("HUD Elements")]
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _squadCountText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _selectedSquadIndicator;
        
        [Header("Squad Info")]
        [SerializeField] private GameObject _squadInfoPanel;
        [SerializeField] private Text _squadHealthText;
        [SerializeField] private Text _squadMoraleText;
        [SerializeField] private Text _squadStateText;
        
        private GameManager _gameManager;
        
        private void Start()
        {
            _gameManager = GameManager.Instance;
            
            // Subscribe to events
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged += HandleGameStateChanged;
            }
            
            // Initial setup
            ShowPanel(_mainMenuPanel);
        }
        
        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        private void Update()
        {
            if (_gameManager == null) return;
            
            UpdateHUD();
        }
        
        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            switch (newState)
            {
                case GameState.READY:
                    ShowPanel(_mainMenuPanel);
                    break;
                    
                case GameState.PLAYING:
                    ShowPanel(_hudPanel);
                    break;
                    
                case GameState.PAUSED:
                    ShowPanel(_pausePanel);
                    break;
                    
                case GameState.VICTORY:
                    ShowVictoryScreen();
                    break;
                    
                case GameState.DEFEAT:
                    ShowDefeatScreen();
                    break;
            }
        }
        
        private void UpdateHUD()
        {
            // Update gold display
            if (_goldText != null)
            {
                _goldText.text = $"Gold: {0}"; // TODO: Add resource system
            }
            
            // Update squad count
            if (_squadCountText != null)
            {
                int playerSquads = _gameManager.GetSquadCountByFaction(Faction.PLAYER);
                _squadCountText.text = $"Squads: {playerSquads}";
            }
            
            // Update timer
            if (_timerText != null)
            {
                TimeSpan time = TimeSpan.FromSeconds(Time.time);
                _timerText.text = $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }
        
        public void ShowVictoryScreen()
        {
            ShowPanel(_victoryPanel);
        }
        
        public void ShowDefeatScreen()
        {
            ShowPanel(_defeatPanel);
        }
        
        public void UpdateSquadInfo(Entity squad)
        {
            if (squad == null)
            {
                _squadInfoPanel?.SetActive(false);
                return;
            }
            
            _squadInfoPanel?.SetActive(true);
            
            var squadComponent = squad.GetComponent<SquadComponent>();
            if (squadComponent != null)
            {
                if (_squadStateText != null)
                    _squadStateText.text = $"State: {squadComponent.State}";
                
                if (_squadMoraleText != null)
                    _squadMoraleText.text = $"Morale: {squadComponent.MoraleValue:F1}";
            }
        }
        
        private void ShowPanel(GameObject panel)
        {
            // Hide all panels
            _mainMenuPanel?.SetActive(false);
            _hudPanel?.SetActive(false);
            _pausePanel?.SetActive(false);
            _victoryPanel?.SetActive(false);
            _defeatPanel?.SetActive(false);
            
            // Show requested panel
            panel?.SetActive(true);
        }
        
        #region Button Callbacks
        public void OnStartGameClicked()
        {
            _gameManager?.StartGame();
        }
        
        public void OnResumeClicked()
        {
            _gameManager?.ResumeGame();
        }
        
        public void OnRestartClicked()
        {
            _gameManager?.RestartGame();
        }
        
        public void OnQuitClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        #endregion
    }
}