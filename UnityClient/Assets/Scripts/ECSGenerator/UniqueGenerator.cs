using System.Collections.Generic;

namespace ECSGenerator
{
    public static class UniqueGenerator
    {
        public static void Gen(List<ComonentInfo> list, FileGenerator file, bool isView)
        {
            if (isView)
            {
                file.AddLine("public partial class ViewContext");
            }
            else
            {
                file.AddLine("public partial class GameContext");
            }
            using (new FileGenerator.Scop(file))
            {
                foreach (var info in list)
                {
                    if (!info.IsUnique)
                        continue;
                    if (info.Fields.Count > 0)
                    {
                        string memberName = ComponentGenerator.LowerFirstCase(info.ShowName);
                        //member
                        file.AddFormat("public {2} {3} {0}get; private set;{1}", "{", "}", info.FullName, memberName);
                        //set
                        file.AddFormat("public void Set{0}({1})", info.ShowName, ComponentGenerator.GenParamList(info));
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddFormat("if({0} == null)", memberName);
                            using (new FileGenerator.Scop(file))
                            {
                                file.AddFormat("{0} = new {1}();", memberName, info.FullName);
                            }
                            file.AddFormat("var component = {0};", memberName);
                            ComponentGenerator.GenAssignment(info, file);
                        }
                        //remove
                        file.AddFormat("public void Remove{0}()", info.ShowName);
                        using (new FileGenerator.Scop(file))
                        {
                            file.AddFormat("{0} = null;", memberName);
                        }
                    }
                    else
                    {
                        file.AddFormat("public bool is{2} {0}get; set;{1}", "{", "}", info.ShowName);
                    }
                }
            }
        }
        
    }
}
