namespace ECS
{
    public class EntityIsNotEnabledException : EntityException
    {
        public EntityIsNotEnabledException(string message)
            : base(message + "\nEntity is not enabled!",
                "The entity has already been destroyed. You cannot modify destroyed entities.")
        {
        }
    }
}
