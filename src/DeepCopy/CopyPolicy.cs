using System;
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
        private enum Policy
        {
            Mutable,
            ShallowCopyable,
            Immutable
        }

        private readonly CachedReadConcurrentDictionary<Type, Policy> policies = new CachedReadConcurrentDictionary<Type, Policy>();
        private readonly RuntimeTypeHandle intPtrTypeHandle = typeof(IntPtr).TypeHandle;
        private readonly RuntimeTypeHandle uIntPtrTypeHandle = typeof(UIntPtr).TypeHandle;
        private readonly Type delegateType = typeof(Delegate);

        public CopyPolicy()
        {
            this.policies[typeof(decimal)] = Policy.Immutable;
            this.policies[typeof(DateTime)] = Policy.Immutable;
            this.policies[typeof(TimeSpan)] = Policy.Immutable;
            this.policies[typeof(string)] = Policy.Immutable;
            this.policies[typeof(Guid)] = Policy.Immutable;
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
                while (current != typeof(object) && current != null)
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
            return this.GetPolicy(type) != Policy.Mutable;
        }

        /// <summary>
        /// Returns true if the provided type is immutable, otherwise false.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the provided type is immutable, otherwise false.</returns>
        public bool IsImmutable(Type type)
        {
            return this.GetPolicy(type) == Policy.Immutable;
        }

        private Policy GetPolicy(Type type)
        {
            if (this.policies.TryGetValue(type, out var result))
            {
                return result;
            }

            if (type.IsPrimitive || type.IsEnum)
            {
                return this.policies[type] = Policy.Immutable;
            }

            if (type.GetCustomAttributes(typeof(ImmutableAttribute), false).Any())
            {
                return this.policies[type] = Policy.Immutable;
            }

            if (type.IsPointer) return Policy.Immutable;

            var handle = type.TypeHandle;
            if (handle.Equals(this.intPtrTypeHandle)) return Policy.Immutable;
            if (handle.Equals(this.uIntPtrTypeHandle)) return Policy.Immutable;
            if (this.delegateType.IsAssignableFrom(type)) return Policy.Immutable;
            
            if (type.IsValueType && !type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                if (this.GetCopyableFields(type).All(f => f.FieldType != type && this.IsShallowCopyable(f.FieldType)))
                {
                    return this.policies[type] = Policy.ShallowCopyable;
                }
            }
            
            return this.policies[type] = Policy.Mutable;
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