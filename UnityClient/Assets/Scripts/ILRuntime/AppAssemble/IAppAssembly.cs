using System.Collections.Generic;

public interface IAppAssembly
{
    void Load(byte[] assBytes, byte[] pdbBytes);
    IStaticMethod GetStaticMethod(string typeName, string methodName, int paramCount);
    List<System.Type> GetTypes();   
}
