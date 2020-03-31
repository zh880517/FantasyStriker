namespace ECSGenerator
{
    public static class LookupGenerator
    {
        public static void Gen(ComponentList components, FileGenerator fileGen, bool isView)
        {
            if (isView)
            {
                fileGen.AddLine("public static class ViewComponentsLookup");
            }
            else
            {
                fileGen.AddLine("public static class GameComponentsLookup");
            }

            using (new FileGenerator.Scop(fileGen))
            {
                if (isView)
                {
                    foreach (var component in components.ViewComponents)
                    {
                        if (!component.IsUnique)
                        {
                            fileGen.AddFormat("public const int {0} = {1};", component.ShowName, component.Id);
                        }
                    }
                }
                else
                {
                    foreach (var component in components.GameComponents)
                    {
                        if (!component.IsUnique)
                        {
                            fileGen.AddFormat("public const int {0} = {1};", component.ShowName, component.Id);
                        }
                    }
                }
                fileGen.AddLine();
                fileGen.AddFormat("public const int TotalComponents = {0};", isView ? components.ViewComponentCount: components.GameComponentCount);
                fileGen.AddLine();
                fileGen.AddLine("public static readonly string[] componentNames = ");
                using (new FileGenerator.Scop(fileGen, true))
                {
                    foreach (var component in components.GameComponents)
                    {
                        if (!component.IsUnique)
                        {
                            fileGen.AddFormat("\"{0}\",", component.ShowName);
                        }
                    }
                    if (isView)
                    {
                        foreach (var component in components.ViewComponents)
                        {
                            if (!component.IsUnique)
                            {
                                fileGen.AddFormat("\"{0}\",", component.ShowName);
                            }
                        }
                    }
                }
                fileGen.AddLine();
                fileGen.AddLine("public static readonly System.Type[] componentTypes = ");
                using (new FileGenerator.Scop(fileGen, true))
                {
                    foreach (var component in components.GameComponents)
                    {
                        if (!component.IsUnique)
                        {
                            fileGen.AddFormat("typeof({0}),", component.FullName);
                        }
                    }
                    if (isView)
                    {
                        foreach (var component in components.ViewComponents)
                        {
                            if (!component.IsUnique)
                            {
                                fileGen.AddFormat("typeof({0}),", component.FullName);
                            }
                        }
                    }
                }
            }
        }
    }
}
