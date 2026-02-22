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
        private SerializedProperty _enableDeviceManagement;
        private SerializedProperty _strictDeviceIsolation;
        private SerializedProperty _deviceSwitchCooldown;
        private SerializedProperty _allowedDevices;
        private SerializedProperty _lockCursorOnStart;
        private SerializedProperty _autoLockCursorOnGamepad;

        private bool _showDevice  = true;
        private bool _showCursor  = true;
        private bool _showDebug   = true;
        private bool _showActions;

        // Colors resolved at draw time — respect Pro vs Personal skin
        private static Color BgSection  => EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.24f) : new Color(0.86f, 0.86f, 0.87f);
        private static Color Accent     => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.52f, 0.78f) : new Color(0.16f, 0.40f, 0.70f);
        private static Color AccentDim  => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.52f, 0.78f, 0.38f) : new Color(0.16f, 0.40f, 0.70f, 0.35f);
        private static Color OkGreen    => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.65f, 0.40f) : new Color(0.12f, 0.48f, 0.25f);
        private static Color WarnYellow => EditorGUIUtility.isProSkin ? new Color(0.82f, 0.68f, 0.22f) : new Color(0.60f, 0.45f, 0.05f);
        private static Color Divider    => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.14f);

        private GUIStyle _sectionStyle;
        private GUIStyle _foldoutStyle;
        private GUIStyle _boldStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _miniStyle;
        private GUIStyle _btnPrimary;
        private GUIStyle _btnSecondary;
        private Texture2D _bgTex;
        private bool _stylesBuilt;
        private bool _lastSkin;

        private void OnEnable()
        {
            _inputActionAsset       = serializedObject.FindProperty("inputActionAsset");
            _enableDebug            = serializedObject.FindProperty("enableDebug");
            _enableVisualDebug      = serializedObject.FindProperty("enableVisualDebug");
            _enableDeviceManagement = serializedObject.FindProperty("enableDeviceManagement");
            _strictDeviceIsolation  = serializedObject.FindProperty("strictDeviceIsolation");
            _deviceSwitchCooldown   = serializedObject.FindProperty("deviceSwitchCooldown");
            _allowedDevices         = serializedObject.FindProperty("allowedDevices");
            _lockCursorOnStart      = serializedObject.FindProperty("lockCursorOnStart");
            _autoLockCursorOnGamepad = serializedObject.FindProperty("autoLockCursorOnGamepad");
        }

        private void OnDisable() { if (_bgTex) { Object.DestroyImmediate(_bgTex); _bgTex = null; } _stylesBuilt = false; }

        private void EnsureStyles()
        {
            bool pro = EditorGUIUtility.isProSkin;
            if (_stylesBuilt && _lastSkin == pro && _bgTex != null) return;
            _lastSkin = pro;

            if (_bgTex) Object.DestroyImmediate(_bgTex);
            _bgTex = Solid(BgSection);

            _sectionStyle = new GUIStyle
            {
                normal  = { background = _bgTex },
                padding = new RectOffset(10, 10, 7, 7),
                margin  = new RectOffset(0, 0, 0, 2)
            };
            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 11
            };
            _boldStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            _labelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11 };
            _miniStyle  = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            _btnPrimary = new GUIStyle(GUI.skin.button)
            {
                fontSize    = 11,
                fontStyle   = FontStyle.Bold,
                fixedHeight = 23,
                normal  = { textColor = Color.white, background = Solid(Accent) },
                hover   = { textColor = Color.white, background = Solid(Accent + new Color(0.07f, 0.07f, 0.07f)) },
                active  = { textColor = Color.white, background = Solid(Accent - new Color(0.05f, 0.05f, 0.05f)) }
            };
            _btnSecondary = new GUIStyle(EditorStyles.miniButton) { fontSize = 11, fixedHeight = 23 };

            _stylesBuilt = true;
        }

        // ── Main ────────────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            DrawTitleBar();
            GUILayout.Space(4);
            DrawAssetBlock();
            GUILayout.Space(2);
            DrawFoldout("Device Management", ref _showDevice, DrawDeviceContent);
            GUILayout.Space(2);
            DrawFoldout("Cursor",            ref _showCursor, DrawCursorContent);
            GUILayout.Space(2);
            DrawFoldout("Debug",             ref _showDebug,  DrawDebugContent);
            GUILayout.Space(2);
            DrawFoldout("Action Map Info",   ref _showActions, DrawActionsContent);
            GUILayout.Space(2);
            DrawStatusBlock();

            serializedObject.ApplyModifiedProperties();
        }

        // ── Title ───────────────────────────────────────────────────────────────

        private void DrawTitleBar()
        {
            Rect lr = GUILayoutUtility.GetRect(1f, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lr, Accent);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Plug Input Reader", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
        }

        // ── Foldout section (no GetLastRect issues) ──────────────────────────────

        private void DrawFoldout(string title, ref bool open, System.Action content)
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            open = EditorGUILayout.Foldout(open, title, true, _foldoutStyle);
            if (open)
            {
                HairLine();
                GUILayout.Space(3);
                content();
                GUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();
        }

        // ── Always-visible blocks ────────────────────────────────────────────────

        private void DrawAssetBlock()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("Input Action Asset", _boldStyle);
            HairLine();
            GUILayout.Space(3);

            var asset = _inputActionAsset.objectReferenceValue as InputActionAsset;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_inputActionAsset,
                new GUIContent("Asset", "The Unity Input System asset that defines all input actions."));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create New", _btnPrimary)) CreateAsset();
            GUI.enabled = asset != null;
            if (GUILayout.Button("Open", _btnSecondary)) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (asset == null)
            {
                GUILayout.Space(4);
                if (GUILayout.Button("Auto-find in Project", _btnSecondary)) AutoFind();
                GUILayout.Space(2);
                EditorGUILayout.LabelField("No Input Action Asset assigned.", _miniStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusBlock()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("Status", _boldStyle);
            HairLine();
            GUILayout.Space(3);

            var reader = target as PlugInputReader;
            if (reader == null) { EditorGUILayout.EndVertical(); return; }

            if (reader.IsValid())
                InfoBar(reader.GetDebugInfo(), OkGreen);
            else
                InfoBar("Assign a valid Input Action Asset with at least one action map.", WarnYellow);

            GUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = reader.InputActionAsset != null;
            if (GUILayout.Button("Ping Asset", _btnSecondary, GUILayout.Width(80)))
                EditorGUIUtility.PingObject(reader.InputActionAsset);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── Section content ──────────────────────────────────────────────────────

        private void DrawDeviceContent()
        {
            EditorGUILayout.PropertyField(_enableDeviceManagement,
                new GUIContent("Enable", "Detect and track which input device the player is using."));

            if (_enableDeviceManagement.boolValue)
            {
                GUILayout.Space(2);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_strictDeviceIsolation,
                    new GUIContent("Strict Isolation", "Only process inputs from the active device."));
                EditorGUILayout.PropertyField(_deviceSwitchCooldown,
                    new GUIContent("Switch Cooldown", "Minimum seconds before the active device can change again."));
                EditorGUILayout.PropertyField(_allowedDevices,
                    new GUIContent("Allowed Devices", "Whitelist of device types. Leave empty to allow all."));
                EditorGUI.indentLevel--;
                if (_strictDeviceIsolation.boolValue) { GUILayout.Space(3); InfoBar("Strict Isolation on — only the active device's inputs will be processed.", AccentDim); }
            }
            else { GUILayout.Space(3); InfoBar("Device management off — all inputs processed regardless of source.", Divider * 4f); }
        }

        private void DrawCursorContent()
        {
            EditorGUILayout.PropertyField(_lockCursorOnStart,    new GUIContent("Lock On Start",     "Lock and hide the cursor when the scene starts."));
            EditorGUILayout.PropertyField(_autoLockCursorOnGamepad, new GUIContent("Auto Lock On Gamepad", "Lock cursor when gamepad/joystick/XR is active; unlock on keyboard or mouse."));
        }

        private void DrawDebugContent()
        {
            EditorGUILayout.PropertyField(_enableDebug,       new GUIContent("Console Logs",  "Print input activity to the Unity console."));
            EditorGUILayout.PropertyField(_enableVisualDebug, new GUIContent("Screen Overlay","Show a real-time input overlay during Play Mode."));
            if (_enableVisualDebug.boolValue) { GUILayout.Space(3); InfoBar("Overlay scale is computed automatically from screen size.", AccentDim); }
        }

        private void DrawActionsContent()
        {
            var asset = _inputActionAsset.objectReferenceValue as InputActionAsset;
            if (asset == null) { EditorGUILayout.LabelField("No asset assigned.", _miniStyle); return; }

            int total = 0;
            foreach (var map in asset.actionMaps)
            {
                total += map.actions.Count;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(map.name, EditorStyles.boldLabel, GUILayout.MinWidth(80));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{map.actions.Count} action{(map.actions.Count != 1 ? "s" : "")}", _miniStyle, GUILayout.Width(65));
                EditorGUILayout.EndHorizontal();

                if (map.actions.Count <= 10)
                {
                    EditorGUI.indentLevel++;
                    foreach (var a in map.actions)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(a.name, _labelStyle);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField(a.expectedControlType ?? "Any", _miniStyle, GUILayout.Width(65));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                GUILayout.Space(3);
            }

            HairLine();
            GUILayout.Space(3);
            EditorGUILayout.LabelField($"{asset.actionMaps.Count} maps  ·  {total} actions total", _miniStyle);
        }

        // ── UI primitives ────────────────────────────────────────────────────────

        private void HairLine()
        {
            Rect r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, Divider);
        }

        private void InfoBar(string msg, Color barColor)
        {
            EditorGUILayout.BeginHorizontal();
            Rect bar = GUILayoutUtility.GetRect(3f, EditorGUIUtility.singleLineHeight + 2f, GUILayout.Width(3));
            EditorGUI.DrawRect(bar, barColor);
            GUILayout.Space(5);
            EditorGUILayout.LabelField(msg, _miniStyle);
            EditorGUILayout.EndHorizontal();
        }

        // ── Asset actions ─────────────────────────────────────────────────────────

        private void AutoFind()
        {
            string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length == 0) { EditorUtility.DisplayDialog("Auto-find", "No InputActionAsset found in the project.", "OK"); return; }
            if (guids.Length == 1)
            {
                _inputActionAsset.objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                var menu = new GenericMenu();
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    menu.AddItem(new GUIContent(p.Replace("Assets/", "")), false, () =>
                    {
                        _inputActionAsset.objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(p);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Input Action Asset", "InputActions", "inputactions", "");
            if (string.IsNullOrEmpty(path)) return;
            var a = ScriptableObject.CreateInstance<InputActionAsset>();
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
            _inputActionAsset.objectReferenceValue = a;
            serializedObject.ApplyModifiedProperties();
            Selection.activeObject = a; EditorGUIUtility.PingObject(a);
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply(); return t;
        }
    }
}