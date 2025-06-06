using UnityEngine;
using UnityEditor;
using PlugInputPack;

namespace PlugInputPack.Editor
{
    [CustomEditor(typeof(PlugInputComponent))]
    public class PlugInputComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputReaderProperty;
        private bool _showDebugInfo;
        private bool _showEvents;
        
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        
        private void OnEnable()
        {
            _inputReaderProperty = serializedObject.FindProperty("inputReader");
        }
        
        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(0.3f, 0.8f, 1f) }
                };
                
                _boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
                
                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true
                };
            }
        }
        
        public override void OnInspectorGUI()
        {
            InitializeStyles();
            
            serializedObject.Update();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Plug Input Component", _headerStyle);
            EditorGUILayout.Space(5);
            
            DrawInputReaderSection();
            
            EditorGUILayout.Space(10);
            
            if (Application.isPlaying)
            {
                DrawRuntimeDebugSection();
            }
            else
            {
                DrawDesignTimeInfo();
            }
            
            DrawEventsSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawInputReaderSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Configuração", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_inputReaderProperty, new GUIContent("Input Reader", "ScriptableObject com as configurações de input"));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            var inputReader = _inputReaderProperty.objectReferenceValue as PlugInputReader;
            if (inputReader == null)
            {
                EditorGUILayout.HelpBox("⚠️Input Reader não configurado! Crie um PlugInputReader para usar o sistema.", MessageType.Warning);
                
                if (GUILayout.Button("Criar PlugInputReader"))
                {
                    CreateInputReader();
                }
            }
            else
            {
                if (inputReader.InputActionAsset == null)
                {
                    EditorGUILayout.HelpBox("⚠️Input Action Asset não configurado no Input Reader!", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"✅Configurado com: {inputReader.GetDebugInfo()}", MessageType.Info);
                }
                
                if (GUILayout.Button("Abrir Input Reader"))
                {
                    Selection.activeObject = inputReader;
                    EditorGUIUtility.PingObject(inputReader);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRuntimeDebugSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            _showDebugInfo = EditorGUILayout.Foldout(_showDebugInfo, "🔍 Informações de Runtime", true, EditorStyles.foldoutHeader);
            
            if (_showDebugInfo)
            {
                var component = target as PlugInputComponent;
                
                EditorGUILayout.LabelField("Status do Sistema:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Sistema Inicializado", "✅", _labelStyle);
                
                var cacheField = typeof(PlugInputComponent).GetField("_cache", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (cacheField != null)
                {
                    if (cacheField.GetValue(component) is PlugInputCache cache)
                    {
                        EditorGUILayout.LabelField("• Cache:", cache.GetCacheStats(), _labelStyle);
                    }
                }
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Forçar Atualização"))
                {
                    EditorUtility.SetDirty(target);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDesignTimeInfo()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Informações", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Execute o jogo para ver informações de runtime.", _labelStyle);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEventsSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            _showEvents = EditorGUILayout.Foldout(_showEvents, "Eventos Disponíveis", true, EditorStyles.foldoutHeader);
            
            if (_showEvents)
            {
                EditorGUILayout.LabelField("Eventos Estáticos:", EditorStyles.boldLabel);
                
                var events = new[]
                {
                    "OnInputPerformed - Quando um input é executado",
                    "OnInputCanceled - Quando um input é cancelado", 
                    "OnInputPressed - Quando um input é pressionado",
                    "OnInputReleased - Quando um input é solto",
                    "OnInputValueChanged - Quando um valor float muda",
                    "OnInputVector2Changed - Quando um valor Vector2 muda",
                    "OnInputStateChanged - Quando um estado bool muda",
                    "OnInputSystemInitialized - Quando o sistema é inicializado",
                    "OnInputSystemDestroyed - Quando o sistema é destruído"
                };
                
                foreach (var eventInfo in events)
                {
                    EditorGUILayout.LabelField($"• {eventInfo}", _labelStyle);
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Use esses eventos para reagir aos inputs em seus scripts.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void CreateInputReader()
        {
            var inputReader = CreateInstance<PlugInputReader>();
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Criar PlugInputReader",
                "New PlugInputReader",
                "asset",
                "Escolha onde salvar o PlugInputReader"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(inputReader, path);
                AssetDatabase.SaveAssets();
                
                _inputReaderProperty.objectReferenceValue = inputReader;
                serializedObject.ApplyModifiedProperties();
                
                Selection.activeObject = inputReader;
                EditorGUIUtility.PingObject(inputReader);
            }
        }
    }
}