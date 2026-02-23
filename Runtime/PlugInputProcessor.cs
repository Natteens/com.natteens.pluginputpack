using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
    /// <summary>
    /// Reads raw input values from Unity Input System callbacks into InputValue structs.
    /// </summary>
    public static class PlugInputProcessor
    {
        private static readonly System.Type s_typeFloat   = typeof(float);
        private static readonly System.Type s_typeVector2 = typeof(Vector2);
        private static readonly System.Type s_typeVector3 = typeof(Vector3);
        private static readonly System.Type s_typeBool    = typeof(bool);
        private static readonly System.Type s_typeInt     = typeof(int);

        
        public static InputValue ReadValue(InputAction.CallbackContext context, string expectedType)
        {
            switch (expectedType)
            {
                case "Vector2":  return InputValue.FromVector2(context.ReadValue<Vector2>());
                case "Vector3":  return InputValue.FromVector3(context.ReadValue<Vector3>());
                case "Button":
                case "Digital":  return InputValue.FromBool(context.ReadValueAsButton());
                case "Axis":
                case "Analog":   return InputValue.FromFloat(context.ReadValue<float>());
                case "Integer":  return InputValue.FromInt(context.ReadValue<int>());
                default:         return ReadValueFallback(context, expectedType);
            }
        }
        
        private static InputValue ReadValueFallback(InputAction.CallbackContext context, string expectedType)
        {
            System.Type vt = context.control?.valueType;

            if (vt == s_typeFloat)   return InputValue.FromFloat(context.ReadValue<float>());
            if (vt == s_typeVector2) return InputValue.FromVector2(context.ReadValue<Vector2>());
            if (vt == s_typeVector3) return InputValue.FromVector3(context.ReadValue<Vector3>());
            if (vt == s_typeBool)    return InputValue.FromBool(context.ReadValueAsButton());
            if (vt == s_typeInt)     return InputValue.FromInt(context.ReadValue<int>());

            Debug.LogWarning($"[PlugInput] Unsupported control type '{expectedType}' ({vt?.Name ?? "null"}).");
            return InputValue.DefaultBool;
        }

        public static InputValue GetDefaultValue(string expectedType)
        {
            switch (expectedType)
            {
                case "Vector2":                 return InputValue.DefaultVector2;
                case "Vector3":                 return InputValue.DefaultVector3;
                case "Button": case "Digital":  return InputValue.DefaultBool;
                case "Axis":   case "Analog":   return InputValue.DefaultFloat;
                case "Integer":                 return InputValue.DefaultInt;
                default:                        return InputValue.DefaultBool;
            }
        }
    }
}