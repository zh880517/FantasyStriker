using System;
using System.Collections.Generic;
using System.Reflection;

namespace ECSCore
{
    public static class PublicMemberInfoExtension
    {
        public static List<PublicMemberInfo> GetPublicMemberInfos(this Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            List<PublicMemberInfo> list = new List<PublicMemberInfo>(fields.Length + properties.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                list.Add(new PublicMemberInfo(fields[i]));
            }
            for (int j = 0; j < properties.Length; j++)
            {
                PropertyInfo propertyInfo = properties[j];
                if (propertyInfo.CanRead && propertyInfo.CanWrite && propertyInfo.GetIndexParameters().Length == 0)
                {
                    list.Add(new PublicMemberInfo(propertyInfo));
                }
            }
            return list;
        }

        public static object PublicMemberClone(this object obj)
        {
            object obj2 = Activator.CreateInstance(obj.GetType());
            obj.CopyPublicMemberValues(obj2);
            return obj2;
        }

        public static T PublicMemberClone<T>(this object obj) where T : new()
        {
            T t = Activator.CreateInstance<T>();
            obj.CopyPublicMemberValues(t);
            return t;
        }

        public static void CopyPublicMemberValues(this object source, object target)
        {
            List<PublicMemberInfo> publicMemberInfos = source.GetType().GetPublicMemberInfos();
            for (int i = 0; i < publicMemberInfos.Count; i++)
            {
                PublicMemberInfo publicMemberInfo = publicMemberInfos[i];
                publicMemberInfo.SetValue(target, publicMemberInfo.GetValue(source));
            }
        }
    }
}
