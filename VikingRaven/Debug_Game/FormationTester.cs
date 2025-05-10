using System.Collections;
using UnityEngine;
using VikingRaven.Units;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Công cụ thử nghiệm và kiểm tra tất cả các loại formation tự động
    /// </summary>
    public class FormationTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private int _targetSquadId = 1;
        [SerializeField] private bool _autoTest = false;
        [SerializeField] private float _formationChangeInterval = 3.0f;
        [SerializeField] private bool _visualizeFormationChanges = true;
        
        [Header("Formation Test Order")]
        [SerializeField] private FormationType[] _testFormationOrder;
        
        private FormationSystem _formationSystem;
        private FormationDebugVisualizer _debugVisualizer;
        private FormationDebugAnalyzer _debugAnalyzer;
        private int _currentFormationIndex = 0;
        private bool _isTesting = false;
        
        private void Start()
        {
            // Tìm FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogError("FormationTester: Không tìm thấy FormationSystem!");
                return;
            }
            
            // Khởi tạo thứ tự test mặc định nếu chưa được set
            if (_testFormationOrder == null || _testFormationOrder.Length == 0)
            {
                _testFormationOrder = new FormationType[]
                {
                    FormationType.Line,
                    FormationType.Column,
                    FormationType.Phalanx, 
                    FormationType.Testudo,
                    FormationType.Circle,
                    FormationType.Normal
                };
            }
            
            // Tìm hoặc tạo mới visualizer
            if (_visualizeFormationChanges)
            {
                SetupDebugVisualizer();
                SetupDebugAnalyzer();
            }
            
            // Bắt đầu test tự động nếu được bật
            if (_autoTest)
            {
                StartFormationTest();
            }
        }
        
        private void Update()
        {
            // Bắt phím tắt để bắt đầu/dừng test
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (_isTesting)
                {
                    StopFormationTest();
                }
                else
                {
                    StartFormationTest();
                }
            }
            
            // Phím tắt để chuyển sang formation tiếp theo
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TestNextFormation();
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("<b>FORMATION TESTER</b>", GUI.skin.label);
            GUILayout.Space(5);
            
            GUILayout.Label($"Target Squad: {_targetSquadId}");
            GUILayout.Label($"Auto Test: {(_isTesting ? "Running" : "Stopped")}");
            
            if (_formationSystem != null)
            {
                FormationType currentType = _formationSystem.GetCurrentFormationType(_targetSquadId);
                GUILayout.Label($"Current Formation: {currentType}");
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button(_isTesting ? "Stop Test (F5)" : "Start Test (F5)", GUILayout.Height(30)))
            {
                if (_isTesting)
                {
                    StopFormationTest();
                }
                else
                {
                    StartFormationTest();
                }
            }
            
            if (GUILayout.Button("Test Next Formation (F6)", GUILayout.Height(30)))
            {
                TestNextFormation();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Bắt đầu test tự động các loại formation
        /// </summary>
        public void StartFormationTest()
        {
            if (_isTesting) return;
            
            Debug.Log("FormationTester: Bắt đầu test tự động các loại formation...");
            _isTesting = true;
            _currentFormationIndex = 0;
            
            // Apply first formation
            ApplyCurrentFormation();
            
            // Start coroutine for automatic testing
            StartCoroutine(AutoFormationTestCoroutine());
        }
        
        /// <summary>
        /// Dừng test tự động
        /// </summary>
        public void StopFormationTest()
        {
            if (!_isTesting) return;
            
            Debug.Log("FormationTester: Dừng test tự động.");
            _isTesting = false;
            StopAllCoroutines();
        }
        
        /// <summary>
        /// Test formation tiếp theo trong danh sách
        /// </summary>
        public void TestNextFormation()
        {
            _currentFormationIndex = (_currentFormationIndex + 1) % _testFormationOrder.Length;
            ApplyCurrentFormation();
        }
        
        /// <summary>
        /// Áp dụng formation hiện tại trong danh sách test
        /// </summary>
        private void ApplyCurrentFormation()
        {
            if (_formationSystem == null) return;
            
            FormationType formationType = _testFormationOrder[_currentFormationIndex];
            _formationSystem.ChangeFormation(_targetSquadId, formationType);
            
            Debug.Log($"FormationTester: Áp dụng đội hình {formationType} cho squad {_targetSquadId}");
            
            // Cập nhật analyzer nếu có
            if (_debugAnalyzer != null)
            {
                _debugAnalyzer.AnalyzeSquad(_targetSquadId);
            }
        }
        
        /// <summary>
        /// Coroutine tự động thay đổi formation theo interval
        /// </summary>
        private IEnumerator AutoFormationTestCoroutine()
        {
            while (_isTesting)
            {
                yield return new WaitForSeconds(_formationChangeInterval);
                
                if (!_isTesting) break;
                
                TestNextFormation();
            }
        }
        
        /// <summary>
        /// Thiết lập debug visualizer
        /// </summary>
        private void SetupDebugVisualizer()
        {
            _debugVisualizer = FindObjectOfType<FormationDebugVisualizer>();
            
            if (_debugVisualizer == null)
            {
                GameObject visualizerObj = new GameObject("FormationDebugVisualizer");
                _debugVisualizer = visualizerObj.AddComponent<FormationDebugVisualizer>();
            }
        }
        
        /// <summary>
        /// Thiết lập debug analyzer
        /// </summary>
        private void SetupDebugAnalyzer()
        {
            _debugAnalyzer = FindObjectOfType<FormationDebugAnalyzer>();
            
            if (_debugAnalyzer == null)
            {
                GameObject analyzerObj = new GameObject("FormationDebugAnalyzer");
                _debugAnalyzer = analyzerObj.AddComponent<FormationDebugAnalyzer>();
                _debugAnalyzer.SetTargetSquad(_targetSquadId);
            }
        }
    }
}