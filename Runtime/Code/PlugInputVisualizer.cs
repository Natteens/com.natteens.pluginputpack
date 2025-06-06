using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace PlugInputPack
{
    /// <summary>
    /// Sistema de visualização minimalista para inputs.
    /// </summary>
    public class PlugInputVisualizer
    {
        private bool _isEnabled = false;
        private Color _handleColor = new Color(0, 1, 0, 0.85f);
        private Rect _debugPanelRect;
        private float _handleScale = 1.0f;
        
        private readonly Dictionary<string, string> _valueCache = new Dictionary<string, string>();
        private readonly List<string> _activeInputs = new List<string>();
        private readonly HashSet<string> _lastFrameInputs = new HashSet<string>();
        
        private Dictionary<string, float> _inactivityTimers = new Dictionary<string, float>();
        private const float INACTIVITY_THRESHOLD = 0.2f;
        
        private Texture2D _panelTexture;
        private Texture2D _lineTexture;
        private Texture2D _circleTexture;
        
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _centeredStyle;
        private GUIStyle _panelStyle;
        
        private float _panelWidth = 300f;  
        private float _panelHeight = 320f;
        private float _lineHeight = 24f;
        private float _padding = 10f;   
        private float _arrowSize = 16f;    
        private float _arrowAreaWidth = 50f; 
        
        /// <summary>
        /// Inicializa o visualizador
        /// </summary>
        public void Initialize(bool enabled, float handleSize, Color handleColor)
        {
            _isEnabled = enabled;
            _handleColor = handleColor;
            _handleScale = handleSize; 
            
            float scaledPanelWidth = _panelWidth * _handleScale;
            float scaledPanelHeight = _panelHeight * _handleScale;
            
            _debugPanelRect = new Rect(
                10, 
                Screen.height - scaledPanelHeight - 10, 
                scaledPanelWidth, 
                scaledPanelHeight
            );
            
            CreateTextures();
            
            InitializeStyles();
            
            _activeInputs.Clear();
            _lastFrameInputs.Clear();
            _valueCache.Clear();
            _inactivityTimers.Clear();
        }
        
        /// <summary>
        /// Cria as texturas necessárias
        /// </summary>
        private void CreateTextures()
        {
            _panelTexture = CreateRoundedRectTexture(32, 32, 8, new Color(0.12f, 0.12f, 0.14f, 0.92f));
            
            _lineTexture = new Texture2D(1, 1);
            _lineTexture.SetPixel(0, 0, Color.white);
            _lineTexture.Apply();
            
            _circleTexture = CreateCircleTexture(16, Color.white);
        }
        
        /// <summary>
        /// Inicializa os estilos para a GUI
        /// </summary>
        private void InitializeStyles()
        {
            int headerSize = Mathf.RoundToInt(14 * _handleScale);
            int normalSize = Mathf.RoundToInt(12 * _handleScale);
            
            _headerStyle = new GUIStyle();
            _headerStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
            _headerStyle.fontSize = headerSize;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.margin = new RectOffset(4, 4, 4, 4);
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            
            _labelStyle = new GUIStyle();
            _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
            _labelStyle.fontSize = normalSize;
            _labelStyle.margin = new RectOffset(4, 4, 2, 2);
            _labelStyle.alignment = TextAnchor.MiddleLeft;
            
            _valueStyle = new GUIStyle();
            _valueStyle.normal.textColor = new Color(0.2f, 0.8f, 1.0f, 1.0f);
            _valueStyle.fontSize = normalSize;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.margin = new RectOffset(4, 4, 2, 2);
            _valueStyle.alignment = TextAnchor.MiddleCenter;
            
            _centeredStyle = new GUIStyle();
            _centeredStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            _centeredStyle.fontSize = normalSize;
            _centeredStyle.alignment = TextAnchor.MiddleCenter;
            
            _panelStyle = new GUIStyle();
            _panelStyle.normal.background = _panelTexture;
            _panelStyle.border = new RectOffset(8, 8, 8, 8);
            _panelStyle.padding = new RectOffset(
                Mathf.RoundToInt(10 * _handleScale), 
                Mathf.RoundToInt(10 * _handleScale), 
                Mathf.RoundToInt(10 * _handleScale), 
                Mathf.RoundToInt(10 * _handleScale)
            );
        }
        
        /// <summary>
        /// Desenha uma interface minimalista com todos os inputs ativos
        /// </summary>
        public void DrawHandles(PlugInputCache cache)
        {
            if (!_isEnabled)
                return;
            
            float scaledPanelWidth = _panelWidth * _handleScale;
            float scaledPanelHeight = _panelHeight * _handleScale;
            float scaledLineHeight = _lineHeight * _handleScale;
            float scaledPadding = _padding * _handleScale;
            float scaledArrowSize = _arrowSize * _handleScale;
            float scaledArrowAreaWidth = _arrowAreaWidth * _handleScale;
                
            if (!Mathf.Approximately(_debugPanelRect.y, Screen.height - scaledPanelHeight - 10))
            {
                _debugPanelRect.y = Screen.height - scaledPanelHeight - 10;
                _debugPanelRect.width = scaledPanelWidth;
                _debugPanelRect.height = scaledPanelHeight;
            }
            
            GUI.Box(_debugPanelRect, "", _panelStyle);
            
            Rect headerRect = new Rect(
                _debugPanelRect.x + scaledPadding,
                _debugPanelRect.y + scaledPadding,
                _debugPanelRect.width - (scaledPadding * 2),
                20 * _handleScale
            );
            GUI.Label(headerRect, "Inputs Ativos", _headerStyle);
            
            Rect lineRect = new Rect(
                _debugPanelRect.x + (scaledPadding * 1.5f),
                headerRect.y + headerRect.height + 5 * _handleScale,
                _debugPanelRect.width - (scaledPadding * 3),
                1 * _handleScale
            );
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(lineRect, _lineTexture);
            GUI.color = Color.white;
            
            float contentY = lineRect.y + lineRect.height + 5 * _handleScale;
            float contentHeight = _debugPanelRect.height - contentY + _debugPanelRect.y - scaledPadding;
            Rect contentRect = new Rect(
                _debugPanelRect.x + scaledPadding,
                contentY,
                _debugPanelRect.width - (scaledPadding * 2),
                contentHeight
            );
            
            _activeInputs.Clear();
            HashSet<string> currentActiveInputs = new HashSet<string>();
            
            float y = contentRect.y;
            int index = 0;
            float maxItems = Mathf.Floor(contentHeight / scaledLineHeight);
            
            foreach (var state in cache.GetStates())
            {
                ProcessInputState(state);
                
                if (ShouldDisplayInput(state))
                {
                    if (index >= maxItems)
                        break;
                        
                    currentActiveInputs.Add(state.Name);
                    _activeInputs.Add(state.Name);
                    
                    if (index % 2 == 1)
                    {
                        Rect rowBgRect = new Rect(
                            contentRect.x,
                            y,
                            contentRect.width,
                            scaledLineHeight
                        );
                        GUI.color = new Color(1f, 1f, 1f, 0.05f);
                        GUI.DrawTexture(rowBgRect, _lineTexture);
                        GUI.color = Color.white;
                    }
                    
                    if (state.IsPressed && !_lastFrameInputs.Contains(state.Name))
                    {
                        Rect glowRect = new Rect(
                            contentRect.x - 5 * _handleScale,
                            y - 2 * _handleScale,
                            contentRect.width + 10 * _handleScale,
                            scaledLineHeight + 4 * _handleScale
                        );
                        GUI.color = new Color(_handleColor.r, _handleColor.g, _handleColor.b, 0.15f);
                        GUI.DrawTexture(glowRect, _panelTexture);
                        GUI.color = Color.white;
                    }
                    
                    Rect nameRect = new Rect(
                        contentRect.x + 5 * _handleScale,
                        y,
                        contentRect.width * 0.35f,
                        scaledLineHeight
                    );
                    GUI.Label(nameRect, state.Name, _labelStyle);
                    
                    string valueText = FormatValue(state);
                    
                    float valueWidth = contentRect.width * 0.35f;
                    Rect valueRect = new Rect(
                        nameRect.x + nameRect.width + 5 * _handleScale, 
                        y,
                        valueWidth,
                        scaledLineHeight
                    );
                    
                    GUI.Label(valueRect, valueText, _valueStyle);
                    
                    if (state.InputType == "Vector2")
                    {
                        Vector2 direction = state.AsVector2;
                        if (direction.magnitude > 0.01f)
                        {
                            Rect arrowRect = new Rect(
                                contentRect.x + contentRect.width - scaledArrowAreaWidth + 5 * _handleScale,
                                y + (scaledLineHeight * 0.5f) - (scaledArrowSize),
                                scaledArrowSize * 2,
                                scaledArrowSize * 2
                            );
                            
                            DrawSimplifiedDirectionArrow(arrowRect, direction, _handleColor);
                        }
                    }
                    
                    y += scaledLineHeight;
                    index++;
                }
            }
            
            _lastFrameInputs.Clear();
            foreach (string input in currentActiveInputs)
            {
                _lastFrameInputs.Add(input);
            }
            
            if (_activeInputs.Count == 0)
            {
                GUI.Label(contentRect, "Nenhum input ativo", _centeredStyle);
            }
        }
        
        /// <summary>
        /// Processa o estado de um input para determinar inatividade
        /// </summary>
        private void ProcessInputState(InputState state)
        {
            string id = state.Name;
            
            if (state.InputType == "Vector2")
            {
                Vector2 vec = state.AsVector2;
                if (vec.magnitude < 0.01f)
                {
                    if (!_inactivityTimers.ContainsKey(id))
                    {
                        _inactivityTimers[id] = 0f;
                    }
                    else
                    {
                        _inactivityTimers[id] += Time.deltaTime;
                    }
                }
                else
                {
                    _inactivityTimers[id] = 0f;
                }
            }
            else
            {
                if (!state.IsPressed)
                {
                    if (!_inactivityTimers.ContainsKey(id))
                    {
                        _inactivityTimers[id] = 0f;
                    }
                    else
                    {
                        _inactivityTimers[id] += Time.deltaTime;
                    }
                }
                else
                {
                    _inactivityTimers[id] = 0f;
                }
            }
        }
        
        /// <summary>
        /// Determina se um input deve ser exibido baseado em sua atividade
        /// </summary>
        private bool ShouldDisplayInput(InputState state)
        {
            string id = state.Name;
            
            if (state.InputType == "Vector2")
            {
                Vector2 vec = state.AsVector2;
                return vec.magnitude >= 0.01f || 
                       !_inactivityTimers.ContainsKey(id) || 
                       _inactivityTimers[id] < INACTIVITY_THRESHOLD;
            }
            else
            {
                return state.IsPressed || 
                       !_inactivityTimers.ContainsKey(id) || 
                       _inactivityTimers[id] < INACTIVITY_THRESHOLD;
            }
        }
        
        /// <summary>
        /// Formata o valor de um input para exibição
        /// </summary>
        private string FormatValue(InputState state)
        {
            string id = state.Name;
            
            if (_valueCache.TryGetValue(id, out string cached))
            {
                if (state.InputType != "Vector2" && !state.IsPressed)
                    return cached;
            }
            
            string result;
            
            switch (state.InputType)
            {
                case "Vector2":
                    Vector2 vec = state.AsVector2;
                    if (vec.magnitude < 0.01f)
                    {
                        result = "(0.0, 0.0)";
                    }
                    else
                    {
                        result = $"({vec.x:F1}, {vec.y:F1})";
                    }
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
                    result = state.RawValue?.ToString() ?? "null";
                    break;
            }
            
            _valueCache[id] = result;
            return result;
        }
        
        /// <summary>
        /// Desenha uma seta simplificada com bolinha que se move conforme a magnitude
        /// </summary>
        private void DrawSimplifiedDirectionArrow(Rect rect, Vector2 direction, Color color)
        {
            Vector2 normalizedDir = direction.normalized;
            normalizedDir.y = -normalizedDir.y;
            
            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
            
            Rect bgRect = new Rect(
                rect.x - 2 * _handleScale, 
                rect.y - 2 * _handleScale, 
                rect.width + 4 * _handleScale, 
                rect.height + 4 * _handleScale
            );
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            GUI.DrawTexture(bgRect, _circleTexture);
            GUI.color = Color.white;
            
            Handles.color = new Color(color.r, color.g, color.b, 0.3f);
            Handles.DrawWireDisc(center, Vector3.forward, rect.width * 0.45f);
            
            float magnitude = Mathf.Clamp01(direction.magnitude);
            Vector2 circlePos = center + normalizedDir * (rect.width * 0.45f * magnitude);
            
            Handles.color = color;
            float lineThickness = 2f * _handleScale; 
            Handles.DrawLine(center, circlePos, lineThickness);
            
            float circleSize = rect.width * 0.18f * _handleScale; 
            Handles.color = color;
            Handles.DrawSolidDisc(circlePos, Vector3.forward, circleSize);
            
            GUI.color = Color.white;
        }
        
        /// <summary>
        /// Cria textura com retângulo arredondado
        /// </summary>
        private Texture2D CreateRoundedRectTexture(int width, int height, int radius, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            texture.SetPixels(colors);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < radius && y < radius)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x >= width - radius && y < radius)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x < radius && y >= height - radius) 
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x >= width - radius && y >= height - radius) 
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height - radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x >= radius && x < width - radius) 
                    {
                        texture.SetPixel(x, y, color);
                    }
                    else if (y >= radius && y < height - radius) 
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Cria textura circular
        /// </summary>
        private Texture2D CreateCircleTexture(int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            float radius = size / 2f;
            float radiusSq = radius * radius;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = radius - x;
                    float dy = radius - y;
                    float distSq = dx * dx + dy * dy;
                    
                    if (distSq <= radiusSq)
                    {
                        float distance = Mathf.Sqrt(distSq);
                        float alpha = Mathf.Clamp01((radius - distance) / radius);
                        
                        texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Classe utilitária para desenho de primitivas
        /// </summary>
        private static class Handles
        {
            private static Texture2D _whiteTexture;
            public static Color color = Color.white;
            
            /// <summary>
            /// Desenha uma linha com espessura customizada
            /// </summary>
            public static void DrawLine(Vector2 start, Vector2 end, float thickness = 1f)
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                
                Color prevColor = GUI.color;
                GUI.color = color;
                
                Vector2 delta = end - start;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                
                GUIUtility.RotateAroundPivot(angle, start);
                GUI.DrawTexture(
                    new Rect(start.x, start.y - thickness / 2, delta.magnitude, thickness),
                    _whiteTexture
                );
                GUIUtility.RotateAroundPivot(-angle, start);
                
                GUI.color = prevColor;
            }
            
            /// <summary>
            /// Desenha um círculo sólido
            /// </summary>
            public static void DrawSolidDisc(Vector2 center, Vector3 normal, float radius)
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                
                Color prevColor = GUI.color;
                GUI.color = color;
                
                Rect rect = new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2);
                GUI.DrawTexture(rect, _whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, radius);
                
                GUI.color = prevColor;
            }
            
            /// <summary>
            /// Desenha um círculo vazio (apenas contorno)
            /// </summary>
            public static void DrawWireDisc(Vector2 center, Vector3 normal, float radius)
            {
                const int segments = 20;
                float angleStep = 360f / segments;
                
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = i * angleStep * Mathf.Deg2Rad;
                    float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                    
                    Vector2 point1 = new Vector2(
                        center.x + Mathf.Cos(angle1) * radius,
                        center.y + Mathf.Sin(angle1) * radius
                    );
                    
                    Vector2 point2 = new Vector2(
                        center.x + Mathf.Cos(angle2) * radius,
                        center.y + Mathf.Sin(angle2) * radius
                    );
                    
                    DrawLine(point1, point2, 1f);
                }
            }
        }
        
        /// <summary>
        /// Limpa os recursos
        /// </summary>
        public void Dispose()
        {
            if (_panelTexture != null)
                Object.Destroy(_panelTexture);
                
            if (_lineTexture != null)
                Object.Destroy(_lineTexture);
                
            if (_circleTexture != null)
                Object.Destroy(_circleTexture);
                
            _valueCache.Clear();
            _activeInputs.Clear();
            _lastFrameInputs.Clear();
            _inactivityTimers.Clear();
        }
    }
}