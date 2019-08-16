using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class MonoAppAssembly : IAppAssembly
{

    private Assembly assembly;

    public IStaticMethod GetStaticMethod(string typeName, string methodName, int paramCount)
    {
        Type type = assembly.GetType(typeName);
        if (type != null)
        {
            var method = type.GetMethod(methodName);
            if (method != null)
                return new MonoStaticMethod(method);
        }
        return null;
    }

    public List<Type> GetTypes()
    {
        return assembly.GetTypes().ToList();
    }

    public void Load(byte[] assBytes, byte[] pdbBytes)
    {
        assembly = Assembly.Load(assBytes, pdbBytes);
        UnityEngine.Debug.Log($"当前使用的是Mono模式");
    }
}
