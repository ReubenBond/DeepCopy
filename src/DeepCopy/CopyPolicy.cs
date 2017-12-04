using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

namespace DeepCopy
{
    /// <summary>
    /// Methods for determining the copyability of types and fields.
    /// </summary>
    internal sealed class CopyPolicy
    {
        private enum Policy
        {
            Tracking,
            Mutable,
            Immutable
        }

        private readonly ConcurrentDictionary<Type, Policy> policies = new ConcurrentDictionary<Type, Policy>();
        private readonly RuntimeTypeHandle intPtrTypeHandle = typeof(IntPtr).TypeHandle;
        private readonly RuntimeTypeHandle uIntPtrTypeHandle = typeof(UIntPtr).TypeHandle;
        private readonly Type delegateType = typeof(Delegate);

        public CopyPolicy()
        {
            this.policies[typeof(object)] = Policy.Tracking; // we need to track
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
        /// Returns true if the provided type is immutable, otherwise false.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the provided type is immutable, otherwise false.</returns>
        public bool IsImmutable(Type type)
        {
            return this.GetPolicy(type) == Policy.Immutable;
        }

        public bool NeedsTracking(Type type)
        {
            var policy = GetPolicy(type);
            // we found something mutable now we need to check if it needs tracking
            if (policy == Policy.Mutable)
            {                
                var copyableFields = GetCopyableFields(type);
                var queue = new Queue<FieldInfo>(copyableFields);
                var duplicateCheck = new HashSet<Type>(AssignableFromEqualityComparer.Instance) {type}; // add root
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var fieldType = current.FieldType;
                    var fieldPolicy = GetPolicy(fieldType);
                    if(fieldPolicy == Policy.Immutable)
                    {
                        continue;
                    }
                    if (fieldPolicy == Policy.Tracking)
                    {
                        this.policies[type] = Policy.Tracking;
                        return true;
                    }
                    if (duplicateCheck.Add(fieldType))
                    {
                        if (typeof(IEnumerable).IsAssignableFrom(fieldType)) // Rule 5: enumerable mutable fields need tracking
                        {
                            this.policies[type] = Policy.Tracking;
                            return true;
                        }
                        var fieldFields = GetCopyableFields(fieldType);
                        foreach (var fieldField in fieldFields)
                        {
                            queue.Enqueue(fieldField); // Rule 6: Recursive
                        }
                    }
                    else
                    {
                        this.policies[type] = Policy.Tracking; // Rule 4: one or more mutable fields of the same type
                        return true;
                    }
                }

            }
            return this.GetPolicy(type) == Policy.Tracking;
        }

        private Policy GetPolicy(Type type)
        {
            if (this.policies.TryGetValue(type, out var result))
            {
                return result;
            }

            if (type.GetCustomAttribute<ImmutableAttribute>(false) != null)
            {
                return this.policies[type] = Policy.Immutable;
            }

            // Rule 1: primitives and quasi primitves
            if (type.IsPrimitive || type.IsEnum || type.IsPointer || type == typeof(string))
            {
                return this.policies[type] = Policy.Immutable;
            }

            if (type.IsInterface || type.IsAbstract)
            {
                return this.policies[type] = Policy.Mutable;
            }

            if (type.IsArray)
            {
                return this.policies[type] = Policy.Mutable;
            }
            // Rule 1,2
            if (type.IsValueType)
            {
                var copyableFields = GetCopyableFields(type);
                foreach (var copyableField in copyableFields)
                {
                    var fieldType = copyableField.FieldType;
                    if (type == fieldType || GetPolicy(fieldType) != Policy.Immutable)
                    {
                        return this.policies[type] = Policy.Mutable;
                    }
                }
            }
            // Rule 3
            else if (type.IsClass)
            {
                var copyableFields = GetCopyableFields(type);
                foreach (var copyableField in copyableFields)
                {
                    if (!copyableField.IsInitOnly)
                    {
                        return this.policies[type] = Policy.Mutable;
                    }
                    var fieldType = copyableField.FieldType;
                    if (AssignableFromEqualityComparer.Instance.Equals(type, fieldType) || GetPolicy(fieldType) != Policy.Immutable)
                    {
                        return this.policies[type] = Policy.Mutable;
                    }
                }
            }
            return this.policies[type] = Policy.Immutable;
        }

        /// <summary>
        /// A comparer for <see cref="FieldInfo"/> which compares by name.
        /// </summary>
        private sealed class FieldInfoComparer : IComparer<FieldInfo>
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

        private sealed class AssignableFromEqualityComparer : IEqualityComparer<Type>
        {
            public static AssignableFromEqualityComparer Instance { get; } = new AssignableFromEqualityComparer();
            private static readonly Type ObjectType = typeof(object);
            public bool Equals(Type x, Type y)
            {
                // We can't reason about object
                if (x == ObjectType || y == ObjectType)
                {
                    return false;
                }
                return x.IsAssignableFrom(y) || y.IsAssignableFrom(x);
            }

            public int GetHashCode(Type obj)
            {
                return 0;
            }
        }

    }
}