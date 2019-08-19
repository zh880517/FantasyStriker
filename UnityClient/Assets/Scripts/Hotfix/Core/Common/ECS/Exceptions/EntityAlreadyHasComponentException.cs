namespace ECS
{
    public class EntityAlreadyHasComponentException : EntityException
    {
        public EntityAlreadyHasComponentException(int index, string message, string hint)
            : base(message + "\nEntity already has a component at index " + index + "!", hint)
        {
        }
    }
}
