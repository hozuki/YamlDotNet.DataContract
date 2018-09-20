using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using YamlDotNet.Serialization.TypeResolvers;

namespace YamlDotNet.Serialization.TypeInspectors {
    /// <inheritdoc />
    /// <summary>
    /// An <see cref="ITypeInspector"/> that considers <see cref="DataMemberAttribute"/> and <see cref="IgnoreDataMemberAttribute"/>
    /// on properties and fields. This inspector ignores <see cref="YamlMemberAttribute"/>.
    /// </summary>
    public class DataContractTypeInspector : ITypeInspector {

        /// <summary>
        /// Creates a new <see cref="DataContractTypeInspector"/> instance with a <see cref="StaticTypeResolver"/>.
        /// </summary>
        /// <param name="baseInspector">Previous <see cref="ITypeInspector"/>.</param>
        public DataContractTypeInspector(ITypeInspector baseInspector)
            : this(baseInspector, new StaticTypeResolver()) {
        }

        /// <summary>
        /// Creates a new <see cref="DataContractTypeInspector"/> instance with a custom <see cref="ITypeResolver"/>.
        /// </summary>
        /// <param name="baseInspector">Previous <see cref="ITypeInspector"/>.</param>
        /// <param name="typeResolver">The custom <see cref="ITypeResolver"/>.</param>
        public DataContractTypeInspector(ITypeInspector baseInspector, ITypeResolver typeResolver) {
            _baseInspector = baseInspector ?? throw new ArgumentNullException(nameof(baseInspector));
            _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        }

        /// <summary>
        /// Gets or sets the way searching for properties and fields.
        /// </summary>
        public DataMemberSerialization DataMemberSerialization { get; set; } = DataMemberSerialization.OptIn;

        /// <summary>
        /// Gets or sets whether the non-public members should be included in properties and fields searching.
        /// </summary>
        public bool IncludeNonPublicMembers { get; set; }

        /// <summary>
        /// Gets or sets the naming convention.
        /// Note that <see cref="BuilderSkeleton{TBuilder}.WithNamingConvention"/> does not work with <see cref="DataContractTypeInspector"/>,
        /// so when setting the naming convention you must use this property.
        /// </summary>
        public INamingConvention NamingConvention { get; set; }

        /// <summary>
        /// Gets or sets whether results of <see cref="GetProperties"/> calls are cached.
        /// Set to <see langword="true"/> (the default setting) to improve performance. When setting to <see langword="false"/>,
        /// the contents of the cache is cleared.
        /// </summary>
        public bool CacheResults {
            get => _cacheResults;
            set {
                if (!value) {
                    _cachedProperties.Clear();
                }

                _cacheResults = value;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Get an <see cref="IPropertyDescriptor"/> for a named property on a type.
        /// </summary>
        /// <param name="type">The entity type.</param>
        /// <param name="container">Temporary object whose properties will be set.</param>
        /// <param name="name">The property name.</param>
        /// <param name="ignoreUnmatched">Whether unmatched (additional/missing) properties should be ignored.</param>
        /// <returns>The <see cref="IPropertyDescriptor"/> for the property.</returns>
        public IPropertyDescriptor GetProperty(Type type, object container, string name, bool ignoreUnmatched) {
            IReadOnlyList<IPropertyDescriptor> propertyList;

            if (CacheResults) {
                if (!_cachedProperties.ContainsKey(type)) {
                    // Well we know we return an array.
                    propertyList = (IReadOnlyList<IPropertyDescriptor>)GetProperties(type, container);
                    _cachedProperties[type] = propertyList;
                } else {
                    propertyList = _cachedProperties[type];
                }
            } else {
                propertyList = (IReadOnlyList<IPropertyDescriptor>)GetProperties(type, container);
            }

            var filteredProperties = propertyList.Where(p => p.Name == name).ToArray();

            if (filteredProperties.Length == 0) {
                if (ignoreUnmatched) {
                    return null;
                } else {
                    throw new SerializationException($"{type} does not have required property/field \"{name}\".");
                }
            } else if (filteredProperties.Length > 1) {
                throw new SerializationException($"There are multiple properties/fields on {type} that have the name \"{name}\".");
            }

            return filteredProperties[0];
        }

        /// <inheritdoc />
        /// <summary>
        /// Get enumerable object of <see cref="IPropertyDescriptor"/> for a type.
        /// </summary>
        /// <param name="type">The entity type.</param>
        /// <param name="container">Temporary object whose properties will be set.</param>
        /// <returns>The enumerable object.</returns>
        public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container) {
            // First, check DataContract attribute.
            if (type.GetCustomAttribute<DataContractAttribute>(true) == null) {
                return Enumerable.Empty<IPropertyDescriptor>();
            }

            var result = new List<IPropertyDescriptor>();
            var bindingFlags = IncludeNonPublicMembers ? NonPublicInstance : PublicInstance;

            // Search for writable properties.
            var properties = type.GetProperties(bindingFlags);

            foreach (var property in properties) {
                if (!IsValidProperty(property)) {
                    continue;
                }

                var memberAttr = property.GetCustomAttribute<DataMemberAttribute>(true);
                var ignoreAttr = property.GetCustomAttribute<IgnoreDataMemberAttribute>(true);

                switch (DataMemberSerialization) {
                    case DataMemberSerialization.OptIn:
                        if (memberAttr == null) {
                            continue;
                        }
                        break;
                    case DataMemberSerialization.OptOut:
                        if (ignoreAttr != null) {
                            continue;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var nameOverride = memberAttr?.Name;
                var p = new PropertyOrField(property, nameOverride, NamingConvention);
                var descriptor = new ReflectionDataContractPropertyDescriptor(p, _typeResolver);

                result.Add(descriptor);
            }

            // Search for fields, but no need for read/write check (because fields are always writable,
            // even with ReadOnlyAttribute.
            var fields = type.GetFields(bindingFlags);

            foreach (var field in fields) {
                var memberAttr = field.GetCustomAttribute<DataMemberAttribute>(true);
                var ignoreAttr = field.GetCustomAttribute<IgnoreDataMemberAttribute>(true);

                switch (DataMemberSerialization) {
                    case DataMemberSerialization.OptIn:
                        if (memberAttr == null) {
                            continue;
                        }
                        break;
                    case DataMemberSerialization.OptOut:
                        if (ignoreAttr != null) {
                            continue;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var nameOverride = memberAttr?.Name;
                var p = new PropertyOrField(field, nameOverride, NamingConvention);
                var descriptor = new ReflectionDataContractPropertyDescriptor(p, _typeResolver);

                result.Add(descriptor);
            }

            return result.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidProperty(PropertyInfo property) {
            return property.CanRead && property.GetGetMethod().GetParameters().Length == 0;
        }

        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly Dictionary<Type, IReadOnlyList<IPropertyDescriptor>> _cachedProperties = new Dictionary<Type, IReadOnlyList<IPropertyDescriptor>>();

        private bool _cacheResults = true;
        private readonly ITypeInspector _baseInspector;
        private readonly ITypeResolver _typeResolver;

    }
}
