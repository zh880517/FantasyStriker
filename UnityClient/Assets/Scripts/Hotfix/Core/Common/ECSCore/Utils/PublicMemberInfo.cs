using System;
using System.Reflection;

namespace ECSCore
{
    public class PublicMemberInfo
    {
        public readonly Type type;

        public readonly string name;

        public readonly AttributeInfo[] attributes;

        private readonly FieldInfo _fieldInfo;

        private readonly PropertyInfo _propertyInfo;

        public PublicMemberInfo(FieldInfo info)
        {
            _fieldInfo = info;
            type = _fieldInfo.FieldType;
            name = _fieldInfo.Name;
            attributes = getAttributes(_fieldInfo.GetCustomAttributes(false));
        }

        public PublicMemberInfo(PropertyInfo info)
        {
            _propertyInfo = info;
            type = _propertyInfo.PropertyType;
            name = _propertyInfo.Name;
            attributes = getAttributes(_propertyInfo.GetCustomAttributes(false));
        }

        public PublicMemberInfo(Type type, string name, AttributeInfo[] attributes = null)
        {
            this.type = type;
            this.name = name;
            this.attributes = attributes;
        }

        public object GetValue(object obj)
        {
            if (_fieldInfo == null)
            {
                return _propertyInfo.GetValue(obj, null);
            }
            return _fieldInfo.GetValue(obj);
        }

        public void SetValue(object obj, object value)
        {
            if (_fieldInfo != null)
            {
                _fieldInfo.SetValue(obj, value);
                return;
            }
            _propertyInfo.SetValue(obj, value, null);
        }

        private static AttributeInfo[] getAttributes(object[] attributes)
        {
            AttributeInfo[] array = new AttributeInfo[attributes.Length];
            for (int i = 0; i < attributes.Length; i++)
            {
                object obj = attributes[i];
                array[i] = new AttributeInfo(obj, obj.GetType().GetPublicMemberInfos());
            }
            return array;
        }
    }
}
