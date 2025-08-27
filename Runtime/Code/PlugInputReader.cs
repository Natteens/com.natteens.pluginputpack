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
        private bool enableDebug;
        
        [SerializeField, Tooltip("Habilita visualizadores na tela durante debug")]
        private bool enableVisualDebug;
        
        [Header("Configurações Visuais")]
        [SerializeField, Tooltip("Tamanho dos elementos visuais de debug (1-300)")]
        [Range(1f, 300f)]
        private float debugHandleSize = 100f;
        
        [SerializeField, Tooltip("Cor dos elementos de visualização")]
        private Color debugHandleColor = Color.yellow;
        
        [Header("Gerenciamento de Dispositivos")]
        [SerializeField, Tooltip("Habilita detecção automática de dispositivos")]
        private bool enableDeviceManagement = true;
        
        [SerializeField, Tooltip("Isola inputs por dispositivo atual")]
        private bool strictDeviceIsolation = false;
        
        [SerializeField, Tooltip("Tempo de cooldown entre trocas de dispositivo (segundos)")]
        [Range(0f, 2f)]
        private float deviceSwitchCooldown = 0.1f;
        
        [SerializeField, Tooltip("Dispositivos permitidos (vazio = todos)")]
        private PlugInputDeviceManager.DeviceType[] allowedDevices = new PlugInputDeviceManager.DeviceType[0];
        
        [Header("Configurações do Cursor")]
        [SerializeField, Tooltip("Oculta cursor quando usando gamepad")]
        private bool hideCursorOnGamepad = true;
        
        [SerializeField, Tooltip("Bloqueia cursor quando usando gamepad")]
        private bool lockCursorOnGamepad = false;
        
        [SerializeField, Tooltip("Modo do cursor quando gamepad está ativo")]
        private CursorLockMode gamepadCursorLockMode = CursorLockMode.None;
        
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
        /// Define se o gerenciamento de dispositivos está ativo
        /// </summary>
        public bool EnableDeviceManagement => enableDeviceManagement;
        
        /// <summary>
        /// Define se o isolamento estrito está ativo
        /// </summary>
        public bool StrictDeviceIsolation => strictDeviceIsolation;
        
        /// <summary>
        /// Tempo de cooldown entre trocas de dispositivo
        /// </summary>
        public float DeviceSwitchCooldown => deviceSwitchCooldown;
        
        /// <summary>
        /// Dispositivos permitidos
        /// </summary>
        public PlugInputDeviceManager.DeviceType[] AllowedDevices => allowedDevices;
        
        /// <summary>
        /// Define se deve ocultar cursor com gamepad
        /// </summary>
        public bool HideCursorOnGamepad => hideCursorOnGamepad;
        
        /// <summary>
        /// Define se deve bloquear cursor com gamepad
        /// </summary>
        public bool LockCursorOnGamepad => lockCursorOnGamepad;
        
        /// <summary>
        /// Modo do cursor quando gamepad está ativo
        /// </summary>
        public CursorLockMode GamepadCursorLockMode => gamepadCursorLockMode;
        
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
            
            var deviceInfo = enableDeviceManagement ? 
                $", Dispositivos: {(allowedDevices.Length > 0 ? allowedDevices.Length.ToString() : "Todos")}" : "";
            
            return $"Mapas: {inputActionAsset.actionMaps.Count}, Ações: {totalActions}{deviceInfo}";
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Validação no editor
        /// </summary>
        private void OnValidate()
        {
            debugHandleSize = Mathf.Clamp(debugHandleSize, 1f, 300f);
            debugHandleColor.a = Mathf.Clamp01(debugHandleColor.a);
            deviceSwitchCooldown = Mathf.Clamp(deviceSwitchCooldown, 0f, 2f);
        }
        #endif
    }
}