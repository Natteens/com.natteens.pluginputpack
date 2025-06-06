using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
    /// <summary>
    /// ScriptableObject que armazena a configuração do sistema de input.
    /// </summary>
    [CreateAssetMenu(fileName = "New PlugInputReader", menuName = "Plug Input Pack/Input Reader")]
    public class PlugInputReader : ScriptableObject
    {
        [Header("Configuração Principal")]
        [SerializeField, Tooltip("Asset de ações do Unity Input System")]
        private InputActionAsset inputActionAsset;
        
        [Header("Configurações de Debug")]
        [SerializeField, Tooltip("Habilita logs de debug no console")]
        private bool enableDebug = false;
        
        [SerializeField, Tooltip("Habilita visualizadores na tela durante debug")]
        private bool enableVisualDebug = false;
        
        [Header("Configurações Visuais")]
        [SerializeField, Tooltip("Tamanho dos elementos visuais de debug (1-300)")]
        [Range(1f, 300f)]
        private float debugHandleSize = 100f;
        
        [SerializeField, Tooltip("Cor dos elementos de visualização")]
        private Color debugHandleColor = new Color(0, 1, 0, 0.8f);
        
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
        /// Tamanho dos elementos visuais
        /// </summary>
        public float DebugHandleSize => debugHandleSize;
        
        /// <summary>
        /// Cor dos elementos visuais
        /// </summary>
        public Color DebugHandleColor => debugHandleColor;
        
        /// <summary>
        /// Valida a configuração do Input Reader
        /// </summary>
        public bool IsValid()
        {
            if (inputActionAsset == null)
            {
                Debug.LogWarning($"PlugInputReader '{name}': InputActionAsset não está configurado!");
                return false;
            }
            
            if (inputActionAsset.actionMaps.Count == 0)
            {
                Debug.LogWarning($"PlugInputReader '{name}': InputActionAsset não possui mapas de ação!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Obtém informações de debug sobre a configuração
        /// </summary>
        public string GetDebugInfo()
        {
            if (!IsValid())
                return "Configuração inválida";
                
            int totalActions = 0;
            foreach (var map in inputActionAsset.actionMaps)
            {
                totalActions += map.actions.Count;
            }
            
            return $"Mapas: {inputActionAsset.actionMaps.Count}, Ações: {totalActions}";
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Validação no editor
        /// </summary>
        private void OnValidate()
        {
            debugHandleSize = Mathf.Clamp(debugHandleSize, 1f, 300f);
            debugHandleColor.a = Mathf.Clamp01(debugHandleColor.a);
        }
        #endif
    }
}