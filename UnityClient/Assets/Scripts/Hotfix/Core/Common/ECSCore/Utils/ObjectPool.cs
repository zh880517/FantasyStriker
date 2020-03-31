using System;
using System.Collections.Generic;
using System.Text;

namespace ECSCore
{
    public class ObjectPool<T>
    {
        private readonly Func<T> _factoryMethod;
        private readonly Action<T> _resetMethod;
        private readonly Stack<T> _objectPool;

        public ObjectPool(Func<T> factoryMethod, Action<T> resetMethod = null)
        {
            _factoryMethod = factoryMethod;
            _resetMethod = resetMethod;
            _objectPool = new Stack<T>();
        }

        public T Get()
        {
            if (_objectPool.Count != 0)
            {
                return _objectPool.Pop();
            }
            return _factoryMethod();
        }

        public void Push(T obj)
        {
            _resetMethod?.Invoke(obj);
            _objectPool.Push(obj);
        }
    }
}
