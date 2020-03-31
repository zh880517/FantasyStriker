using System.Collections.Generic;

namespace ECSGenerator
{
    public static class ContextGenerator
    {
        public static void GenGameContext(FileGenerator file, ComponentList components)
        {
            file.AddLine("public partial class GameEntity : ECSCore.Entity");
            file.BeginScop();
            file.EndScop();
            file.AddLine();

            file.AddLine("public partial class GameContext : ECSCore.Context<GameEntity>");
            using (new FileGenerator.Scop(file))
            {
                GenEntityIndexKey(file, components.GameComponents);
                file.AddLine("public GameContext(int totalComponents, string[] names, System.Type[] types, string name)");
                file.AddLine("    : base(totalComponents, 0, new ECSCore.ContextInfo(name, names, types),");
                file.AddLine("          (entity) =>\n#if (!UNITY_EDITOR)");
                file.AddLine("            new UnsafeAERC(),\n#else");
                file.AddLine("            new ECSCore.SafeAERC(entity),\n#endif");
                file.AddLine("             () => new GameEntity())");
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("InitializeEntityIndices();");
                }
                file.AddLine();

                file.AddLine("public GameContext(int totalComponents, string[] names, System.Type[] types)");
                file.AddLine("   : this(totalComponents, names, types, \"Game\")");
                file.BeginScop();
                file.EndScop();
                file.AddLine();

                file.AddLine("public GameContext()");
                file.AddLine("   : this(GameComponentsLookup.TotalComponents, GameComponentsLookup.componentNames, GameComponentsLookup.componentTypes, \"Game\")");
                file.BeginScop();
                file.EndScop();
                file.AddLine();

                file.AddLine("protected virtual void InitializeEntityIndices()");
                using (new FileGenerator.Scop(file))
                {
                    GenEntityIndex(file, components.GameComponents, false);
                }
                file.AddLine();
                GenGetEntityIndex(file, components.GameComponents, false);
            }
            GenMatcher(file);
        }

        public static void GenViewContext(FileGenerator file, ComponentList components)
        {
            file.AddLine("public partial class ViewContext : GameContext");
            using (new FileGenerator.Scop(file))
            {
                GenEntityIndexKey(file, components.ViewComponents);
                file.AddLine("public ViewContext()");
                file.AddLine("   : base(ViewComponentsLookup.TotalComponents, ViewComponentsLookup.componentNames, ViewComponentsLookup.componentTypes, \"View\")");
                file.BeginScop();
                file.EndScop();
                file.AddLine();
                file.AddLine("protected override void InitializeEntityIndices()");
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("base.InitializeEntityIndices();");
                    GenEntityIndex(file, components.ViewComponents, true);
                }
                file.AddLine();
                GenGetEntityIndex(file, components.ViewComponents, true);
            }
        }

        public static void GenMatcher(FileGenerator file)
        {
            file.AddLine("public sealed partial class GameMatcher");
            using (new FileGenerator.Scop(file))
            {
                //AllOf
                file.AddLine("public static ECSCore.IAllOfMatcher<GameEntity> AllOf(params int[] indices)");
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("return ECSCore.Matcher<GameEntity>.AllOf(indices);");
                }
                file.AddLine("public static ECSCore.IAllOfMatcher<GameEntity> AllOf(params ECSCore.IMatcher<GameEntity>[] matchers)");
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("return ECSCore.Matcher<GameEntity>.AllOf(matchers);");
                }
                //AnyOf
                file.AddLine("public static ECSCore.IAnyOfMatcher<GameEntity> AnyOf(params int[] indices)");
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("return ECSCore.Matcher<GameEntity>.AnyOf(indices);");
                }
                file.AddLine("public static ECSCore.IAnyOfMatcher<GameEntity> AnyOf(params ECSCore.IMatcher<GameEntity>[] matchers)");
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("return ECSCore.Matcher<GameEntity>.AnyOf(matchers);");
                }
            }
        }

        private static void GenEntityIndexKey(FileGenerator file, List<ComonentInfo> list)
        {
            foreach (var component in list)
            {
                if (component.IsUnique)
                    continue;
                foreach (var field in component.Fields)
                {
                    if (field.IndexType == ComonentInfo.EntityIndexType.None)
                        continue;
                    file.AddFormat("public const string {0}_{1} = \"{0}.{1}\";", component.ShowName, field.Name);
                }
            }
        }

        private static void GenEntityIndex(FileGenerator file, List<ComonentInfo> list, bool isView)
        {

            foreach (var component in list)
            {
                if (component.IsUnique)
                    continue;
                foreach (var field in component.Fields)
                {
                    if (field.IndexType == ComonentInfo.EntityIndexType.None)
                        continue;
                    if (field.IndexType == ComonentInfo.EntityIndexType.Index)
                    {
                        file.AddFormat("AddEntityIndex(new ECSCore.EntityIndex<GameEntity, {0}>(", field.TypeName);
                    }
                    else
                    {
                        file.AddFormat("AddEntityIndex(new ECSCore.PrimaryEntityIndex<GameEntity, {0}>(", field.TypeName);
                    }
                    file.AddFormat("    {0}_{1},", component.ShowName, field.Name);
                    file.AddFormat("    GetGroup({0}Matcher.{1}),", isView ? "View" : "Game", component.ShowName);
                    file.AddFormat("    (e, c) => (({0})c).{1}));", component.FullName, field.Name);
                }
            }
        }

        private static void GenGetEntityIndex(FileGenerator file, List<ComonentInfo> list, bool isView)
        {
            foreach (var component in list)
            {
                if (component.IsUnique)
                    continue;
                foreach (var field in component.Fields)
                {
                    if (field.IndexType == ComonentInfo.EntityIndexType.None)
                        continue;
                    if (field.IndexType == ComonentInfo.EntityIndexType.Index)
                    {
                        string paramName = ComponentGenerator.LowerFirstCase(field.Name);
                        file.AddFormat("public System.Collections.Generic.HashSet<GameEntity> GetEntitiesWith{0}{1}({2} {3})", 
                            component.ShowName, field.Name, field.TypeName, paramName);
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddFormat("return ((ECSCore.EntityIndex<GameEntity, {0}>)GetEntityIndex({1}_{2})).GetEntities({3});",
                                field.TypeName, component.ShowName, field.Name, paramName);
                        }
                    }
                    else
                    {
                        string paramName = ComponentGenerator.LowerFirstCase(field.Name);
                        file.AddFormat("public GameEntity GetEntityWith{0}{1}({2} {3})",
                            component.ShowName, field.Name, field.TypeName, paramName);
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddFormat("return ((ECSCore.PrimaryEntityIndex<GameEntity, {0}>)GetEntityIndex({1}_{2})).GetEntity({3});",
                                field.TypeName, component.ShowName, field.Name, paramName);
                        }
                    }
                }
            }
        }
    }
}
