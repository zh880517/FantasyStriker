using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HotfixCopy
{
    private const string ScriptAssembliesDir = "Library/ScriptAssemblies";
    private const string CodeDir = "Assets/Resources/";
    private const string HotfixDll = "Hotfix.dll";
    private const string HotfixPdb = "Hotfix.pdb";
    static HotfixCopy()
    {
        var dllPath = Path.Combine(ScriptAssembliesDir, HotfixDll);
        if(File.Exists(dllPath))
        {
            File.Copy(dllPath, Path.Combine(CodeDir, "Hotfix.dll.bytes"), true);
            File.Copy(Path.Combine(ScriptAssembliesDir, HotfixPdb), Path.Combine(CodeDir, "Hotfix.pdb.bytes"), true);
            //Debug.Log($"复制Hotfix.dll, Hotfix.pdb到Assets/Resources完成");
            AssetDatabase.Refresh();
        }
    }
}
