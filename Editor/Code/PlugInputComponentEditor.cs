using UnityEngine;
using UnityEditor;

namespace PlugInputPack.Editor
{
    [CustomEditor(typeof(PlugInputComponent))]
    public class PlugInputComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputReaderProperty;
        private bool _showRuntimeInfo;
        private bool _showDeviceInfo;
        private bool _showEvents;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;

        private static readonly string[] InputEventDescriptions =
        {
            "OnInputPerformed(string name, InputValue value)  —  any input performed",
            "OnInputCanceled(string name)  —  any input canceled",
            "OnInputPressed(string name)  —  pressed this frame",
            "OnInputReleased(string name)  —  released this frame",
            "OnInputValueChanged(string name, float value)  —  float value changed",
            "OnInputVector2Changed(string name, Vector2 value)  —  Vector2 value changed",
            "OnInputStateChanged(string name, bool value)  —  bool value changed",
            "OnInputSystemInitialized()  —  system ready",
            "OnInputSystemDestroyed()  —  system shut down",
        };

        private static readonly string[] DeviceEventDescriptions =
        {
            "OnDeviceChanged(DeviceType prev, DeviceType current)  —  player switched device",
            "OnDeviceConnected(InputDevice device)  —  new device connected",
            "OnDeviceDisconnected(InputDevice device)  —  device disconnected",
            "OnDeviceFiltered(DeviceType device)  —  input rejected (not in allowed list)",
        };

        private void OnEnable()
        {
            _inputReaderProperty = serializedObject.FindProperty("inputReader");
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal   = { textColor = new Color(0.3f, 0.8f, 1f) }
            };
            _boxStyle   = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 10, 10) };
            _labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Plug Input Component", _headerStyle);
            EditorGUILayout.Space(8);

            DrawInputReaderSection();
            EditorGUILayout.Space(6);

            if (Application.isPlaying)
            {
                DrawRuntimeSection();
                EditorGUILayout.Space(6);
                DrawDeviceSection();
                EditorGUILayout.Space(6);
            }
            else
            {
                DrawDesignTimeNote();
                EditorGUILayout.Space(6);
            }

            DrawEventsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInputReaderSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_inputReaderProperty, new GUIContent("Input Reader", "PlugInputReader ScriptableObject that holds all settings."));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            var reader = _inputReaderProperty.objectReferenceValue as PlugInputReader;

            if (reader == null)
            {
                EditorGUILayout.HelpBox("Assign a PlugInputReader to activate the input system.", MessageType.Warning);
                if (GUILayout.Button("Create PlugInputReader", GUILayout.Height(24)))
                    CreateInputReader();
            }
            else if (reader.InputActionAsset == null)
            {
                EditorGUILayout.HelpBox("The assigned PlugInputReader has no Input Action Asset.", MessageType.Warning);
                if (GUILayout.Button("Open Input Reader", GUILayout.Height(24)))
                    PingAndSelect(reader);
            }
            else
            {
                EditorGUILayout.HelpBox(reader.GetDebugInfo(), MessageType.Info);
                if (GUILayout.Button("Open Input Reader", GUILayout.Height(24)))
                    PingAndSelect(reader);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Info", true, EditorStyles.foldoutHeader);

            if (_showRuntimeInfo)
            {
                EditorGUILayout.Space(2);
                var component = target as PlugInputComponent;

                EditorGUILayout.LabelField("System", "Active", _labelStyle);

                var cacheField = typeof(PlugInputComponent).GetField("_cache",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (cacheField?.GetValue(component) is PlugInputCache cache)
                    EditorGUILayout.LabelField("Cache", cache.GetCacheStats(), _labelStyle);

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Repaint", GUILayout.Height(22)))
                    EditorUtility.SetDirty(target);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDeviceSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showDeviceInfo = EditorGUILayout.Foldout(_showDeviceInfo, "Active Device", true, EditorStyles.foldoutHeader);

            if (_showDeviceInfo)
            {
                EditorGUILayout.Space(2);
                var component = target as PlugInputComponent;

                EditorGUILayout.LabelField("Type", component.CurrentDeviceType.ToString(), _labelStyle);
                EditorGUILayout.LabelField("Name", component.CurrentDeviceName, _labelStyle);

                if (component.DeviceManager != null)
                {
                    EditorGUILayout.Space(4);
                    foreach (string line in component.DeviceManager.GetDebugInfo().Split('\n'))
                        if (!string.IsNullOrWhiteSpace(line))
                            EditorGUILayout.LabelField(line, _labelStyle);
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Keyboard")) component.ForceDeviceType(PlugInputDeviceManager.DeviceType.Keyboard);
                if (GUILayout.Button("Force Mouse"))    component.ForceDeviceType(PlugInputDeviceManager.DeviceType.Mouse);
                if (GUILayout.Button("Force Gamepad"))  component.ForceDeviceType(PlugInputDeviceManager.DeviceType.Gamepad);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDesignTimeNote()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Enter Play Mode to see runtime info and device details.", _labelStyle);
            EditorGUILayout.EndVertical();
        }

        private void DrawEventsSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            _showEvents = EditorGUILayout.Foldout(_showEvents, "Available Events", true, EditorStyles.foldoutHeader);

            if (_showEvents)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Input Events", EditorStyles.boldLabel);
                foreach (string e in InputEventDescriptions)
                    EditorGUILayout.LabelField(e, _labelStyle);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Device Events", EditorStyles.boldLabel);
                foreach (string e in DeviceEventDescriptions)
                    EditorGUILayout.LabelField(e, _labelStyle);
            }

            EditorGUILayout.EndVertical();
        }

        // -------------------------------------------------------------------------

        private static void PingAndSelect(Object obj)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private void CreateInputReader()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create PlugInputReader", "InputReader", "asset", "Choose where to save the asset.");
            if (string.IsNullOrEmpty(path)) return;

            var reader = CreateInstance<PlugInputReader>();
            AssetDatabase.CreateAsset(reader, path);
            AssetDatabase.SaveAssets();

            _inputReaderProperty.objectReferenceValue = reader;
            serializedObject.ApplyModifiedProperties();
            PingAndSelect(reader);
        }
    }
}