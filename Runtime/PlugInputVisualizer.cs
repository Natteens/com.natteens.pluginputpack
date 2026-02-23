using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
    /// <summary>
    /// Runtime overlay displaying active input states on screen.
    ///
    /// Performance notes:
    ///   • DrawHandles no longer allocates a new HashSet every frame.
    ///     Instead two pre-allocated HashSets are swapped each frame (double-buffer).
    ///   • All Color values used in DrawHandles are pre-computed static readonly fields —
    ///     no "new Color(...)" per draw call.
    ///   • FormatValue uses a reused StringBuilder + a string cache, so it only
    ///     allocates a new string when the value actually changes (not every frame).
    ///   • List iteration (GetStates) uses index-based loop — struct enumerator, no alloc.
    /// </summary>
    public class PlugInputVisualizer
    {
        private bool  _isEnabled;
        private float _uiScale;
        private int   _lastScreenWidth;
        private int   _lastScreenHeight;
        private Rect  _panelRect;

        // Layout constants (at 1× scale)
        private const float BasePanelWidth  = 300f;
        private const float BasePanelHeight = 320f;
        private const float BaseLineHeight  = 24f;
        private const float BasePadding     = 10f;
        private const float BaseArrowSize   = 16f;
        private const float BaseArrowAreaW  = 50f;
        private const float InactivityThreshold = 0.2f;

        // Accent / UI colors — computed once, never allocated per-frame
        private static readonly Color AccentColor     = new Color(0.20f, 0.75f, 1.00f, 1.00f);
        private static readonly Color AccentDim       = new Color(0.20f, 0.75f, 1.00f, 0.28f);
        private static readonly Color AccentLine      = new Color(0.20f, 0.75f, 1.00f, 0.70f);
        private static readonly Color AccentHighlight = new Color(0.20f, 0.75f, 1.00f, 0.12f);
        private static readonly Color RowAlt          = new Color(1.00f, 1.00f, 1.00f, 0.04f);
        private static readonly Color SeparatorColor  = new Color(1.00f, 1.00f, 1.00f, 0.18f);
        private static readonly Color CircleBg        = new Color(0.15f, 0.15f, 0.15f, 0.50f);

        // Double-buffered HashSets — swapped each frame; no per-frame allocation
        private HashSet<string> _currentFrameActive = new HashSet<string>();
        private HashSet<string> _prevFrameActive     = new HashSet<string>();

        // Value string cache — only allocates when value changes
        private readonly Dictionary<string, string>  _valueCache       = new Dictionary<string, string>();
        private readonly Dictionary<string, float>   _cachedFloatVal   = new Dictionary<string, float>();   // last float rendered
        private readonly Dictionary<string, Vector2> _cachedVec2Val    = new Dictionary<string, Vector2>(); // last vec2 rendered
        private readonly List<string>                _activeInputs     = new List<string>();
        private readonly Dictionary<string, float>   _inactivityTimers = new Dictionary<string, float>();

        // StringBuilder reused for FormatValue — avoids string alloc on stable values
        private readonly StringBuilder _formatSb = new StringBuilder(32);

        // Textures and styles (created once on Initialize)
        private Texture2D _panelTexture;
        private Texture2D _circleTexture;
        private GUIStyle  _headerStyle;
        private GUIStyle  _labelStyle;
        private GUIStyle  _valueStyle;
        private GUIStyle  _emptyStyle;
        private GUIStyle  _panelStyle;

        // ── Initialization ────────────────────────────────────────────────────────

        public void Initialize(bool enabled, float ignoredHandleSize, Color ignoredColor)
        {
            _isEnabled = enabled;
            if (!enabled) return;

            RefreshScale();
            CreateTextures();
            InitStyles();

            _activeInputs.Clear();
            _currentFrameActive.Clear();
            _prevFrameActive.Clear();
            _valueCache.Clear();
            _inactivityTimers.Clear();
        }

        // ── Scale ─────────────────────────────────────────────────────────────────

        private void RefreshScale()
        {
            _lastScreenWidth  = Screen.width;
            _lastScreenHeight = Screen.height;
            _uiScale = 1.0f; // fixed; base panel size is comfortable at all resolutions
            _panelRect = new Rect(10f, Screen.height - BasePanelHeight - 10f, BasePanelWidth, BasePanelHeight);
        }

        // ── DrawHandles ───────────────────────────────────────────────────────────

        public void DrawHandles(PlugInputCache cache)
        {
            if (!_isEnabled) return;

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                RefreshScale();
                InitStyles();
            }

            float s        = _uiScale;
            float lh       = BaseLineHeight * s;
            float pad      = BasePadding    * s;
            float arrowSize = BaseArrowSize  * s;
            float arrowW   = BaseArrowAreaW  * s;

            _panelRect.y      = Screen.height - _panelRect.height - 10f;
            _panelRect.width  = BasePanelWidth  * s;
            _panelRect.height = BasePanelHeight * s;

            GUI.Box(_panelRect, "", _panelStyle);

            // Header
            Rect headerRect = new Rect(_panelRect.x + pad, _panelRect.y + pad, _panelRect.width - pad * 2f, 20f * s);
            GUI.Label(headerRect, "Active Inputs", _headerStyle);

            // Separator
            Rect lineRect = new Rect(_panelRect.x + pad * 1.5f, headerRect.yMax + 4f * s, _panelRect.width - pad * 3f, Mathf.Max(1f, s));
            GUI.color = SeparatorColor;
            GUI.DrawTexture(lineRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Content area
            float contentY = lineRect.yMax + 4f * s;
            float contentH = _panelRect.yMax - contentY - pad;
            Rect  content  = new Rect(_panelRect.x + pad, contentY, _panelRect.width - pad * 2f, contentH);

            _activeInputs.Clear();
            // Swap double-buffer: _currentFrameActive becomes fresh, previous frame stays in _prevFrameActive
            HashSet<string> tmp = _currentFrameActive;
            _currentFrameActive = _prevFrameActive;
            _currentFrameActive.Clear();
            _prevFrameActive = tmp; // now holds last frame's data for press-flash comparison

            float y        = content.y;
            int   index    = 0;
            int   maxItems = Mathf.FloorToInt(contentH / lh);

            List<InputState> states = cache.GetStates();
            int stateCount = states.Count;

            for (int i = 0; i < stateCount; i++)
            {
                InputState state = states[i];
                TickInactivity(state);

                if (!ShouldDisplay(state)) continue;
                if (index >= maxItems)     break;

                string name = state.Name;
                _currentFrameActive.Add(name);
                _activeInputs.Add(name);

                // Alternate row tint
                if (index % 2 == 1)
                {
                    GUI.color = RowAlt;
                    GUI.DrawTexture(new Rect(content.x, y, content.width, lh), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                // Press flash (new press this frame = not in previous frame's set)
                if (state.IsPressed && !_prevFrameActive.Contains(name))
                {
                    GUI.color = AccentHighlight;
                    GUI.DrawTexture(new Rect(content.x - 4f * s, y - 2f * s, content.width + 8f * s, lh + 4f * s), _panelTexture);
                    GUI.color = Color.white;
                }

                // Name
                Rect nameRect = new Rect(content.x + 4f * s, y, content.width * 0.38f, lh);
                GUI.Label(nameRect, name, _labelStyle);

                // Value
                Rect valRect = new Rect(nameRect.xMax + 4f * s, y, content.width * 0.34f, lh);
                GUI.Label(valRect, GetCachedValue(state), _valueStyle);

                // Direction dot for Vector2
                if (state.InputType == "Vector2")
                {
                    Vector2 dir = state.AsVector2;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Rect arrowRect = new Rect(
                            content.xMax - arrowW + 4f * s,
                            y + lh * 0.5f - arrowSize,
                            arrowSize * 2f,
                            arrowSize * 2f
                        );
                        DrawDirectionDot(arrowRect, dir);
                    }
                }

                y += lh;
                index++;
            }

            if (_activeInputs.Count == 0)
                GUI.Label(content, "No active inputs", _emptyStyle);
        }

        // ── Value formatting ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns a formatted string for the state's current value.
        /// Uses a cache keyed by action name — only rebuilds (allocates) when value changes.
        /// Uses StringBuilder to build Vector2/float strings, then caches the result.
        /// </summary>
        private string GetCachedValue(InputState state)
        {
            switch (state.InputType)
            {
                case "Button":
                case "Digital":
                {
                    // Bool: only two possible strings — use literals, never allocate
                    bool pressed = state.IsPressed;
                    if (_valueCache.TryGetValue(state.Name, out string prev))
                    {
                        bool wasOn = prev == "ON";
                        if (wasOn == pressed) return prev;
                    }
                    string r = pressed ? "ON" : "OFF";
                    _valueCache[state.Name] = r;
                    return r;
                }

                case "Vector2":
                {
                    Vector2 v = state.AsVector2;

                    // Only rebuild string if value changed from last render
                    if (_cachedVec2Val.TryGetValue(state.Name, out Vector2 lastV2) &&
                        (v - lastV2).sqrMagnitude < 0.005f * 0.005f &&
                        _valueCache.TryGetValue(state.Name, out string prevV2))
                        return prevV2;

                    string built;
                    if (v.sqrMagnitude < 0.0001f)
                    {
                        built = "(0.0, 0.0)";
                    }
                    else
                    {
                        _formatSb.Clear();
                        _formatSb.Append('(');
                        AppendF1(_formatSb, v.x);
                        _formatSb.Append(", ");
                        AppendF1(_formatSb, v.y);
                        _formatSb.Append(')');
                        built = _formatSb.ToString(); // alloc only when value changed
                    }
                    _valueCache[state.Name]    = built;
                    _cachedVec2Val[state.Name] = v;
                    return built;
                }

                case "Axis":
                case "Analog":
                {
                    float f = state.AsFloat;

                    // Only rebuild string if float changed beyond display resolution (0.01)
                    if (_cachedFloatVal.TryGetValue(state.Name, out float lastF) &&
                        Mathf.Abs(f - lastF) < 0.005f &&
                        _valueCache.TryGetValue(state.Name, out string prevF))
                        return prevF;

                    _formatSb.Clear();
                    AppendF2(_formatSb, f);
                    string builtF = _formatSb.ToString(); // alloc only when value changed
                    _valueCache[state.Name]     = builtF;
                    _cachedFloatVal[state.Name] = f;
                    return builtF;
                }

                default:
                    return state.GetDebugString();
            }
        }

        // Fast fixed-precision formatters — avoid string.Format / interpolation allocs
        private static void AppendF1(StringBuilder sb, float v)
        {
            if (v < 0) { sb.Append('-'); v = -v; }
            int whole = (int)v;
            int frac  = (int)((v - whole) * 10f + 0.5f);
            if (frac == 10) { whole++; frac = 0; }
            sb.Append(whole); sb.Append('.'); sb.Append(frac);
        }

        private static void AppendF2(StringBuilder sb, float v)
        {
            if (v < 0) { sb.Append('-'); v = -v; }
            int whole = (int)v;
            int frac  = (int)((v - whole) * 100f + 0.5f);
            if (frac == 100) { whole++; frac = 0; }
            sb.Append(whole); sb.Append('.');
            if (frac < 10) sb.Append('0');
            sb.Append(frac);
        }

        // ── Inactivity tracking ───────────────────────────────────────────────────

        private void TickInactivity(InputState state)
        {
            bool inactive = state.InputType == "Vector2"
                ? state.AsVector2.sqrMagnitude < 0.0001f
                : !state.IsPressed;

            _inactivityTimers[state.Name] = inactive
                ? (_inactivityTimers.TryGetValue(state.Name, out float t) ? t + Time.deltaTime : 0f)
                : 0f;
        }

        private bool ShouldDisplay(InputState state)
        {
            bool active = state.InputType == "Vector2"
                ? state.AsVector2.sqrMagnitude >= 0.0001f
                : state.IsPressed;

            return active
                || !_inactivityTimers.TryGetValue(state.Name, out float t)
                || t < InactivityThreshold;
        }

        // ── Direction dot ─────────────────────────────────────────────────────────

        private void DrawDirectionDot(Rect rect, Vector2 direction)
        {
            Vector2 nd = direction.normalized;
            nd.y = -nd.y;
            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);

            GUI.color = CircleBg;
            GUI.DrawTexture(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), _circleTexture);
            GUI.color = Color.white;

            DrawWireRing(center, rect.width * 0.45f, AccentDim);

            float   mag    = Mathf.Clamp01(direction.magnitude);
            Vector2 dotPos = center + nd * (rect.width * 0.45f * mag);
            float   dotR   = rect.width * 0.18f * _uiScale;

            Miniline(center, dotPos, AccentLine, Mathf.Max(1f, 2f * _uiScale));

            GUI.color = AccentColor;
            GUI.DrawTexture(
                new Rect(dotPos.x - dotR, dotPos.y - dotR, dotR * 2f, dotR * 2f),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, AccentColor, 0, dotR
            );
            GUI.color = Color.white;
        }

        private static void DrawWireRing(Vector2 center, float radius, Color c)
        {
            const int segs = 20;
            const float step = 2f * Mathf.PI / segs;
            for (int i = 0; i < segs; i++)
            {
                float a1 = i * step, a2 = (i + 1) * step;
                Miniline(
                    center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius,
                    center + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * radius,
                    c, 1f
                );
            }
        }

        private static void Miniline(Vector2 a, Vector2 b, Color c, float thickness)
        {
            Color prev = GUI.color;
            GUI.color = c;
            Vector2 delta = b - a;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, a);
            GUI.color = prev;
        }

        // ── Texture and style creation ─────────────────────────────────────────────

        private void CreateTextures()
        {
            _panelTexture  = CreateRoundedRect(32, 32, 8, new Color(0.11f, 0.11f, 0.13f, 0.93f));
            _circleTexture = CreateCircle(16, Color.white);
        }

        private void InitStyles()
        {
            int headerPx = Mathf.RoundToInt(13f * _uiScale);
            int normalPx = Mathf.RoundToInt(11f * _uiScale);

            _headerStyle = new GUIStyle
            {
                fontSize  = headerPx,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                margin    = new RectOffset(3, 3, 3, 3),
                normal    = { textColor = new Color(0.92f, 0.92f, 0.92f) }
            };
            _labelStyle = new GUIStyle
            {
                fontSize  = normalPx,
                alignment = TextAnchor.MiddleLeft,
                margin    = new RectOffset(3, 3, 2, 2),
                normal    = { textColor = new Color(0.78f, 0.78f, 0.78f) }
            };
            _valueStyle = new GUIStyle
            {
                fontSize  = normalPx,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                margin    = new RectOffset(3, 3, 2, 2),
                normal    = { textColor = AccentColor }
            };
            _emptyStyle = new GUIStyle
            {
                fontSize  = normalPx,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.6f, 0.6f, 0.6f, 0.7f) }
            };
            _panelStyle = new GUIStyle
            {
                normal  = { background = _panelTexture },
                border  = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(
                    Mathf.RoundToInt(9f * _uiScale),
                    Mathf.RoundToInt(9f * _uiScale),
                    Mathf.RoundToInt(9f * _uiScale),
                    Mathf.RoundToInt(9f * _uiScale)
                )
            };
        }

        private static Texture2D CreateRoundedRect(int w, int h, int r, Color c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px  = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            tex.SetPixels(px);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool tl = x < r     && y < r;
                bool tr = x >= w-r  && y < r;
                bool bl = x < r     && y >= h-r;
                bool br = x >= w-r  && y >= h-r;
                float dx, dy;
                if (tl) { dx = x - r;     dy = y - r;     if (dx*dx+dy*dy > r*r) continue; }
                if (tr) { dx = x-(w-r);   dy = y - r;     if (dx*dx+dy*dy > r*r) continue; }
                if (bl) { dx = x - r;     dy = y-(h-r);   if (dx*dx+dy*dy > r*r) continue; }
                if (br) { dx = x-(w-r);   dy = y-(h-r);   if (dx*dx+dy*dy > r*r) continue; }
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCircle(int size, Color c)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = r - x, dy = r - y;
                float d2 = dx*dx + dy*dy;
                tex.SetPixel(x, y, d2 <= r*r
                    ? new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01((r - Mathf.Sqrt(d2)) / r))
                    : Color.clear);
            }
            tex.Apply();
            return tex;
        }

        // ── Disposal ──────────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_panelTexture  != null) Object.Destroy(_panelTexture);
            if (_circleTexture != null) Object.Destroy(_circleTexture);
            _valueCache.Clear();
            _cachedFloatVal.Clear();
            _cachedVec2Val.Clear();
            _activeInputs.Clear();
            _currentFrameActive.Clear();
            _prevFrameActive.Clear();
            _inactivityTimers.Clear();
        }
    }
}