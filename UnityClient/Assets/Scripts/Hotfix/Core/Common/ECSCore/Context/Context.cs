using System;
using System.Collections.Generic;
using System.Linq;

namespace ECSCore
{
    /// A context manages the lifecycle of entities and groups.
    /// You can create and destroy entities and get groups of entities.
    /// The prefered way to create a context is to use the generated methods
    /// from the code generator, e.g. var context = new GameContext();
    public class Context<TEntity> : IContext<TEntity> where TEntity : class, IEntity
    {
        /// Occurs when an entity gets created.
        public event ContextEntityChanged OnEntityCreated;

        /// Occurs when an entity will be destroyed.
        public event ContextEntityChanged OnEntityWillBeDestroyed;

        /// Occurs when an entity got destroyed.
        public event ContextEntityChanged OnEntityDestroyed;

        /// Occurs when a group gets created for the first time.
        public event ContextGroupChanged OnGroupCreated;

        /// The total amount of components an entity can possibly have.
        /// This value is generated by the code generator,
        /// e.g ComponentLookup.TotalComponents.
        public int totalComponents { get; private set; }

        /// Returns all componentPools. componentPools is used to reuse
        /// removed components.
        /// Removed components will be pushed to the componentPool.
        /// Use entity.CreateComponent(index, type) to get a new or reusable
        /// component from the componentPool.
        public Stack<IComponent>[] componentPools { get; private set; }

        /// The contextInfo contains information about the context.
        /// It's used to provide better error messages.
        public ContextInfo contextInfo { get; private set; }

        /// Returns the number of entities in the context.
        public int count { get { return _entities.Count; } }

        /// Returns the number of entities in the internal ObjectPool
        /// for entities which can be reused.
        public int reusableEntitiesCount { get { return _reusableEntities.Count; } }

        /// Returns the number of entities that are currently retained by
        /// other objects (e.g. Group, Collector, ReactiveSystem).
        public int retainedEntitiesCount { get { return _retainedEntities.Count; } }
        
        readonly Func<IEntity, IAERC> _aercFactory;
        readonly Func<TEntity> _entityFactory;

        readonly HashSet<TEntity> _entities = new HashSet<TEntity>(EntityEqualityComparer<TEntity>.comparer);
        readonly Stack<TEntity> _reusableEntities = new Stack<TEntity>();
        readonly HashSet<TEntity> _retainedEntities = new HashSet<TEntity>(EntityEqualityComparer<TEntity>.comparer);

        readonly Dictionary<IMatcher<TEntity>, IGroup<TEntity>> _groups = new Dictionary<IMatcher<TEntity>, IGroup<TEntity>>();
        readonly List<IGroup<TEntity>>[] _groupsForIndex;
        readonly ObjectPool<List<GroupChanged<TEntity>>> _groupChangedListPool;

        readonly Dictionary<string, IEntityIndex> _entityIndices;

        int _creationIndex;

        TEntity[] _entitiesCache;

        // Cache delegates to avoid gc allocations
        readonly EntityComponentChanged _cachedEntityChanged;
        readonly EntityComponentReplaced _cachedComponentReplaced;
        readonly EntityEvent _cachedEntityReleased;
        readonly EntityEvent _cachedDestroyEntity;

        /// The prefered way to create a context is to use the generated methods
        /// from the code generator, e.g. var context = new GameContext();
        public Context(int totalComponents, Func<TEntity> entityFactory) : this(totalComponents, 0, null, null, entityFactory)
        {
        }

        /// The prefered way to create a context is to use the generated methods
        /// from the code generator, e.g. var context = new GameContext();
        public Context(int totalComponents, int startCreationIndex, ContextInfo contextInfo, Func<IEntity, IAERC> aercFactory, Func<TEntity> entityFactory)
        {
            this.totalComponents = totalComponents;
            _creationIndex = startCreationIndex;

            if (contextInfo != null)
            {
                this.contextInfo = contextInfo;
                if (contextInfo.componentNames.Length != totalComponents)
                {
                    throw new ContextInfoException(this, contextInfo);
                }
            }
            else
            {
                this.contextInfo = CreateDefaultContextInfo();
            }

            _aercFactory = aercFactory ?? (entity => new SafeAERC(entity));
            _entityFactory = entityFactory;

            _groupsForIndex = new List<IGroup<TEntity>>[totalComponents];
            componentPools = new Stack<IComponent>[totalComponents];
            _entityIndices = new Dictionary<string, IEntityIndex>();
            _groupChangedListPool = new ObjectPool<List<GroupChanged<TEntity>>>(
                () => new List<GroupChanged<TEntity>>(),
                list => list.Clear()
            );

            // Cache delegates to avoid gc allocations
            _cachedEntityChanged = updateGroupsComponentAddedOrRemoved;
            _cachedComponentReplaced = updateGroupsComponentReplaced;
            _cachedEntityReleased = onEntityReleased;
            _cachedDestroyEntity = onDestroyEntity;
        }

        ContextInfo CreateDefaultContextInfo()
        {
            var componentNames = new string[totalComponents];
            const string prefix = "Index ";
            for (int i = 0; i < componentNames.Length; i++)
            {
                componentNames[i] = prefix + i;
            }

            return new ContextInfo("Unnamed Context", componentNames, null);
        }

        /// Creates a new entity or gets a reusable entity from the
        /// internal ObjectPool for entities.
        public TEntity CreateEntity()
        {
            TEntity entity;

            if (_reusableEntities.Count > 0)
            {
                entity = _reusableEntities.Pop();
                entity.Reactivate(_creationIndex++);
            }
            else
            {
                entity = _entityFactory();
                entity.Initialize(_creationIndex++, totalComponents, componentPools, contextInfo, _aercFactory(entity));
            }

            _entities.Add(entity);
            entity.Retain(this);
            _entitiesCache = null;

            entity.OnComponentAdded += _cachedEntityChanged;
            entity.OnComponentRemoved += _cachedEntityChanged;
            entity.OnComponentReplaced += _cachedComponentReplaced;
            entity.OnEntityReleased += _cachedEntityReleased;
            entity.OnDestroyEntity += _cachedDestroyEntity;

            OnEntityCreated?.Invoke(this, entity);

            return entity;
        }

        /// Destroys all entities in the context.
        /// Throws an exception if there are still retained entities.
        public void DestroyAllEntities()
        {
            var entities = GetEntities();
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i].Destroy();
            }

            _entities.Clear();

            if (_retainedEntities.Count != 0)
            {
                throw new ContextStillHasRetainedEntitiesException(this, _retainedEntities.ToArray());
            }
        }

        /// Determines whether the context has the specified entity.
        public bool HasEntity(TEntity entity)
        {
            return _entities.Contains(entity);
        }

        /// Returns all entities which are currently in the context.
        public TEntity[] GetEntities()
        {
            if (_entitiesCache == null)
            {
                _entitiesCache = new TEntity[_entities.Count];
                _entities.CopyTo(_entitiesCache);
            }

            return _entitiesCache;
        }

        /// Returns a group for the specified matcher.
        /// Calling context.GetGroup(matcher) with the same matcher will always
        /// return the same instance of the group.
        public IGroup<TEntity> GetGroup(IMatcher<TEntity> matcher)
        {
            if (!_groups.TryGetValue(matcher, out IGroup<TEntity> group))
            {
                group = new Group<TEntity>(matcher);
                var entities = GetEntities();
                for (int i = 0; i < entities.Length; i++)
                {
                    group.HandleEntitySilently(entities[i]);
                }

                _groups.Add(matcher, group);

                for (int i = 0; i < matcher.indices.Length; i++)
                {
                    var index = matcher.indices[i];
                    if (_groupsForIndex[index] == null)
                    {
                        _groupsForIndex[index] = new List<IGroup<TEntity>>();
                    }

                    _groupsForIndex[index].Add(group);
                }

                OnGroupCreated?.Invoke(this, group);
            }

            return group;
        }

        /// Adds the IEntityIndex for the specified name.
        /// There can only be one IEntityIndex per name.
        public void AddEntityIndex(IEntityIndex entityIndex)
        {
            if (_entityIndices.ContainsKey(entityIndex.name))
            {
                throw new ContextEntityIndexDoesAlreadyExistException(this, entityIndex.name);
            }

            _entityIndices.Add(entityIndex.name, entityIndex);
        }

        /// Gets the IEntityIndex for the specified name.
        public IEntityIndex GetEntityIndex(string name)
        {
            if (!_entityIndices.TryGetValue(name, out IEntityIndex entityIndex))
            {
                throw new ContextEntityIndexDoesNotExistException(this, name);
            }

            return entityIndex;
        }

        /// Resets the creationIndex back to 0.
        public void ResetCreationIndex()
        {
            _creationIndex = 0;
        }

        /// Clears the componentPool at the specified index.
        public void ClearComponentPool(int index)
        {
            var componentPool = componentPools[index];
            if (componentPool != null)
            {
                componentPool.Clear();
            }
        }

        /// Clears all componentPools.
        public void ClearComponentPools()
        {
            for (int i = 0; i < componentPools.Length; i++)
            {
                ClearComponentPool(i);
            }
        }

        /// Resets the context (destroys all entities and
        /// resets creationIndex back to 0).
        public void Reset()
        {
            DestroyAllEntities();
            ResetCreationIndex();
        }

        /// Removes all event handlers
        /// OnEntityCreated, OnEntityWillBeDestroyed,
        /// OnEntityDestroyed and OnGroupCreated
        public void RemoveAllEventHandlers()
        {
            OnEntityCreated = null;
            OnEntityWillBeDestroyed = null;
            OnEntityDestroyed = null;
            OnGroupCreated = null;
        }

        public override string ToString()
        {
            return contextInfo.name;
        }

        void updateGroupsComponentAddedOrRemoved(IEntity entity, int index, IComponent component)
        {
            var groups = _groupsForIndex[index];
            if (groups != null)
            {
                var events = _groupChangedListPool.Get();

                var tEntity = (TEntity)entity;

                for (int i = 0; i < groups.Count; i++)
                {
                    events.Add(groups[i].HandleEntity(tEntity));
                }

                for (int i = 0; i < events.Count; i++)
                {
                    events[i]?.Invoke(
                            groups[i], tEntity, index, component
                        );
                }

                _groupChangedListPool.Push(events);
            }
        }

        void updateGroupsComponentReplaced(IEntity entity, int index, IComponent previousComponent, IComponent newComponent)
        {
            var groups = _groupsForIndex[index];
            if (groups != null)
            {
                var tEntity = (TEntity)entity;

                for (int i = 0; i < groups.Count; i++)
                {
                    groups[i].UpdateEntity(
                        tEntity, index, previousComponent, newComponent
                    );
                }
            }
        }

        void onEntityReleased(IEntity entity)
        {
            if (entity.isEnabled)
            {
                throw new EntityIsNotDestroyedException(
                    "Cannot release " + entity + "!"
                );
            }

            var tEntity = (TEntity)entity;
            entity.RemoveAllOnEntityReleasedHandlers();
            _retainedEntities.Remove(tEntity);
            _reusableEntities.Push(tEntity);
        }

        void onDestroyEntity(IEntity entity)
        {
            var tEntity = (TEntity)entity;
            var removed = _entities.Remove(tEntity);
            if (!removed)
            {
                throw new ContextDoesNotContainEntityException(
                    "'" + this + "' cannot destroy " + tEntity + "!",
                    "This cannot happen!?!"
                );
            }

            _entitiesCache = null;

            if (OnEntityWillBeDestroyed != null)
            {
                OnEntityWillBeDestroyed(this, tEntity);
            }

            tEntity.InternalDestroy();

            OnEntityDestroyed?.Invoke(this, tEntity);

            if (tEntity.retainCount == 1)
            {
                // Can be released immediately without
                // adding to _retainedEntities
                tEntity.OnEntityReleased -= _cachedEntityReleased;
                _reusableEntities.Push(tEntity);
                tEntity.Release(this);
                tEntity.RemoveAllOnEntityReleasedHandlers();
            }
            else
            {
                _retainedEntities.Add(tEntity);
                tEntity.Release(this);
            }
        }
    }
}
