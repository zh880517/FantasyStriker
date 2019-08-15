using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ILAppAssembly : IAppAssembly
{
    private ILRuntime.Runtime.Enviorment.AppDomain appDomain;
    private MemoryStream dllStream;
    private MemoryStream pdbStream;

    public IStaticMethod GetStaticMethod(string typeName, string methodName, int paramCount)
    {
        return new ILStaticMethod(this.appDomain, typeName, methodName, paramCount);
    }

    public List<Type> GetTypes()
    {
        return appDomain.LoadedTypes.Values.Select(x => x.ReflectionType).ToList();
    }

    public void Load(byte[] assBytes, byte[] pdbBytes)
    {
        appDomain = new ILRuntime.Runtime.Enviorment.AppDomain();
        dllStream = new MemoryStream(assBytes);
        pdbStream = new MemoryStream(pdbBytes);
        appDomain.LoadAssembly(dllStream, pdbStream, new Mono.Cecil.Pdb.PdbReaderProvider());
        Debug.Log($"当前使用的是ILRuntime模式");
    }
    
    private void Int()
    {

    }
}
