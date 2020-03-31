using ECSGenerator;

public static class UnityGenHelper
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/ECS/generator")]
    public static void Gen()
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        Generator generator = new Generator(
            "Assets/Scripts/Hotfix/Core/Common/ECS/Generated/",
            "Assets/Scripts/Hotfix/Core/Client/ECS/Generated/",
            assemblies);
        generator.Gen();
        UnityEditor.AssetDatabase.Refresh();
    }
#endif
}
