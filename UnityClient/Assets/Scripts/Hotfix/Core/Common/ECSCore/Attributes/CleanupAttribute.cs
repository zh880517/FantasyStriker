using System;

namespace ECSCore
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class CleanupAttribute : Attribute
    {
        public readonly CleanupMode cleanupMode;

        public CleanupAttribute(CleanupMode cleanupMode)
        {
            this.cleanupMode = cleanupMode;
        }
    }

    public enum CleanupMode
    {
        RemoveComponent,
        DestroyEntity
    }
}
