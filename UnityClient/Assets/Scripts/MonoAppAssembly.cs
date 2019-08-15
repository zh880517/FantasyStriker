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
        return new MonoStaticMethod(type, methodName);
    }

    public List<Type> GetTypes()
    {
        return assembly.GetTypes().ToList();
    }

    public void Load(byte[] assBytes, byte[] pdbBytes)
    {
        assembly = Assembly.Load(assBytes, pdbBytes);
    }
}
