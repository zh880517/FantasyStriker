using System;

namespace ECSCore
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class DontGenerateAttribute : Attribute
    {
        public readonly bool generateIndex;

        public DontGenerateAttribute(bool generateIndex = true)
        {
            this.generateIndex = generateIndex;
        }
    }
}
