using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

namespace PlugInputPack.Editor
{
    [CustomEditor(typeof(PlugInputReader))]
    public class PlugInputReaderEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputActionAsset;
        private SerializedProperty _enableDebug;
        private SerializedProperty _enableVisualDebug;
        private SerializedProperty _debugHandleSize;
        private SerializedProperty _debugHandleColor;
        private SerializedProperty _enableDeviceManagement;
        private SerializedProperty _strictDeviceIsolation;
        private SerializedProperty _deviceSwitchCooldown;
        private SerializedProperty _allowedDevices;
        private SerializedProperty _lockCursorOnStart;
        private SerializedProperty _autoLockCursorOnGamepad;

        private bool _showDeviceSettings = true;
        private bool _showCursorSettings = true;
        private bool _showDebugSettings  = true;
        private bool _showActionsInfo;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;

        private void OnEnable()
        {
            _inputActionAsset        = serializedObject.FindProperty("inputActionAsset");
            _enableDebug             = serializedObject.FindProperty("enableDebug");
            _enableVisualDebug       = serializedObject.FindProperty("enableVisualDebug");
            _debugHandleSize         = serializedObject.FindProperty("debugHandleSize");
            _debugHandleColor        = serializedObject.FindProperty("debugHandleColor");
            _enableDeviceManagement  = serializedObject.FindProperty("enableDeviceManagement");
            _strictDeviceIsolation   = serializedObject.FindProperty("strictDeviceIsolation");
            _deviceSwitchCooldown    = serializedObject.FindProperty("deviceSwitchCooldown");
            _allowedDevices          = serializedObject.FindProperty("allowedDevices");
            _lockCursorOnStart       = serializedObject.FindProperty("lockCursorOnStart");
            _autoLockCursorOnGamepad = serializedObject.FindProperty("autoLockCursorOnGamepad");
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal   = { textColor = new Color(0.3f, 0.8f, 1f) }
            };
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
            _labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Plug Input Reader", _headerStyle);
            EditorGUILayout.Space(8);

            DrawInputSection();
            EditorGUILayout.Space(6);
            DrawDeviceSection();
            EditorGUILayout.Space(6);
            DrawCursorSection();
            EditorGUILayout.Space(6);
            DrawDebugSection();
            EditorGUILayout.Space(6);
            DrawActionsInfo();
            EditorGUILayout.Space(6);
            DrawValidation();

            serializedObject.ApplyModifiedProperties();
        }

        // -------------------------------------------------------------------------

        private void DrawInputSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Input Action Asset", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_inputActionAsset, new GUIContent("Asset", "The Unity Input System asset that defines all input actions."));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New Asset", GUILayout.Height(24)))
                CreateInputActionAsset();

            if (_inputActionAsset.objectReferenceValue != null)
            {
                if (GUILayout.Button("Open Asset", GUILayout.Height(24)))
                {
                    Selection.activeObject = _inputActionAsset.objectReferenceValue;
                    EditorGUIUtility.PingObject(_inputActionAsset.objectReferenceValue);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawDeviceSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showDeviceSettings = EditorGUILayout.Foldout(_showDeviceSettings, "Device Management", true, EditorStyles.foldoutHeader);

            if (_showDeviceSettings)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(_enableDeviceManagement, new GUIContent("Enable", "Detect and track which input device the player is using."));

                if (_enableDeviceManagement.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_strictDeviceIsolation,
                        new GUIContent("Strict Isolation", "Only process inputs from the currently active device. Prevents ghost inputs when switching between keyboard and gamepad."));
                    EditorGUILayout.PropertyField(_deviceSwitchCooldown,
                        new GUIContent("Switch Cooldown", "Minimum seconds before the active device can change again."));
                    EditorGUILayout.PropertyField(_allowedDevices,
                        new GUIContent("Allowed Devices", "Whitelist of device types. Leave empty to allow all."));
                    EditorGUI.indentLevel--;

                    if (_strictDeviceIsolation.boolValue)
                        EditorGUILayout.HelpBox("Strict Isolation is on. Only the active device's inputs will be processed.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Device management is disabled. All inputs will be processed regardless of source.", MessageType.None);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCursorSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showCursorSettings = EditorGUILayout.Foldout(_showCursorSettings, "Cursor", true, EditorStyles.foldoutHeader);

            if (_showCursorSettings)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(_lockCursorOnStart,
                    new GUIContent("Lock On Start", "Lock and hide the cursor when the scene starts."));
                EditorGUILayout.PropertyField(_autoLockCursorOnGamepad,
                    new GUIContent("Auto Lock On Gamepad", "Lock cursor when a gamepad/joystick/XR controller is active. Unlock when switching back to keyboard or mouse."));
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showDebugSettings = EditorGUILayout.Foldout(_showDebugSettings, "Debug", true, EditorStyles.foldoutHeader);

            if (_showDebugSettings)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(_enableDebug,       new GUIContent("Console Logs", "Print input activity to the Unity console."));
                EditorGUILayout.PropertyField(_enableVisualDebug, new GUIContent("Screen Overlay", "Show a real-time input overlay during Play Mode."));

                if (_enableVisualDebug.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_debugHandleSize,  new GUIContent("Overlay Scale", "Scale of the on-screen overlay elements (1–300)."));
                    EditorGUILayout.PropertyField(_debugHandleColor, new GUIContent("Overlay Color", "Color of the on-screen overlay."));
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionsInfo()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showActionsInfo = EditorGUILayout.Foldout(_showActionsInfo, "Action Map Info", true, EditorStyles.foldoutHeader);

            if (_showActionsInfo)
            {
                EditorGUILayout.Space(2);
                var asset = _inputActionAsset.objectReferenceValue as InputActionAsset;

                if (asset == null)
                {
                    EditorGUILayout.HelpBox("No Input Action Asset assigned.", MessageType.Info);
                }
                else
                {
                    int totalActions = 0;
                    foreach (var map in asset.actionMaps)
                    {
                        totalActions += map.actions.Count;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(map.name, EditorStyles.boldLabel, GUILayout.Width(140));
                        EditorGUILayout.LabelField($"{map.actions.Count} action(s)", _labelStyle);
                        EditorGUILayout.EndHorizontal();

                        // Show individual actions only when there are few enough to read at a glance
                        if (map.actions.Count <= 8)
                        {
                            EditorGUI.indentLevel++;
                            foreach (var action in map.actions)
                                EditorGUILayout.LabelField($"{action.name}  ({action.expectedControlType ?? "Any"})", _labelStyle);
                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.LabelField($"Total: {asset.actionMaps.Count} maps, {totalActions} actions", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawValidation()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            var reader = target as PlugInputReader;
            if (reader == null) { EditorGUILayout.EndVertical(); return; }

            if (reader.IsValid())
                EditorGUILayout.HelpBox(reader.GetDebugInfo(), MessageType.Info);
            else
                EditorGUILayout.HelpBox("Assign a valid Input Action Asset with at least one action map.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Asset") && reader.InputActionAsset != null)
                EditorGUIUtility.PingObject(reader.InputActionAsset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateInputActionAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Input Action Asset", "InputActions", "inputactions", "Choose where to save the asset.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _inputActionAsset.objectReferenceValue = asset;
            serializedObject.ApplyModifiedProperties();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}