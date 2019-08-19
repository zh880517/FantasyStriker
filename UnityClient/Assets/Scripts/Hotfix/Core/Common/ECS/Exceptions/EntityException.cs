using System;
namespace ECS
{
    public class EntityException : Exception
    {
        public EntityException(string message, string hint)
                : base(hint != null ? (message + "\n" + hint) : message)
        {
        }
    }
}