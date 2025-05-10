using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units;
using VikingRaven.Units.Components;
using VikingRaven.Units.Systems;

namespace VikingRaven.Debug_Game
{
    /// <summary>
    /// Công cụ điều khiển formation để dễ dàng thử nghiệm các loại đội hình khác nhau
    /// </summary>
    public class FormationController : MonoBehaviour
    {
        [SerializeField] private int _targetSquadId = 1;
        [SerializeField] private FormationType _currentFormationType = FormationType.Line;
        [SerializeField] private bool _showDebugInfo = true;

        private FormationSystem _formationSystem;
        private FormationDebugAnalyzer _debugAnalyzer;

        private void Start()
        {
            // Tìm FormationSystem
            _formationSystem = FindObjectOfType<FormationSystem>();
            if (_formationSystem == null)
            {
                Debug.LogError("FormationController: Không tìm thấy FormationSystem!");
            }

            // Tạo FormationDebugAnalyzer nếu hiển thị debug
            if (_showDebugInfo)
            {
                CreateDebugAnalyzer();
            }

            // Áp dụng formation type ban đầu
            ApplyFormation(_currentFormationType);
        }

        private void OnGUI()
        {
            if (!_showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 250, 400));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("<b>FORMATION CONTROLLER</b>", GUI.skin.label);
            GUILayout.Space(10);

            GUILayout.Label($"Current Squad: {_targetSquadId}");
            int newSquadId = (int)GUILayout.HorizontalSlider(_targetSquadId, 1, 10, GUILayout.Width(200));
            if (newSquadId != _targetSquadId)
            {
                _targetSquadId = newSquadId;
                // Cập nhật formation type hiện tại
                UpdateCurrentFormationTypeInfo();
            }

            GUILayout.Space(15);
            GUILayout.Label($"Current Formation: {_currentFormationType}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Line", GUILayout.Height(30)))
            {
                ApplyFormation(FormationType.Line);
            }
            if (GUILayout.Button("Column", GUILayout.Height(30)))
            {
                ApplyFormation(FormationType.Column);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Phalanx", GUILayout.Height(30)))
            {
                ApplyFormation(FormationType.Phalanx);
            }
            if (GUILayout.Button("Testudo", GUILayout.Height(30)))
            {
                ApplyFormation(FormationType.Testudo);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Circle", GUILayout.Height(30)))
            {
                ApplyFormation(FormationType.Circle);
            }
            if (GUILayout.Button("Normal 3x3", GUILayout.Height(30)))
            {
                ApplyFormation(FormationType.Normal);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            if (GUILayout.Button("Analyze Formation", GUILayout.Height(30)))
            {
                AnalyzeFormation();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Áp dụng loại đội hình mới cho đội được chọn
        /// </summary>
        public void ApplyFormation(FormationType formationType)
        {
            if (_formationSystem == null)
            {
                Debug.LogError("FormationController: Không thể áp dụng đội hình - FormationSystem chưa khởi tạo!");
                return;
            }

            Debug.Log($"Áp dụng đội hình {formationType} cho squad {_targetSquadId}");
            _formationSystem.ChangeFormation(_targetSquadId, formationType);
            _currentFormationType = formationType;

            // Cập nhật analyzer nếu có
            if (_debugAnalyzer != null)
            {
                _debugAnalyzer.AnalyzeSquad(_targetSquadId);
            }
        }

        /// <summary>
        /// Cập nhật thông tin formation type hiện tại từ FormationSystem
        /// </summary>
        private void UpdateCurrentFormationTypeInfo()
        {
            if (_formationSystem == null) return;

            _currentFormationType = _formationSystem.GetCurrentFormationType(_targetSquadId);
            Debug.Log($"Squad {_targetSquadId} đang sử dụng đội hình {_currentFormationType}");
        }

        /// <summary>
        /// Tạo và cấu hình FormationDebugAnalyzer
        /// </summary>
        private void CreateDebugAnalyzer()
        {
            // Kiểm tra nếu đã có
            _debugAnalyzer = FindObjectOfType<FormationDebugAnalyzer>();
            
            // Tạo mới nếu chưa có
            if (_debugAnalyzer == null)
            {
                GameObject analyzerObj = new GameObject("FormationDebugAnalyzer");
                _debugAnalyzer = analyzerObj.AddComponent<FormationDebugAnalyzer>();
            }

            // Cấu hình
            _debugAnalyzer.SetTargetSquad(_targetSquadId);
        }

        /// <summary>
        /// Thực hiện phân tích đội hình hiện tại
        /// </summary>
        private void AnalyzeFormation()
        {
            if (_debugAnalyzer != null)
            {
                _debugAnalyzer.AnalyzeSquad(_targetSquadId);
            }
        }
    }
}