using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
    /// <summary>
    /// Runtime overlay that displays active input states on screen.
    /// Panel size is computed automatically from the available screen area — no manual scale needed.
    /// </summary>
    public class PlugInputVisualizer
    {
        private bool _isEnabled;

        // Auto-computed from screen; recalculated when screen size changes
        private float _uiScale;
        private int   _lastScreenWidth;
        private int   _lastScreenHeight;

        private Rect  _panelRect;

        // Base layout constants (at 1x scale)
        private const float BasePanelWidth  = 300f;
        private const float BasePanelHeight = 320f;
        private const float BaseLineHeight  = 24f;
        private const float BasePadding     = 10f;
        private const float BaseArrowSize   = 16f;
        private const float BaseArrowAreaW  = 50f;

        // Accent color is internal — not exposed as a configurable field
        private static readonly Color AccentColor = new Color(0.2f, 0.75f, 1f, 1f);

        private readonly Dictionary<string, string> _valueCache     = new();
        private readonly List<string>               _activeInputs   = new();
        private readonly HashSet<string>            _lastFrameInputs = new();
        private readonly Dictionary<string, float>  _inactivityTimers = new();
        private const float InactivityThreshold = 0.2f;

        private Texture2D _panelTexture;
        private Texture2D _solidTexture;
        private Texture2D _circleTexture;

        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _emptyStyle;
        private GUIStyle _panelStyle;

        // -------------------------------------------------------------------------

        public void Initialize(bool enabled, float ignoredHandleSize, Color ignoredColor)
        {
            _isEnabled = enabled;
            if (!enabled) return;

            RefreshScale();
            CreateTextures();
            InitStyles();

            _activeInputs.Clear();
            _lastFrameInputs.Clear();
            _valueCache.Clear();
            _inactivityTimers.Clear();
        }

        // -------------------------------------------------------------------------
        // Scale helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Computes a UI scale that keeps the panel comfortably visible at any resolution.
        /// Target: panel occupies roughly 22% of screen width, clamped to a sensible range.
        /// </summary>
        private void RefreshScale()
        {
            _lastScreenWidth  = Screen.width;
            _lastScreenHeight = Screen.height;

            // Fixed scale — the base panel (300x320) is already comfortable to read
            // at any resolution from a small docked Game view to a maximised monitor.
            // Scaling by resolution caused the panel to become enormous on large screens.
            _uiScale = 1.0f;

            float w = BasePanelWidth  * _uiScale;
            float h = BasePanelHeight * _uiScale;
            _panelRect = new Rect(10f, Screen.height - h - 10f, w, h);
        }

        // -------------------------------------------------------------------------
        // Drawing
        // -------------------------------------------------------------------------

        public void DrawHandles(PlugInputCache cache)
        {
            if (!_isEnabled) return;

            // Rebuild if screen was resized (e.g. Play Mode resize, docked/undocked)
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                RefreshScale();
                InitStyles(); // font sizes depend on scale
            }

            float s         = _uiScale;
            float lh        = BaseLineHeight  * s;
            float pad       = BasePadding     * s;
            float arrowSize = BaseArrowSize   * s;
            float arrowW    = BaseArrowAreaW  * s;

            // Anchor panel to bottom-left
            _panelRect.y      = Screen.height - _panelRect.height - 10f;
            _panelRect.width  = BasePanelWidth  * s;
            _panelRect.height = BasePanelHeight * s;

            GUI.Box(_panelRect, "", _panelStyle);

            // --- Header ---
            Rect headerRect = new Rect(
                _panelRect.x + pad,
                _panelRect.y + pad,
                _panelRect.width - pad * 2f,
                20f * s
            );
            GUI.Label(headerRect, "Active Inputs", _headerStyle);

            // Separator line
            Rect lineRect = new Rect(
                _panelRect.x + pad * 1.5f,
                headerRect.yMax + 4f * s,
                _panelRect.width - pad * 3f,
                Mathf.Max(1f, s)
            );
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(lineRect, _solidTexture);
            GUI.color = Color.white;

            // --- Content area ---
            float contentY = lineRect.yMax + 4f * s;
            float contentH = _panelRect.yMax - contentY - pad;
            Rect  content  = new Rect(_panelRect.x + pad, contentY, _panelRect.width - pad * 2f, contentH);

            _activeInputs.Clear();
            var currentActive = new HashSet<string>();

            float y        = content.y;
            int   index    = 0;
            int   maxItems = Mathf.FloorToInt(contentH / lh);

            foreach (var state in cache.GetStates())
            {
                TickInactivity(state);

                if (!ShouldDisplay(state)) continue;
                if (index >= maxItems)     break;

                currentActive.Add(state.Name);
                _activeInputs.Add(state.Name);

                // Alternate row tint
                if (index % 2 == 1)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.04f);
                    GUI.DrawTexture(new Rect(content.x, y, content.width, lh), _solidTexture);
                    GUI.color = Color.white;
                }

                // Press highlight
                if (state.IsPressed && !_lastFrameInputs.Contains(state.Name))
                {
                    GUI.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.12f);
                    GUI.DrawTexture(new Rect(content.x - 4f * s, y - 2f * s, content.width + 8f * s, lh + 4f * s), _panelTexture);
                    GUI.color = Color.white;
                }

                // Name label
                Rect nameRect = new Rect(content.x + 4f * s, y, content.width * 0.38f, lh);
                GUI.Label(nameRect, state.Name, _labelStyle);

                // Value label
                Rect valRect = new Rect(nameRect.xMax + 4f * s, y, content.width * 0.34f, lh);
                GUI.Label(valRect, FormatValue(state), _valueStyle);

                // Direction arrow for Vector2
                if (state.InputType == "Vector2")
                {
                    Vector2 dir = state.AsVector2;
                    if (dir.magnitude > 0.01f)
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

            // Update last-frame set
            _lastFrameInputs.Clear();
            foreach (string n in currentActive) _lastFrameInputs.Add(n);

            // Empty state message
            if (_activeInputs.Count == 0)
                GUI.Label(content, "No active inputs", _emptyStyle);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private void TickInactivity(InputState state)
        {
            bool inactive = state.InputType == "Vector2"
                ? state.AsVector2.magnitude < 0.01f
                : !state.IsPressed;

            if (inactive)
                _inactivityTimers[state.Name] = _inactivityTimers.TryGetValue(state.Name, out float t) ? t + Time.deltaTime : 0f;
            else
                _inactivityTimers[state.Name] = 0f;
        }

        private bool ShouldDisplay(InputState state)
        {
            bool active = state.InputType == "Vector2"
                ? state.AsVector2.magnitude >= 0.01f
                : state.IsPressed;

            return active
                || !_inactivityTimers.TryGetValue(state.Name, out float t)
                || t < InactivityThreshold;
        }

        private string FormatValue(InputState state)
        {
            if (_valueCache.TryGetValue(state.Name, out string cached)
                && state.InputType != "Vector2" && !state.IsPressed)
                return cached;

            string result;
            switch (state.InputType)
            {
                case "Vector2":
                    Vector2 v = state.AsVector2;
                    result = v.magnitude < 0.01f ? "(0.0, 0.0)" : $"({v.x:F1}, {v.y:F1})";
                    break;
                case "Button":
                case "Digital":
                    result = state.IsPressed ? "ON" : "OFF";
                    break;
                case "Axis":
                case "Analog":
                    result = state.AsFloat.ToString("F2");
                    break;
                default:
                    result = state.GetDebugString();
                    break;
            }

            _valueCache[state.Name] = result;
            return result;
        }

        private void DrawDirectionDot(Rect rect, Vector2 direction)
        {
            Vector2 nd = direction.normalized;
            nd.y = -nd.y;
            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);

            // Background circle
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            GUI.DrawTexture(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), _circleTexture);
            GUI.color = Color.white;

            // Wire ring
            DrawWireRing(center, rect.width * 0.45f, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.28f));

            // Moving dot
            float mag      = Mathf.Clamp01(direction.magnitude);
            Vector2 dotPos = center + nd * (rect.width * 0.45f * mag);
            float dotR     = rect.width * 0.18f * _uiScale;

            Miniline(center, dotPos, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.7f), Mathf.Max(1f, 2f * _uiScale));

            GUI.color = AccentColor;
            GUI.DrawTexture(new Rect(dotPos.x - dotR, dotPos.y - dotR, dotR * 2f, dotR * 2f), _solidTexture, ScaleMode.StretchToFill, true, 0, AccentColor, 0, dotR);
            GUI.color = Color.white;
        }

        private void DrawWireRing(Vector2 center, float radius, Color c)
        {
            const int segs = 20;
            float step = 360f / segs * Mathf.Deg2Rad;
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

        // -------------------------------------------------------------------------
        // Texture + style creation
        // -------------------------------------------------------------------------

        private void CreateTextures()
        {
            _panelTexture  = CreateRoundedRect(32, 32, 8, new Color(0.11f, 0.11f, 0.13f, 0.93f));
            _solidTexture  = Texture2D.whiteTexture;
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
                normal    = { textColor = new Color(0.92f, 0.92f, 0.92f, 1f) }
            };
            _labelStyle = new GUIStyle
            {
                fontSize  = normalPx,
                alignment = TextAnchor.MiddleLeft,
                margin    = new RectOffset(3, 3, 2, 2),
                normal    = { textColor = new Color(0.78f, 0.78f, 0.78f, 1f) }
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
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            tex.SetPixels(px);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inCornerTL = x < r && y < r;
                    bool inCornerTR = x >= w - r && y < r;
                    bool inCornerBL = x < r && y >= h - r;
                    bool inCornerBR = x >= w - r && y >= h - r;

                    if (inCornerTL && Vector2.Distance(new Vector2(x, y), new Vector2(r, r))         > r) continue;
                    if (inCornerTR && Vector2.Distance(new Vector2(x, y), new Vector2(w - r, r))     > r) continue;
                    if (inCornerBL && Vector2.Distance(new Vector2(x, y), new Vector2(r, h - r))     > r) continue;
                    if (inCornerBR && Vector2.Distance(new Vector2(x, y), new Vector2(w - r, h - r)) > r) continue;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCircle(int size, Color c)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r  = size * 0.5f;
            float r2 = r * r;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = r - x, dy = r - y;
                    float d2 = dx * dx + dy * dy;
                    tex.SetPixel(x, y, d2 <= r2
                        ? new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01((r - Mathf.Sqrt(d2)) / r))
                        : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        public void Dispose()
        {
            if (_panelTexture  != null) Object.Destroy(_panelTexture);
            if (_circleTexture != null) Object.Destroy(_circleTexture);
            _valueCache.Clear();
            _activeInputs.Clear();
            _lastFrameInputs.Clear();
            _inactivityTimers.Clear();
        }
    }
}