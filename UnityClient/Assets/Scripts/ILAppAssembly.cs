using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ILAppAssembly : IAppAssembly
{
    private ILRuntime.Runtime.Enviorment.AppDomain appDomain;
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
        var dllStream = new MemoryStream(assBytes);
        var pdbStream = new MemoryStream(pdbBytes);
        appDomain.LoadAssembly(dllStream, pdbStream, new Mono.Cecil.Pdb.PdbReaderProvider());
    }
}
