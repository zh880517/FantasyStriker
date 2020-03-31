using System;
using System.Collections.Generic;
using System.Reflection;
using ECSCore;

namespace ECSGenerator
{
    public class ComonentInfo
    {
        public enum EntityIndexType
        {
            None,
            Index,
            PrimaryIndex,
        }
        public class Field
        {
            public string TypeName;
            public string Name;
            public EntityIndexType IndexType = EntityIndexType.None;
        }
        public string ShowName;
        public string FullName;
        public bool IsUnique;
        public List<Field> Fields = new List<Field>();
        public int Id = -1;
        
        public static ComonentInfo FromType(Type type)
        {
            if (type.GetCustomAttribute<DontGenerateAttribute>() != null)
                return null;
            ComonentInfo info = new ComonentInfo();
            ComponentNameAttribute componentName = type.GetCustomAttribute<ComponentNameAttribute>();
            if (componentName != null)
            {
                info.ShowName = componentName.Name;
            }
            else
            {
                info.ShowName = type.Name.Replace("Component", "");
            }
            info.FullName = type.FullName;
            if (type.GetCustomAttribute<UniqueAttribute>() != null)
            {
                info.IsUnique = true;
            }
            var fields = type.GetFields();
            foreach (var filed in fields)
            {
                Field newField = new Field
                {
                    Name = filed.Name,
                    TypeName = TypeNameHelper.ToCompilableString(filed.FieldType)
                };
                if (filed.GetCustomAttribute<PrimaryEntityIndexAttribute>() != null)
                {
                    newField.IndexType = EntityIndexType.PrimaryIndex;
                }
                else if (filed.GetCustomAttribute<EntityIndexAttribute>() != null)
                {
                    newField.IndexType = EntityIndexType.Index;
                }
                info.Fields.Add(newField);
            }
            return info;
        }

        public static string GetTypeFullName(Type type)
        {
            if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(double))
            {
                return "double";
            }
            else if (type == typeof(long))
            {
                return "long";
            }
            else if (type == typeof(ulong))
            {
                return "ulong";
            }
            else if (type == typeof(int))
            {
                return "int";
            }
            else if (type == typeof(uint))
            {
                return "uint";
            }
            else if (type == typeof(short))
            {
                return "short";
            }
            else if (type == typeof(ushort))
            {
                return "ushort";
            }
            else if (type == typeof(char))
            {
                return "char";
            }
            else if (type == typeof(byte))
            {
                return "byte";
            }
            else if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(bool))
            {
                return "bool";
            }
            return type.FullName;
        }
        
    }
}
