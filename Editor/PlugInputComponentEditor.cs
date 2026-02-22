using UnityEngine;
using UnityEditor;

namespace PlugInputPack.Editor
{
    [CustomEditor(typeof(PlugInputComponent))]
    public class PlugInputComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _inputReaderProperty;
        private bool _showRuntime;
        private bool _showDevice;
        private bool _showEvents;

        // Colors resolved at draw time — respect Pro vs Personal skin
        private static Color BgSection  => EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.24f) : new Color(0.86f, 0.86f, 0.87f);
        private static Color Accent     => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.52f, 0.78f) : new Color(0.16f, 0.40f, 0.70f);
        private static Color OkGreen    => EditorGUIUtility.isProSkin ? new Color(0.28f, 0.65f, 0.40f) : new Color(0.12f, 0.48f, 0.25f);
        private static Color WarnYellow => EditorGUIUtility.isProSkin ? new Color(0.82f, 0.68f, 0.22f) : new Color(0.60f, 0.45f, 0.05f);
        private static Color Divider    => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.14f);

        private GUIStyle _sectionStyle;
        private GUIStyle _foldoutStyle;
        private GUIStyle _boldStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _miniStyle;
        private GUIStyle _monoStyle;
        private GUIStyle _btnPrimary;
        private GUIStyle _btnSecondary;
        private Texture2D _bgTex;
        private bool _stylesBuilt;
        private bool _lastSkin;

        private static readonly (string sig, string desc)[] InputEvents =
        {
            ("OnInputPerformed(string, InputValue)",  "any input performed"),
            ("OnInputCanceled(string)",               "any input canceled"),
            ("OnInputPressed(string)",                "pressed this frame"),
            ("OnInputReleased(string)",               "released this frame"),
            ("OnInputValueChanged(string, float)",    "float value changed"),
            ("OnInputVector2Changed(string, Vector2)","Vector2 value changed"),
            ("OnInputStateChanged(string, bool)",     "bool value changed"),
            ("OnInputSystemInitialized()",            "system ready"),
            ("OnInputSystemDestroyed()",              "system shut down"),
        };

        private static readonly (string sig, string desc)[] DeviceEvents =
        {
            ("OnDeviceChanged(DeviceType, DeviceType)", "player switched device"),
            ("OnDeviceConnected(InputDevice)",          "new device connected"),
            ("OnDeviceDisconnected(InputDevice)",       "device disconnected"),
            ("OnDeviceFiltered(DeviceType)",            "rejected (not in allowed list)"),
        };

        private void OnEnable() => _inputReaderProperty = serializedObject.FindProperty("inputReader");
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
            _foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 11 };
            _boldStyle    = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            _labelStyle   = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 11 };
            _miniStyle    = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            _monoStyle    = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal   = { textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.75f, 0.95f) : new Color(0.10f, 0.30f, 0.60f) }
            };
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
            DrawConfigBlock();
            GUILayout.Space(2);

            if (Application.isPlaying)
            {
                DrawFoldout("Runtime Info",  ref _showRuntime, DrawRuntimeContent);
                GUILayout.Space(2);
                DrawFoldout("Active Device", ref _showDevice,  DrawDeviceContent);
                GUILayout.Space(2);
            }
            else
            {
                DrawDesignBanner();
                GUILayout.Space(2);
            }

            DrawFoldout("Available Events", ref _showEvents, DrawEventsContent);

            serializedObject.ApplyModifiedProperties();
        }

        // ── Title ───────────────────────────────────────────────────────────────

        private void DrawTitleBar()
        {
            Rect lr = GUILayoutUtility.GetRect(1f, 2f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lr, Accent);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Plug Input Component", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });
        }

        // ── Foldout helper (no GetLastRect after BeginVertical) ─────────────────

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

        private void DrawConfigBlock()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("Configuration", _boldStyle);
            HairLine();
            GUILayout.Space(3);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_inputReaderProperty,
                new GUIContent("Input Reader", "PlugInputReader ScriptableObject that holds all settings."));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            var reader = _inputReaderProperty.objectReferenceValue as PlugInputReader;
            GUILayout.Space(4);

            if (reader == null)
            {
                InfoBar("Assign a PlugInputReader to activate the input system.", WarnYellow);
                GUILayout.Space(4);
                if (GUILayout.Button("Create PlugInputReader", _btnPrimary)) CreateReader();
            }
            else if (reader.InputActionAsset == null)
            {
                InfoBar("The assigned PlugInputReader has no Input Action Asset.", WarnYellow);
                GUILayout.Space(4);
                if (GUILayout.Button("Open Input Reader", _btnSecondary)) Ping(reader);
            }
            else
            {
                InfoBar(reader.GetDebugInfo(), OkGreen);
                GUILayout.Space(4);
                if (GUILayout.Button("Open Input Reader", _btnSecondary)) Ping(reader);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDesignBanner()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("Enter Play Mode to inspect runtime state and device info.", _miniStyle);
            EditorGUILayout.EndVertical();
        }

        // ── Section content ──────────────────────────────────────────────────────

        private void DrawRuntimeContent()
        {
            var c = target as PlugInputComponent;
            KV("System", "Active");

            var cf = typeof(PlugInputComponent).GetField("_cache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cf?.GetValue(c) is PlugInputCache cache) KV("Cache", cache.GetCacheStats());

            GUILayout.Space(4);
            if (GUILayout.Button("Repaint", _btnSecondary, GUILayout.Width(74)))
                EditorUtility.SetDirty(target);
        }

        private void DrawDeviceContent()
        {
            var c = target as PlugInputComponent;
            KV("Type", c.CurrentDeviceType.ToString());
            KV("Name", c.CurrentDeviceName);

            if (c.DeviceManager != null)
            {
                GUILayout.Space(4);
                HairLine();
                GUILayout.Space(4);
                foreach (string line in c.DeviceManager.GetDebugInfo().Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int sep = line.IndexOf(':');
                    if (sep > 0) KV(line[..sep].Trim(), line[(sep + 1)..].Trim());
                    else EditorGUILayout.LabelField(line, _labelStyle);
                }
            }

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Keyboard", _btnSecondary)) c.ForceDeviceType(PlugInputDeviceManager.DeviceType.Keyboard);
            if (GUILayout.Button("Mouse",    _btnSecondary)) c.ForceDeviceType(PlugInputDeviceManager.DeviceType.Mouse);
            if (GUILayout.Button("Gamepad",  _btnSecondary)) c.ForceDeviceType(PlugInputDeviceManager.DeviceType.Gamepad);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventsContent()
        {
            EditorGUILayout.LabelField("Input Events", _boldStyle);
            GUILayout.Space(3);
            foreach (var (sig, desc) in InputEvents) EventRow(sig, desc);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Device Events", _boldStyle);
            GUILayout.Space(3);
            foreach (var (sig, desc) in DeviceEvents) EventRow(sig, desc);
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

        private void KV(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, _miniStyle, GUILayout.Width(85));
            EditorGUILayout.LabelField(value, _labelStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void EventRow(string sig, string desc)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(sig,  _monoStyle, GUILayout.MinWidth(160));
            EditorGUILayout.LabelField($"— {desc}", _miniStyle);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(1);
        }

        // ── Asset helpers ────────────────────────────────────────────────────────

        private static void Ping(Object o) { Selection.activeObject = o; EditorGUIUtility.PingObject(o); }

        private void CreateReader()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create PlugInputReader", "InputReader", "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            var r = CreateInstance<PlugInputReader>();
            AssetDatabase.CreateAsset(r, path); AssetDatabase.SaveAssets();
            _inputReaderProperty.objectReferenceValue = r;
            serializedObject.ApplyModifiedProperties();
            Ping(r);
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply(); return t;
        }
    }
}