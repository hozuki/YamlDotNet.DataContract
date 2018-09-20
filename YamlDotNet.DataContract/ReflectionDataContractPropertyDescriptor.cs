using System;
using YamlDotNet.Core;

namespace YamlDotNet.Serialization {
    internal sealed class ReflectionDataContractPropertyDescriptor : IPropertyDescriptor {

        public ReflectionDataContractPropertyDescriptor(PropertyOrField propertyOrField, ITypeResolver typeResolver) {
            _propertyOrField = propertyOrField ?? throw new ArgumentNullException(nameof(propertyOrField));
            _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        }

        public T GetCustomAttribute<T>() where T : Attribute {
            return _propertyOrField.GetCustomAttribute<T>();
        }

        public IObjectDescriptor Read(object target) {
            var value = _propertyOrField.GetValue(target);
            var actualType = TypeOverride ?? _typeResolver.Resolve(Type, value);
            return new ObjectDescriptor(value, actualType, Type, ScalarStyle);
        }

        public void Write(object target, object value) {
            _propertyOrField.SetValue(target, value);
        }

        public string Name => _propertyOrField.Name;

        public bool CanWrite => _propertyOrField.CanWrite;

        public Type Type => _propertyOrField.GetMemberType();

        public Type TypeOverride { get; set; }

        public int Order { get; set; }

        public ScalarStyle ScalarStyle { get; set; }

        private readonly PropertyOrField _propertyOrField;
        private readonly ITypeResolver _typeResolver;

    }
}
