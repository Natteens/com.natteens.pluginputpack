using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using PlugInputPack;

namespace PlugInputPack.Editor
{
    [CustomEditor(typeof(PlugInputReader))]
    public class PlugInputReaderEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputActionAssetProperty;
        private SerializedProperty _enableDebugProperty;
        private SerializedProperty _enableVisualDebugProperty;
        private SerializedProperty _debugHandleSizeProperty;
        private SerializedProperty _debugHandleColorProperty;
        
        private bool _showAdvancedSettings = false;
        private bool _showInputActions;
        
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        
        private void OnEnable()
        {
            _inputActionAssetProperty = serializedObject.FindProperty("inputActionAsset");
            _enableDebugProperty = serializedObject.FindProperty("enableDebug");
            _enableVisualDebugProperty = serializedObject.FindProperty("enableVisualDebug");
            _debugHandleSizeProperty = serializedObject.FindProperty("debugHandleSize");
            _debugHandleColorProperty = serializedObject.FindProperty("debugHandleColor");
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
            EditorGUILayout.LabelField("Plug Input Reader", _headerStyle);
            EditorGUILayout.Space(5);
            
            DrawMainConfiguration();
            
            EditorGUILayout.Space(10);
            
            DrawDebugSettings();
            
            EditorGUILayout.Space(10);
            
            DrawInputActionsInfo();
            
            EditorGUILayout.Space(10);
            
            DrawValidationAndActions();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawMainConfiguration()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Configuração Principal", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_inputActionAssetProperty, 
                new GUIContent("Input Action Asset", "Asset do Unity Input System com todas as ações de input"));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            // Quick actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Criar Input Actions", GUILayout.Height(25)))
            {
                CreateInputActionAsset();
            }
            
            if (_inputActionAssetProperty.objectReferenceValue != null)
            {
                if (GUILayout.Button("Abrir Input Actions", GUILayout.Height(25)))
                {
                    Selection.activeObject = _inputActionAssetProperty.objectReferenceValue;
                    EditorGUIUtility.PingObject(_inputActionAssetProperty.objectReferenceValue);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Configurações de Debug", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(_enableDebugProperty, 
                new GUIContent("Habilitar Debug", "Ativa logs de debug no console"));
            
            EditorGUILayout.PropertyField(_enableVisualDebugProperty, 
                new GUIContent("Debug Visual", "Mostra informações na tela durante o jogo"));
            
            if (_enableVisualDebugProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_debugHandleSizeProperty, 
                    new GUIContent("Tamanho dos Elementos", "Tamanho dos elementos visuais de debug (1-300)"));
                
                EditorGUILayout.PropertyField(_debugHandleColorProperty, 
                    new GUIContent("Cor dos Elementos", "Cor dos elementos de visualização"));
                
                EditorGUI.indentLevel--;
                
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInputActionsInfo()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.LabelField("Informações das Input Actions", EditorStyles.boldLabel);
            
            var inputActionAsset = _inputActionAssetProperty.objectReferenceValue as InputActionAsset;
            
            if (inputActionAsset == null)
            {
                EditorGUILayout.HelpBox("Nenhum Input Action Asset configurado.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Asset: {inputActionAsset.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Mapas de Ação: {inputActionAsset.actionMaps.Count}");
                
                int totalActions = 0;
                foreach (var actionMap in inputActionAsset.actionMaps)
                {
                    totalActions += actionMap.actions.Count;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  • {actionMap.name}", GUILayout.Width(150));
                    EditorGUILayout.LabelField($"{actionMap.actions.Count} ações", _labelStyle);
                    EditorGUILayout.EndHorizontal();
                    
                    if (actionMap.actions.Count <= 5)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var action in actionMap.actions)
                        {
                            EditorGUILayout.LabelField($" - {action.name} ({action.expectedControlType ?? "Any"})");
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Total de Ações: {totalActions}", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawValidationAndActions()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("✓ Validação", EditorStyles.boldLabel);
            
            var inputReader = target as PlugInputReader;
            
            if (inputReader.IsValid())
            {
                EditorGUILayout.HelpBox($"✅ Configuração válida!\n{inputReader.GetDebugInfo()}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ Configuração incompleta! Verifique se o Input Action Asset está configurado e possui ações.", MessageType.Warning);
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validar Configuração"))
            {
                bool isValid = inputReader.IsValid();
                EditorUtility.DisplayDialog(
                    "Validação do Input Reader",
                    isValid ? $"✅ Configuração válida!\n\n{inputReader.GetDebugInfo()}" : "❌ Configuração inválida!",
                    "OK"
                );
            }
            
            if (GUILayout.Button("Ping Asset"))
            {
                if (inputReader.InputActionAsset != null)
                {
                    EditorGUIUtility.PingObject(inputReader.InputActionAsset);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void CreateInputActionAsset()
        {
            var inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Criar Input Action Asset",
                "New Input Actions",
                "inputactions",
                "Escolha onde salvar o Input Action Asset"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(inputActionAsset, path);
                AssetDatabase.SaveAssets();
                
                _inputActionAssetProperty.objectReferenceValue = inputActionAsset;
                serializedObject.ApplyModifiedProperties();
                
                Selection.activeObject = inputActionAsset;
                EditorGUIUtility.PingObject(inputActionAsset);
                
                EditorUtility.DisplayDialog(
                    "Input Actions Criado",
                    "Input Action Asset criado com sucesso!\n\nAbra-o no Input Action Editor para configurar suas ações de input.",
                    "OK"
                );
            }
        }
    }
}