using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
    /// <summary>
    /// ScriptableObject que armazena a configuração do sistema de input.
    /// </summary>
    [CreateAssetMenu(fileName = "PlugInputReader", menuName = "Plug Input Pack/Input Reader")]
    public class PlugInputReader : ScriptableObject
    {
        [SerializeField] 
        private InputActionAsset inputActionAsset;
        
        [SerializeField, Tooltip("Habilita logs de debug")]
        private bool enableDebug = false;
        
        [SerializeField, Tooltip("Habilita visualizadores na tela quando em modo de debug")]
        private bool enableVisualDebug = false;
        
        [SerializeField, Tooltip("Tamanho dos handles visuais de debug")]
        private float debugHandleSize = 100f;
        
        [SerializeField, Tooltip("Cor dos handles de visualização")]
        private Color debugHandleColor = new Color(0, 1, 0, 0.5f);
        
        /// <summary>
        /// Asset de ações do Unity Input System
        /// </summary>
        public InputActionAsset InputActionAsset => inputActionAsset;
        
        /// <summary>
        /// Define se o debug está ativado
        /// </summary>
        public bool EnableDebug => enableDebug;
        
        /// <summary>
        /// Define se a visualização na tela está ativada
        /// </summary>
        public bool EnableVisualDebug => enableVisualDebug;
        
        /// <summary>
        /// Tamanho dos handles visuais
        /// </summary>
        public float DebugHandleSize => debugHandleSize;
        
        /// <summary>
        /// Cor dos handles visuais
        /// </summary>
        public Color DebugHandleColor => debugHandleColor;
    }
}