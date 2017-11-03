using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeepCopy
{
    /// <summary>
    /// Methods for determining the copyability of types and fields.
    /// </summary>
    internal sealed class CopyPolicy
    {
        private readonly ConcurrentDictionary<Type, bool> shallowCopyableTypes = new ConcurrentDictionary<Type, bool>();
        private readonly ConcurrentDictionary<Type, bool> immutableTypes = new ConcurrentDictionary<Type, bool>();
        private readonly RuntimeTypeHandle intPtrTypeHandle = typeof(IntPtr).TypeHandle;
        private readonly RuntimeTypeHandle uIntPtrTypeHandle = typeof(UIntPtr).TypeHandle;
        private readonly Type delegateType = typeof(Delegate);

        public CopyPolicy()
        {
            this.immutableTypes[typeof(Decimal)] = true;
            this.immutableTypes[typeof(DateTime)] = true;
            this.immutableTypes[typeof(TimeSpan)] = true;
            this.immutableTypes[typeof(string)] = true;
        }

        /// <summary>
        /// Returns a sorted list of the copyable fields of the provided type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A sorted list of the fields of the provided type.</returns>
        public List<FieldInfo> GetCopyableFields(Type type)
        {
            var result =
                GetAllFields(type)
                    .Where(field => IsSupportedFieldType(field.FieldType))
                    .ToList();
            result.Sort(FieldInfoComparer.Instance);
            return result;

            IEnumerable<FieldInfo> GetAllFields(Type containingType)
            {
                const BindingFlags allFields =
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                var current = containingType;
                while ((current != typeof(object)) && (current != null))
                {
                    var fields = current.GetFields(allFields);
                    foreach (var field in fields)
                    {
                        yield return field;
                    }

                    current = current.BaseType;
                }
            }

            bool IsSupportedFieldType(Type fieldType)
            {
                if (fieldType.IsPointer || fieldType.IsByRef) return false;

                var handle = fieldType.TypeHandle;
                if (handle.Equals(this.intPtrTypeHandle)) return false;
                if (handle.Equals(this.uIntPtrTypeHandle)) return false;
                if (this.delegateType.IsAssignableFrom(fieldType)) return false;

                return true;
            }
        }

        /// <summary>
        /// Returns true if the provided type can be shallow-copied, false if it must be deep copied instead.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the provided type can be shallow-copied, false if it must be deep copied instead.</returns>
        public bool IsShallowCopyable(Type type)
        {
            if (this.shallowCopyableTypes.TryGetValue(type, out var result))
            {
                return result;
            }

            if (this.IsImmutable(type))
            {
                return this.shallowCopyableTypes[type] = true;
            }

            if (type.IsValueType && !type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                result = this.GetCopyableFields(type).All(f => f.FieldType != type && this.IsShallowCopyable(f.FieldType));
                return this.shallowCopyableTypes[type] = result;
            }

            return this.shallowCopyableTypes[type] = false;
        }

        /// <summary>
        /// Returns true if the provided type is immutable, otherwise false.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the provided type is immutable, otherwise false.</returns>
        public bool IsImmutable(Type type)
        {
            if (this.immutableTypes.TryGetValue(type, out var result))
            {
                return result;
            }

            if (type.IsPrimitive || type.IsEnum)
            {
                return this.immutableTypes[type] = true;
            }

            if (type.GetCustomAttributes(typeof(ImmutableAttribute), false).Any())
            {
                return this.immutableTypes[type] = true;
            }

            if (type.IsPointer) return true;

            var handle = type.TypeHandle;
            if (handle.Equals(this.intPtrTypeHandle)) return true;
            if (handle.Equals(this.uIntPtrTypeHandle)) return true;
            if (this.delegateType.IsAssignableFrom(type)) return true;

            return false;
        }

        /// <summary>
        /// A comparer for <see cref="FieldInfo"/> which compares by name.
        /// </summary>
        private class FieldInfoComparer : IComparer<FieldInfo>
        {
            /// <summary>
            /// Gets the singleton instance of this class.
            /// </summary>
            public static FieldInfoComparer Instance { get; } = new FieldInfoComparer();

            /// <inheritdoc />
            public int Compare(FieldInfo x, FieldInfo y)
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
        }
    }
}