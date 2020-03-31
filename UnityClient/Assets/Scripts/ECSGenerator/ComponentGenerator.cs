using System.Text;

namespace ECSGenerator
{
    public static class ComponentGenerator
    {
        public static void Gen(ComonentInfo info, FileGenerator file, bool isView)
        {
            if (info.IsUnique)
                return;

            file.AddLine("public partial class GameEntity");
            using (new FileGenerator.Scop(file))
            {
                if (info.Fields.Count > 0)
                {
                    GenNormalComponent(info, file, isView);
                }
                else
                {
                    GenFlagComponent(info, file, isView);
                }
            }
            GenMatcher(info, file, isView);
        }

        private static void GenFlagComponent(ComonentInfo info, FileGenerator file, bool isView)
        {
            file.AddFormat("static readonly {0} {1} = new {0}();", info.FullName, LowerFirstCase(info.FullName));
            string lookupName = string.Format("{0}ComponentsLookup.{1}", isView ? "View" : "Game", info.ShowName);
            file.AddFormat("public bool is{0}", info.ShowName);
            using (new FileGenerator.Scop(file))
            {
                file.AddFormat("get {0} return HasComponent({2}); {1}", "{", "}", lookupName);
                file.AddLine("set");
                using (new FileGenerator.Scop(file))
                {
                    file.AddFormat("if (value != is{0})", info.ShowName);
                    using (new FileGenerator.Scop(file))
                    {
                        file.AddFormat("var index = {0};", lookupName);
                        file.AddLine("if (value)");
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddLine("var componentPool = GetComponentPool(index);");
                            file.AddLine("var component = componentPool.Count > 0 ? componentPool.Pop() : blockMoveComponent;");
                            file.AddLine("AddComponent(index, component);");
                        }
                        file.AddLine("else");
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddLine("RemoveComponent(index);");
                        }
                    }
                }
            }
        }

        private static void GenNormalComponent(ComonentInfo info, FileGenerator file, bool isView)
        {

            string lookupName = string.Format("{0}ComponentsLookup.{1}", isView ? "View" : "Game", info.ShowName);

            file.AddFormat("{2} {3} {0} get {0} return ({2})GetComponent({4}); {1} {1}"
                , "{", "}"
                , info.FullName, LowerFirstCase(info.ShowName)
                , lookupName);
            file.AddFormat("public bool has{2} {0} get {0} return HasComponent({3});{1} {1}"
                , "{", "}"
                , info.ShowName, lookupName);
            file.AddLine();

            //AddComponent
            file.AddFormat("public void Add{0}({1})", info.ShowName, GenParamList(info));
            using (new FileGenerator.Scop(file))
            {
                file.AddFormat("var index = {0};", lookupName);
                file.AddFormat("var component = CreateComponent<{0}>(index);", info.FullName);
                GenAssignment(info, file);
                file.AddLine("AddComponent(index, component);");
            }
            file.AddLine();

            //ReplaceComponent
            file.AddFormat("public void Replace{0}({1})", info.ShowName, GenParamList(info));
            using (new FileGenerator.Scop(file))
            {
                file.AddFormat("var index = {0};", lookupName);
                file.AddFormat("{0} component = null;", info.FullName);
                file.AddLine("if (HasComponent(index))");
                using (new FileGenerator.Scop(file))
                {
                    file.AddFormat("component = ({0})GetComponent(index);", info.FullName);
                }
                file.AddLine("else");
                using (new FileGenerator.Scop(file))
                {
                    file.AddFormat("component = CreateComponent<{0}>(index);", info.FullName);
                }
                GenAssignment(info, file);
                file.AddLine("ReplaceComponent(index, component);");
            }
            file.AddLine();

            //RemoveComponent
            file.AddFormat("public void Remove{0}()", info.ShowName);
            using (new FileGenerator.Scop(file))
            {
                file.AddFormat("RemoveComponent({0});", lookupName);
            }
        }

        public static void GenMatcher(ComonentInfo info, FileGenerator file, bool isView)
        {
            string lookupName = string.Format("{0}ComponentsLookup.{1}", isView ? "View" : "Game", info.ShowName);
            if (isView)
            {
                file.AddLine("public sealed partial class ViewMatcher");
            }
            else
            {
                file.AddLine("public sealed partial class GameMatcher");
            }
            using (new FileGenerator.Scop(file))
            {
                file.AddFormat("static ECSCore.IMatcher<GameEntity> _matcher{0};", info.ShowName);
                file.AddLine();
                file.AddFormat("public static ECSCore.IMatcher<GameEntity> {0}", info.ShowName);
                using (new FileGenerator.Scop(file))
                {
                    file.AddLine("get");
                    using (new FileGenerator.Scop(file))
                    {
                        file.AddFormat("if (_matcher{0} == null)", info.ShowName);
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddFormat("var matcher = (ECSCore.Matcher<GameEntity>)ECSCore.Matcher<GameEntity>.AllOf({0});", lookupName);
                            if (isView)
                            {
                                file.AddLine("matcher.componentNames = ViewComponentsLookup.componentNames;");
                            }
                            else
                            {
                                file.AddLine("matcher.componentNames = GameComponentsLookup.componentNames;");
                            }
                            file.AddFormat("_matcher{0} = matcher;", info.ShowName);
                        }
                        file.AddFormat("return _matcher{0};", info.ShowName);
                    }
                }
            }
        }

        public static string GenParamList(ComonentInfo info)
        {
            StringBuilder sb = new StringBuilder();
            for (int i=0; i<info.Fields.Count; ++i)
            {
                var field = info.Fields[i];
                sb.AppendFormat("{0} new{1}", field.TypeName, field.Name);
                if (i<info.Fields.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }
        
        public static void GenAssignment(ComonentInfo info, FileGenerator file)
        {
            foreach (var field in info.Fields)
            {
                file.AddFormat("component.{0} = new{0};", field.Name);
            }
        }

        public static string LowerFirstCase(string name)
        {
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }
    }
}
