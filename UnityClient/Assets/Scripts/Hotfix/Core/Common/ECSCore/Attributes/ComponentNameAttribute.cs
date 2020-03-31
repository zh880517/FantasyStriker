using System;

namespace ECSCore
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class ComponentNameAttribute : Attribute
    {
        public readonly string Name;

        public ComponentNameAttribute(string componentName)
        {
            Name = componentName;
        }
    }
}
