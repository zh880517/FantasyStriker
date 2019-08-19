using System.Collections.Generic;

namespace ECS
{
    public class Context
    {
        private Stack<IComponent>[] componentPools;
        protected int entityIndexKey;
        public ContextInfo Info { get; private set; }
        public int TotalComponents { get; private set; }
        public void Init(int totalComponents, ContextInfo contextInfo)
        {
            Info = contextInfo;
            componentPools = new Stack<IComponent>[totalComponents];
            TotalComponents = totalComponents;
        }
        

        public T CreateComponent<T>(Entity entity, int index) where T : class, IComponent, new()
        {
            var pool = GetPool(index);
            return pool.Count > 0 ? pool.Pop() as T : new T();
        }

        public void RemoveComponent(Entity entity, int index, IComponent component)
        {
            var pool = GetPool(index);
            pool.Push(component);
        }

        private Stack<IComponent> GetPool(int index)
        {
            var pool = componentPools[index];
            if (pool == null)
            {
                pool = new Stack<IComponent>();
                componentPools[index] = pool;
            }
            return pool;
        }
    }
}