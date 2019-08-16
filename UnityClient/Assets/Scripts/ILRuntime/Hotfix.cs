using UnityEngine;

public class Hotfix : MonoBehaviour
{
    private static Hotfix _Instance;
    public static IAppAssembly Assembly { get { return _Instance?.AppAssembly; } }
    private IAppAssembly AppAssembly;
    private IStaticMethod UpdateMethod;
    private IStaticMethod LateUpdateMethod;
    private IStaticMethod FixedUpdateMethod;
    private IStaticMethod OnApplicationQuitMethod;
    private IStaticMethod OnApplicationFocusMethod;
    private IStaticMethod OnApplicationPauseMethod;

    public static void Init(byte[] assBytes, byte[] pdbBytes)
    {
        if (_Instance == null)
        {
            GameObject go = new GameObject("_Hotfix_");
            DontDestroyOnLoad(go);
            _Instance = go.AddComponent<Hotfix>();
        }
#if ILRuntime
        _Instance.AppAssembly = new ILAppAssembly();
#else
        _Instance.AppAssembly = new MonoAppAssembly();
#endif
        _Instance.AppAssembly.Load(assBytes, pdbBytes);
        _Instance.RegistMethod();
    }

    private void RegistMethod()
    {
        UpdateMethod = AppAssembly.GetStaticMethod("HotfixApp", "Update", 0);
        LateUpdateMethod = AppAssembly.GetStaticMethod("HotfixApp", "LateUpdate", 0);
        FixedUpdateMethod = AppAssembly.GetStaticMethod("HotfixApp", "FixedUpdate", 0);
        OnApplicationQuitMethod = AppAssembly.GetStaticMethod("HotfixApp", "OnApplicationQuit", 0);
        OnApplicationFocusMethod = AppAssembly.GetStaticMethod("HotfixApp", "OnApplicationFocus", 1);
        OnApplicationPauseMethod = AppAssembly.GetStaticMethod("HotfixApp", "OnApplicationPause", 1);

        var initFunc = AppAssembly.GetStaticMethod("HotfixApp", "Init", 0);
        initFunc?.Run();
        if (initFunc == null)
        {
            Debug.LogError("HotfixApp 缺少 Init，Hotfix 模块的功能可能无法启用");
        }
    }
    
    private void Update()
    {
        UpdateMethod?.Run();
    }

    private void LateUpdate()
    {
        LateUpdateMethod?.Run();
    }

    private void FixedUpdate()
    {
        FixedUpdateMethod?.Run();
    }

    private void OnApplicationQuit()
    {
        OnApplicationQuitMethod?.Run();
    }

    private void OnApplicationFocus(bool focus)
    {
        OnApplicationFocusMethod?.Run(focus);
    }

    private void OnApplicationPause(bool pause)
    {
        OnApplicationPauseMethod?.Run(pause);
    }

}
