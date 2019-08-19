namespace ECS
{
    public class Entity
    {
        private IComponent[] components;
        public Context Context { get; private set; }
        public ContextInfo ContextInfo { get { return Context.Info; } }
        public int CreatIndex { get; private set; }

        public bool Enable { get; private set; }

        public void Initialize(Context context, int index)
        {
            Context = context;
            CreatIndex = index;
            components = new IComponent[context.TotalComponents];
            Enable = true;
        }

        public T AddComponent<T>(int index) where T : class, IComponent, new()
        {
            if (!Enable)
            {
                throw new EntityIsNotEnabledException(
                    "Cannot add component '" +
                    ContextInfo.componentNames[index] + "' to " + CreatIndex + "!"
                );
            }
            if (HasComponent(index))
            {
                throw new EntityAlreadyHasComponentException(
                    index, "Cannot add component '" +
                           ContextInfo.componentNames[index] + "' to " + CreatIndex + "!",
                    "You should check if an entity already has the component " +
                    "before adding it or use entity.ReplaceComponent()."
                );
            }
            T component = Context.CreateComponent<T>(this, index);
            components[index] = component;
            return component;
        }

        public void RemoveComponent(int index)
        {
            if (!Enable)
            {
                throw new EntityIsNotEnabledException(
                    "Cannot remove component '" +
                    ContextInfo.componentNames[index] + "' from " + this + "!"
                );
            }
            if (!HasComponent(index))
            {
                throw new EntityDoesNotHaveComponentException(
                    index, "Cannot remove component '" +
                           ContextInfo.componentNames[index] + "' from " + this + "!",
                    "You should check if an entity has the component " +
                    "before removing it."
                );
            }
            var component = components[index];
            components[index] = null;
            Context.RemoveComponent(this, index, component);
        }

        public IComponent GetComponent(int index)
        {
            if (!HasComponent(index))
            {
                throw new EntityDoesNotHaveComponentException(
                    index, "Cannot get component '" +
                           ContextInfo.componentNames[index] + "' from entity " + CreatIndex + "!",
                    "You should check if an entity has the component " + "before getting it."
                );
            }

            return components[index];
        }

        public bool HasComponent(int index)
        {
            return components[index] != null;
        }

        public void Reactivate(int creationIndex)
        {
            CreatIndex = creationIndex;
            Enable = true;
        }
    }
}