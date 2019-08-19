namespace ECS
{
    public class EntityDoesNotHaveComponentException : EntityException
    {
        public EntityDoesNotHaveComponentException(int index, string message, string hint)
            : base(message + "\nEntity does not have a component at index " + index + "!", hint)
        {
        }
    }
}