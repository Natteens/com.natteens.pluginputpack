using UnityEngine;
using UnityEngine.InputSystem;

namespace PlugInputPack
{
    /// <summary>
    /// Reads raw input values from the Unity Input System into InputValue structs.
    /// No boxing occurs — all values are stored as typed fields.
    /// </summary>
    public static class PlugInputProcessor
    {
        /// <summary>
        /// Reads the value from an InputAction callback into a zero-alloc InputValue.
        /// </summary>
        public static InputValue ReadValue(InputAction.CallbackContext context, string expectedType)
        {
            try
            {
                switch (expectedType)
                {
                    case "Vector2":
                        return InputValue.FromVector2(context.ReadValue<Vector2>());

                    case "Vector3":
                        return InputValue.FromVector3(context.ReadValue<Vector3>());

                    case "Button":
                    case "Digital":
                        return InputValue.FromBool(context.ReadValueAsButton());

                    case "Axis":
                    case "Analog":
                        return InputValue.FromFloat(context.ReadValue<float>());

                    case "Integer":
                        return InputValue.FromInt(context.ReadValue<int>());

                    default:
                        return ReadValueFallback(context, expectedType);
                }
            }
            catch
            {
                return GetDefaultValue(expectedType);
            }
        }

        private static InputValue ReadValueFallback(InputAction.CallbackContext context, string expectedType)
        {
            var controlType = context.control?.valueType;

            if (controlType == typeof(float))   return InputValue.FromFloat(context.ReadValue<float>());
            if (controlType == typeof(Vector2)) return InputValue.FromVector2(context.ReadValue<Vector2>());
            if (controlType == typeof(bool))    return InputValue.FromBool(context.ReadValueAsButton());

            Debug.LogWarning($"PlugInputPack: Unsupported control type: {expectedType} ({controlType?.Name ?? "null"})");
            return InputValue.DefaultBool;
        }

        /// <summary>
        /// Returns a zero-value InputValue for the given control type.
        /// </summary>
        public static InputValue GetDefaultValue(string expectedType)
        {
            switch (expectedType)
            {
                case "Vector2":                  return InputValue.DefaultVector2;
                case "Vector3":                  return InputValue.DefaultVector3;
                case "Button":  case "Digital":  return InputValue.DefaultBool;
                case "Axis":    case "Analog":   return InputValue.DefaultFloat;
                case "Integer":                  return InputValue.DefaultInt;
                default:                         return InputValue.DefaultBool;
            }
        }
    }
}