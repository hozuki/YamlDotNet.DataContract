using System;
using System.Reflection;

namespace YamlDotNet.Serialization {
    /// <summary>
    /// A simple container for <see cref="PropertyInfo"/> and <see cref="MemberInfo"/>, exposing some common methods
    /// for both types.
    /// </summary>
    internal sealed class PropertyOrField {

        public PropertyOrField(PropertyInfo property, string nameOverride, INamingConvention namingConvention)
            : this(nameOverride, namingConvention) {
            if (property == null) {
                throw new ArgumentNullException(nameof(property));
            }

            _propertyInfo = property;
            _fieldInfo = null;
            _memberType = MemberType.Property;
        }

        public PropertyOrField(FieldInfo field, string nameOverride, INamingConvention namingConvention)
            : this(nameOverride, namingConvention) {
            if (field == null) {
                throw new ArgumentNullException(nameof(field));
            }

            _propertyInfo = null;
            _fieldInfo = field;
            _memberType = MemberType.Field;
        }

        private PropertyOrField(string nameOverride, INamingConvention namingConvention) {
            _nameOverride = nameOverride;
            _namingConvention = namingConvention;
        }

        public void SetValue(object obj, object value) {
            switch (_memberType) {
                case MemberType.Property:
                    _propertyInfo.SetValue(obj, value);
                    break;
                case MemberType.Field:
                    _fieldInfo.SetValue(obj, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object GetValue(object obj) {
            switch (_memberType) {
                case MemberType.Property:
                    return _propertyInfo.GetValue(obj);
                case MemberType.Field:
                    return _fieldInfo.GetValue(obj);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Type GetMemberType() {
            switch (_memberType) {
                case MemberType.Property:
                    return _propertyInfo.PropertyType;
                case MemberType.Field:
                    return _fieldInfo.FieldType;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public T GetCustomAttribute<T>()
            where T : Attribute {
            switch (_memberType) {
                case MemberType.Property:
                    return _propertyInfo.GetCustomAttribute<T>(true);
                case MemberType.Field:
                    return _fieldInfo.GetCustomAttribute<T>(true);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool CanRead {
            get {
                switch (_memberType) {
                    case MemberType.Property:
                        return _propertyInfo.CanRead;
                    case MemberType.Field:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool CanWrite {
            get {
                switch (_memberType) {
                    case MemberType.Property:
                        return _propertyInfo.CanWrite;
                    case MemberType.Field:
                        return true;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public string Name {
            get {
                // The name can still be whitespaces and so on.
                // This is permitted in CLS, and YAML.
                if (!string.IsNullOrEmpty(_nameOverride)) {
                    return _nameOverride;
                }

                string name;

                switch (_memberType) {
                    case MemberType.Property:
                        name = _propertyInfo.Name;
                        break;
                    case MemberType.Field:
                        name = _fieldInfo.Name;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (_namingConvention != null) {
                    name = _namingConvention.Apply(name);
                }

                return name;
            }
        }

        private enum MemberType {

            Property = 0,
            Field = 1

        }

        private readonly MemberType _memberType;
        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;
        private readonly string _nameOverride;
        private readonly INamingConvention _namingConvention;

    }
}
