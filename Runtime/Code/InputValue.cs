using UnityEngine;

namespace PlugInputPack
{
    /// <summary>
    /// Stores an input value without boxing. Replaces the previous object-based storage.
    /// All value types are stored directly as fields — no heap allocation occurs.
    /// </summary>
    public struct InputValue
    {
        public enum ValueKind : byte
        {
            Bool,
            Float,
            Int,
            Vector2,
            Vector3
        }

        public ValueKind Kind;

        // All possible values stored inline — only the active one is read
        public bool   BoolVal;
        public float  FloatVal;
        public int    IntVal;
        public Vector2 Vec2Val;
        public Vector3 Vec3Val;

        // --- Static factories ---

        public static InputValue FromBool(bool v)    => new InputValue { Kind = ValueKind.Bool,    BoolVal  = v };
        public static InputValue FromFloat(float v)  => new InputValue { Kind = ValueKind.Float,   FloatVal = v };
        public static InputValue FromInt(int v)      => new InputValue { Kind = ValueKind.Int,     IntVal   = v };
        public static InputValue FromVector2(Vector2 v) => new InputValue { Kind = ValueKind.Vector2, Vec2Val = v };
        public static InputValue FromVector3(Vector3 v) => new InputValue { Kind = ValueKind.Vector3, Vec3Val = v };

        public static readonly InputValue DefaultBool    = FromBool(false);
        public static readonly InputValue DefaultFloat   = FromFloat(0f);
        public static readonly InputValue DefaultInt     = FromInt(0);
        public static readonly InputValue DefaultVector2 = FromVector2(Vector2.zero);
        public static readonly InputValue DefaultVector3 = FromVector3(Vector3.zero);

        // --- Conversions (no alloc) ---

        public bool IsActive()
        {
            switch (Kind)
            {
                case ValueKind.Bool:    return BoolVal;
                case ValueKind.Float:   return Mathf.Abs(FloatVal) > 0.1f;
                case ValueKind.Int:     return IntVal != 0;
                case ValueKind.Vector2: return Vec2Val.sqrMagnitude > 0.01f;
                case ValueKind.Vector3: return Vec3Val.sqrMagnitude > 0.01f;
                default:                return false;
            }
        }

        public bool AsBool()
        {
            switch (Kind)
            {
                case ValueKind.Bool:    return BoolVal;
                case ValueKind.Float:   return Mathf.Abs(FloatVal) > 0.1f;
                case ValueKind.Int:     return IntVal != 0;
                case ValueKind.Vector2: return Vec2Val.sqrMagnitude > 0.01f;
                case ValueKind.Vector3: return Vec3Val.sqrMagnitude > 0.01f;
                default:                return false;
            }
        }

        public float AsFloat()
        {
            switch (Kind)
            {
                case ValueKind.Bool:    return BoolVal ? 1f : 0f;
                case ValueKind.Float:   return FloatVal;
                case ValueKind.Int:     return IntVal;
                case ValueKind.Vector2: return Vec2Val.magnitude;
                case ValueKind.Vector3: return Vec3Val.magnitude;
                default:                return 0f;
            }
        }

        public int AsInt()
        {
            switch (Kind)
            {
                case ValueKind.Bool:    return BoolVal ? 1 : 0;
                case ValueKind.Float:   return Mathf.RoundToInt(FloatVal);
                case ValueKind.Int:     return IntVal;
                case ValueKind.Vector2: return Mathf.RoundToInt(Vec2Val.magnitude);
                case ValueKind.Vector3: return Mathf.RoundToInt(Vec3Val.magnitude);
                default:                return 0;
            }
        }

        public Vector2 AsVector2()
        {
            switch (Kind)
            {
                case ValueKind.Vector2: return Vec2Val;
                case ValueKind.Vector3: return new Vector2(Vec3Val.x, Vec3Val.y);
                case ValueKind.Float:   return new Vector2(FloatVal, 0f);
                case ValueKind.Bool:    return new Vector2(BoolVal ? 1f : 0f, 0f);
                case ValueKind.Int:     return new Vector2(IntVal, 0f);
                default:                return Vector2.zero;
            }
        }

        public Vector3 AsVector3()
        {
            switch (Kind)
            {
                case ValueKind.Vector3: return Vec3Val;
                case ValueKind.Vector2: return new Vector3(Vec2Val.x, Vec2Val.y, 0f);
                case ValueKind.Float:   return new Vector3(FloatVal, 0f, 0f);
                case ValueKind.Bool:    return new Vector3(BoolVal ? 1f : 0f, 0f, 0f);
                case ValueKind.Int:     return new Vector3(IntVal, 0f, 0f);
                default:                return Vector3.zero;
            }
        }

        /// <summary>
        /// Equality check without boxing — compares by kind and value directly.
        /// </summary>
        public bool Equals(InputValue other)
        {
            if (Kind != other.Kind) return false;
            switch (Kind)
            {
                case ValueKind.Bool:    return BoolVal == other.BoolVal;
                case ValueKind.Float:   return Mathf.Abs(FloatVal - other.FloatVal) < 0.001f;
                case ValueKind.Int:     return IntVal == other.IntVal;
                case ValueKind.Vector2: return (Vec2Val - other.Vec2Val).sqrMagnitude < 0.000001f;
                case ValueKind.Vector3: return (Vec3Val - other.Vec3Val).sqrMagnitude < 0.000001f;
                default:                return false;
            }
        }

        /// <summary>
        /// For debug display only — string allocation is acceptable here.
        /// </summary>
        public override string ToString()
        {
            switch (Kind)
            {
                case ValueKind.Bool:    return BoolVal.ToString();
                case ValueKind.Float:   return FloatVal.ToString("F2");
                case ValueKind.Int:     return IntVal.ToString();
                case ValueKind.Vector2: return $"({Vec2Val.x:F2}, {Vec2Val.y:F2})";
                case ValueKind.Vector3: return $"({Vec3Val.x:F2}, {Vec3Val.y:F2}, {Vec3Val.z:F2})";
                default:                return "unknown";
            }
        }
    }
}