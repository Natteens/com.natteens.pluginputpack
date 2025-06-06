using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace PlugInputPack
{
    /// <summary>
    /// Classe utilitária para processamento de valores de input.
    /// </summary>
    public static class PlugInputProcessor
    {
        /// <summary>
        /// Lê o valor de um contexto de input com base no tipo esperado
        /// </summary>
        public static object ReadValue(InputAction.CallbackContext context, string expectedType)
        {
            try
            {
                switch (expectedType)
                {
                    case "Vector2":
                        return context.ReadValue<Vector2>();
                        
                    case "Vector3":
                        return context.ReadValue<Vector3>();
                        
                    case "Button":
                    case "Digital":
                        return context.ReadValueAsButton();
                        
                    case "Axis":
                    case "Analog":
                        return context.ReadValue<float>();
                        
                    case "Integer":
                        return context.ReadValue<int>();
                        
                    default:
                        try
                        {
                            return context.ReadValueAsButton();
                        }
                        catch
                        {
                            try 
                            {
                                return context.ReadValue<float>();
                            }
                            catch
                            {
                                try
                                {
                                    return context.ReadValue<Vector2>();
                                }
                                catch
                                {
                                    Debug.LogWarning($"PlugInputPack: Tipo de controle não suportado: {expectedType}");
                                    return false;
                                }
                            }
                        }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Erro ao ler valor de input: {e.Message}");
                return GetDefaultValue(expectedType);
            }
        }
        
        /// <summary>
        /// Obtém o valor padrão para um tipo de input
        /// </summary>
        public static object GetDefaultValue(string expectedType)
        {
            switch (expectedType)
            {
                case "Vector2":
                    return Vector2.zero;
                    
                case "Vector3":
                    return Vector3.zero;
                    
                case "Button":
                case "Digital":
                    return false;
                    
                case "Axis":
                case "Analog":
                    return 0f;
                    
                case "Integer":
                    return 0;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Verifica se um valor é considerado "ativo" (pressionado/não-zero)
        /// </summary>
        public static bool IsValueActive(object value)
        {
            if (value is bool boolValue)
                return boolValue;
            
            if (value is float floatValue)
                return Mathf.Abs(floatValue) > 0.1f;
            
            if (value is Vector2 vector2Value)
                return vector2Value.magnitude > 0.1f;
            
            if (value is Vector3 vector3Value)
                return vector3Value.magnitude > 0.1f;
            
            if (value is int intValue)
                return intValue != 0;
            
            return false;
        }
        
        /// <summary>
        /// Converte um valor para Vector2
        /// </summary>
        public static Vector2 ConvertToVector2(object value)
        {
            if (value is Vector2 vector2Value)
                return vector2Value;
            
            if (value is Vector3 vector3Value)
                return new Vector2(vector3Value.x, vector3Value.y);
            
            if (value is float floatValue)
                return new Vector2(floatValue, 0);
            
            if (value is bool boolValue)
                return new Vector2(boolValue ? 1 : 0, 0);
            
            if (value is int intValue)
                return new Vector2(intValue, 0);
            
            return Vector2.zero;
        }
        
        /// <summary>
        /// Converte um valor para Vector3
        /// </summary>
        public static Vector3 ConvertToVector3(object value)
        {
            if (value is Vector3 vector3Value)
                return vector3Value;
            
            if (value is Vector2 vector2Value)
                return new Vector3(vector2Value.x, vector2Value.y, 0);
            
            if (value is float floatValue)
                return new Vector3(floatValue, 0, 0);
            
            if (value is bool boolValue)
                return new Vector3(boolValue ? 1 : 0, 0, 0);
            
            if (value is int intValue)
                return new Vector3(intValue, 0, 0);
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Converte um valor para float
        /// </summary>
        public static float ConvertToFloat(object value)
        {
            if (value is float floatValue)
                return floatValue;
            
            if (value is bool boolValue)
                return boolValue ? 1f : 0f;
            
            if (value is int intValue)
                return intValue;
            
            if (value is Vector2 vector2Value)
                return vector2Value.magnitude;
            
            if (value is Vector3 vector3Value)
                return vector3Value.magnitude;
            
            return 0f;
        }
        
        /// <summary>
        /// Converte um valor para bool
        /// </summary>
        public static bool ConvertToBool(object value)
        {
            if (value is bool boolValue)
                return boolValue;
            
            if (value is float floatValue)
                return Mathf.Abs(floatValue) > 0.1f;
            
            if (value is int intValue)
                return intValue != 0;
            
            if (value is Vector2 vector2Value)
                return vector2Value.magnitude > 0.1f;
            
            if (value is Vector3 vector3Value)
                return vector3Value.magnitude > 0.1f;
            
            return false;
        }
        
        /// <summary>
        /// Converte um valor para int
        /// </summary>
        public static int ConvertToInt(object value)
        {
            if (value is int intValue)
                return intValue;
            
            if (value is float floatValue)
                return Mathf.RoundToInt(floatValue);
            
            if (value is bool boolValue)
                return boolValue ? 1 : 0;
            
            if (value is Vector2 vector2Value)
                return Mathf.RoundToInt(vector2Value.magnitude);
            
            if (value is Vector3 vector3Value)
                return Mathf.RoundToInt(vector3Value.magnitude);
            
            return 0;
        }
    }
}