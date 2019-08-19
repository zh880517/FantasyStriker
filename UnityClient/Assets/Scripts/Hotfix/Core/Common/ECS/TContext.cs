using System.Collections.Generic;

namespace ECS
{
    public class TContext<TEntity> : Context where TEntity: Entity , new()
    {
        private readonly HashSet<TEntity> entities = new HashSet<TEntity>(EntityEqualityComparer<TEntity>.comparer);
        private readonly Stack<TEntity> reusableEntities = new Stack<TEntity>();

        public TEntity CreateEntity()
        {
            TEntity entity;
            if (reusableEntities.Count > 0)
            {
                entity = reusableEntities.Pop();
                entity.Reactivate(++entityIndexKey);
            }
            else
            {
                entity = new TEntity();
                entity.Initialize(this, ++entityIndexKey);
            }
            entities.Add(entity);
            return entity;
        }
    }
}