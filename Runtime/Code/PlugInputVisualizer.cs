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
        private float _handleScale = 1.0f; // Fator de escala baseado no debugHandleSize
        
        // Cache de valores para evitar alocações excessivas
        private readonly Dictionary<string, string> _valueCache = new Dictionary<string, string>();
        private readonly List<string> _activeInputs = new List<string>();
        private readonly HashSet<string> _lastFrameInputs = new HashSet<string>();
        
        // Inatividade de inputs
        private Dictionary<string, float> _inactivityTimers = new Dictionary<string, float>();
        private const float INACTIVITY_THRESHOLD = 0.2f;
        
        // Texturas para desenho
        private Texture2D _panelTexture;
        private Texture2D _lineTexture;
        private Texture2D _circleTexture;
        
        // Estilos para GUI
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _centeredStyle;
        private GUIStyle _panelStyle;
        
        // Constantes para layout - Ajustáveis pelo fator de escala
        private float _panelWidth = 300f;  // Base width, será multiplicada por _handleScale
        private float _panelHeight = 320f; // Base height, será multiplicada por _handleScale
        private float _lineHeight = 24f;   // Base line height, será multiplicada por _handleScale
        private float _padding = 10f;      // Base padding, será multiplicado por _handleScale
        private float _arrowSize = 16f;    // Base arrow size, será multiplicado por _handleScale
        private float _arrowAreaWidth = 50f; // Base arrow area width, será multiplicada por _handleScale
        
        /// <summary>
        /// Inicializa o visualizador
        /// </summary>
        public void Initialize(bool enabled, float handleSize, Color handleColor)
        {
            _isEnabled = enabled;
            _handleColor = handleColor;
            _handleScale = handleSize; // Usa o handleSize como fator de escala
            
            // Ajusta dimensões com base na escala
            float scaledPanelWidth = _panelWidth * _handleScale;
            float scaledPanelHeight = _panelHeight * _handleScale;
            
            // Posiciona o painel no canto inferior esquerdo
            _debugPanelRect = new Rect(
                10, 
                Screen.height - scaledPanelHeight - 10, 
                scaledPanelWidth, 
                scaledPanelHeight
            );
            
            // Cria texturas necessárias
            CreateTextures();
            
            // Inicializa estilos
            InitializeStyles();
            
            // Limpa caches
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
            // Painel com cantos arredondados
            _panelTexture = CreateRoundedRectTexture(32, 32, 8, new Color(0.12f, 0.12f, 0.14f, 0.92f));
            
            // Textura para linhas
            _lineTexture = new Texture2D(1, 1);
            _lineTexture.SetPixel(0, 0, Color.white);
            _lineTexture.Apply();
            
            // Textura para círculos
            _circleTexture = CreateCircleTexture(16, Color.white);
        }
        
        /// <summary>
        /// Inicializa os estilos para a GUI
        /// </summary>
        private void InitializeStyles()
        {
            // Ajusta tamanho das fontes com base na escala
            int headerSize = Mathf.RoundToInt(14 * _handleScale);
            int normalSize = Mathf.RoundToInt(12 * _handleScale);
            
            // Estilo do título
            _headerStyle = new GUIStyle();
            _headerStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
            _headerStyle.fontSize = headerSize;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.margin = new RectOffset(4, 4, 4, 4);
            _headerStyle.alignment = TextAnchor.MiddleLeft;
            
            // Estilo dos rótulos
            _labelStyle = new GUIStyle();
            _labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
            _labelStyle.fontSize = normalSize;
            _labelStyle.margin = new RectOffset(4, 4, 2, 2);
            _labelStyle.alignment = TextAnchor.MiddleLeft;
            
            // Estilo dos valores (agora CENTRALIZADOS)
            _valueStyle = new GUIStyle();
            _valueStyle.normal.textColor = new Color(0.2f, 0.8f, 1.0f, 1.0f);
            _valueStyle.fontSize = normalSize;
            _valueStyle.fontStyle = FontStyle.Bold;
            _valueStyle.margin = new RectOffset(4, 4, 2, 2);
            _valueStyle.alignment = TextAnchor.MiddleCenter; // MUDADO para centralizar
            
            // Estilo para mensagens centralizadas
            _centeredStyle = new GUIStyle();
            _centeredStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            _centeredStyle.fontSize = normalSize;
            _centeredStyle.alignment = TextAnchor.MiddleCenter;
            
            // Estilo do painel
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
            
            // Valores escalados para uso no layout
            float scaledPanelWidth = _panelWidth * _handleScale;
            float scaledPanelHeight = _panelHeight * _handleScale;
            float scaledLineHeight = _lineHeight * _handleScale;
            float scaledPadding = _padding * _handleScale;
            float scaledArrowSize = _arrowSize * _handleScale;
            float scaledArrowAreaWidth = _arrowAreaWidth * _handleScale;
                
            // Atualiza posição do painel se necessário
            if (_debugPanelRect.y != Screen.height - scaledPanelHeight - 10)
            {
                _debugPanelRect.y = Screen.height - scaledPanelHeight - 10;
                _debugPanelRect.width = scaledPanelWidth;
                _debugPanelRect.height = scaledPanelHeight;
            }
            
            // Desenha o painel de fundo
            GUI.Box(_debugPanelRect, "", _panelStyle);
            
            // Título
            Rect headerRect = new Rect(
                _debugPanelRect.x + scaledPadding,
                _debugPanelRect.y + scaledPadding,
                _debugPanelRect.width - (scaledPadding * 2),
                20 * _handleScale
            );
            GUI.Label(headerRect, "Inputs Ativos", _headerStyle);
            
            // Linha separadora
            Rect lineRect = new Rect(
                _debugPanelRect.x + (scaledPadding * 1.5f),
                headerRect.y + headerRect.height + 5 * _handleScale,
                _debugPanelRect.width - (scaledPadding * 3),
                1 * _handleScale
            );
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(lineRect, _lineTexture);
            GUI.color = Color.white;
            
            // Área de conteúdo
            float contentY = lineRect.y + lineRect.height + 5 * _handleScale;
            float contentHeight = _debugPanelRect.height - contentY + _debugPanelRect.y - scaledPadding;
            Rect contentRect = new Rect(
                _debugPanelRect.x + scaledPadding,
                contentY,
                _debugPanelRect.width - (scaledPadding * 2),
                contentHeight
            );
            
            // Controle de inputs ativos
            _activeInputs.Clear();
            HashSet<string> currentActiveInputs = new HashSet<string>();
            
            // Desenha cada input
            float y = contentRect.y;
            int index = 0;
            float maxItems = Mathf.Floor(contentHeight / scaledLineHeight);
            
            foreach (var state in cache.GetStates())
            {
                // Gerencia o timer de inatividade
                ProcessInputState(state);
                
                // Verifica se o input deve ser exibido
                if (ShouldDisplayInput(state))
                {
                    if (index >= maxItems)
                        break;
                        
                    currentActiveInputs.Add(state.Name);
                    _activeInputs.Add(state.Name);
                    
                    // Destaca linhas alternadas para melhor legibilidade
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
                    
                    // Efeito de highlight para inputs recém ativados
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
                    
                    // Nome do input - usa 35% da largura disponível
                    Rect nameRect = new Rect(
                        contentRect.x + 5 * _handleScale,
                        y,
                        contentRect.width * 0.35f,
                        scaledLineHeight
                    );
                    GUI.Label(nameRect, state.Name, _labelStyle);
                    
                    // Valor do input - CENTRALIZADO, usa 40% da largura
                    string valueText = FormatValue(state);
                    
                    // Calcula espaço para o valor, agora CENTRALIZADO entre o nome e a seta
                    float valueWidth = contentRect.width * 0.35f;
                    Rect valueRect = new Rect(
                        nameRect.x + nameRect.width + 5 * _handleScale, // Posicionado após o nome
                        y,
                        valueWidth,
                        scaledLineHeight
                    );
                    
                    GUI.Label(valueRect, valueText, _valueStyle);
                    
                    // Para valores Vector2, desenha uma seta indicativa com tamanho aumentado
                    if (state.InputType == "Vector2")
                    {
                        Vector2 direction = state.AsVector2;
                        if (direction.magnitude > 0.01f)
                        {
                            // Área da seta agora bem maior e no extremo direito
                            Rect arrowRect = new Rect(
                                contentRect.x + contentRect.width - scaledArrowAreaWidth + 5 * _handleScale,
                                y + (scaledLineHeight * 0.5f) - (scaledArrowSize),
                                scaledArrowSize * 2,
                                scaledArrowSize * 2
                            );
                            
                            // Nova implementação de seta com tamanho baseado no handleScale
                            DrawSimplifiedDirectionArrow(arrowRect, direction, _handleColor);
                        }
                    }
                    
                    y += scaledLineHeight;
                    index++;
                }
            }
            
            // Atualiza os inputs do último frame
            _lastFrameInputs.Clear();
            foreach (string input in currentActiveInputs)
            {
                _lastFrameInputs.Add(input);
            }
            
            // Se não há inputs ativos
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
            
            // Lógica para Vector2
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
            // Lógica para outros tipos
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
            
            // Para Vector2
            if (state.InputType == "Vector2")
            {
                Vector2 vec = state.AsVector2;
                return vec.magnitude >= 0.01f || 
                       !_inactivityTimers.ContainsKey(id) || 
                       _inactivityTimers[id] < INACTIVITY_THRESHOLD;
            }
            // Para outros tipos
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
            
            // Usa cache para evitar alocações
            if (_valueCache.TryGetValue(id, out string cached))
            {
                // Para Vector2, sempre atualiza para valores em tempo real
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
            // Normaliza a direção e inverte Y para corresponder à direção na tela
            Vector2 normalizedDir = direction.normalized;
            normalizedDir.y = -normalizedDir.y;
            
            // Ponto central da área
            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
            
            // Desenha fundo para a área da seta - aumentado com base em _handleScale
            Rect bgRect = new Rect(
                rect.x - 2 * _handleScale, 
                rect.y - 2 * _handleScale, 
                rect.width + 4 * _handleScale, 
                rect.height + 4 * _handleScale
            );
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            GUI.DrawTexture(bgRect, _circleTexture);
            GUI.color = Color.white;
            
            // Desenha círculo de referência
            Handles.color = new Color(color.r, color.g, color.b, 0.3f);
            Handles.DrawWireDisc(center, Vector3.forward, rect.width * 0.45f);
            
            // Calcula posição da bolinha proporcional à magnitude do vetor
            float magnitude = Mathf.Clamp01(direction.magnitude); // Normaliza entre 0-1
            Vector2 circlePos = center + normalizedDir * (rect.width * 0.45f * magnitude);
            
            // Desenha a linha do centro até a bolinha - ESPESSURA AUMENTADA
            Handles.color = color;
            float lineThickness = 2f * _handleScale; // Espessura da linha escalada
            Handles.DrawLine(center, circlePos, lineThickness);
            
            // Desenha a bolinha na ponta - TAMANHO AUMENTADO
            float circleSize = rect.width * 0.18f * _handleScale; // Tamanho da bolinha escalado
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
            
            // Preenche com transparência
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            texture.SetPixels(colors);
            
            // Desenha o retângulo com cantos arredondados
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Cantos
                    if (x < radius && y < radius) // Superior esquerdo
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x >= width - radius && y < radius) // Superior direito
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x < radius && y >= height - radius) // Inferior esquerdo
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x >= width - radius && y >= height - radius) // Inferior direito
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height - radius));
                        if (distance <= radius)
                            texture.SetPixel(x, y, color);
                    }
                    else if (x >= radius && x < width - radius) // Centro horizontal
                    {
                        texture.SetPixel(x, y, color);
                    }
                    else if (y >= radius && y < height - radius) // Centro vertical
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
                        // Suaviza as bordas
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