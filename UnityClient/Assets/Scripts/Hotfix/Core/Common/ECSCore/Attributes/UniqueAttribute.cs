using System;

namespace ECSCore
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class UniqueAttribute : Attribute
    {
    }
}
