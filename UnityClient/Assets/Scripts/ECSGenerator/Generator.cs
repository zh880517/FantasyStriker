using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace ECSGenerator
{
    public class Generator
    {
        private readonly string gamePath;
        private readonly string viewPath;
        private readonly Dictionary<string, List<FileGenerator>> genFiles = new Dictionary<string, List<FileGenerator>>();
        private readonly ComponentList componentList = new ComponentList();

        public Generator(string gamePath, string viewPath, params Assembly[] assemblies)
        {
            this.gamePath = FormatPath(gamePath);
            this.viewPath = FormatPath(viewPath);
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(ECSCore.IComponent)))
                    {
                        ComonentInfo info = ComonentInfo.FromType(type);
                        if (info != null)
                        {
                            if (type.GetCustomAttribute<ViewAttribute>() != null)
                            {
                                componentList.ViewComponents.Add(info);
                            }
                            else
                            {
                                componentList.GameComponents.Add(info);
                            }
                        }
                    }
                }
            }

            componentList.GenId();
        }

        public void Gen()
        {
            {
                FileGenerator contentFile = CreateFile("GameContext", gamePath);
                ContextGenerator.GenGameContext(contentFile, componentList);

                FileGenerator lookupfile = CreateFile("GameComponentsLookup", gamePath);
                LookupGenerator.Gen(componentList, lookupfile, false);
            }
            {
                FileGenerator contentFile = CreateFile("ViewContext", viewPath);
                ContextGenerator.GenViewContext(contentFile, componentList);

                FileGenerator lookupfile = CreateFile("ViewComponentsLookup", viewPath);
                LookupGenerator.Gen(componentList, lookupfile, true);
            }
            {
                if (componentList.GameComponents.Exists(obj=>obj.IsUnique))
                {
                    var file = CreateFile("GameUniqueComponent", gamePath);
                    UniqueGenerator.Gen(componentList.GameComponents, file, false);
                }
                if (componentList.ViewComponents.Exists(obj => obj.IsUnique))
                {
                    var file = CreateFile("ViewUniqueComponent", viewPath);
                    UniqueGenerator.Gen(componentList.ViewComponents, file, true);
                }
            }
            foreach(var info in componentList.GameComponents)
            {
                if (!info.IsUnique)
                {
                    var file = CreateFile(string.Format("{0}{1}", "Components/Game", info.ShowName), gamePath);
                    ComponentGenerator.Gen(info, file, false);
                }
            }
            foreach (var info in componentList.ViewComponents)
            {
                if (!info.IsUnique)
                {
                    var file = CreateFile(string.Format("{0}{1}", "Components/View", info.ShowName), viewPath);
                    ComponentGenerator.Gen(info, file, true);
                }
            }
            //write file
            var utf8WithoutBom = new System.Text.UTF8Encoding(false);
            foreach (var kv in genFiles)
            {
                foreach (var file in kv.Value)
                {
                    string path = kv.Key + file.Name;
                    string dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    string content = file.ToString();
                    if (File.Exists(path) && content == File.ReadAllText(path, utf8WithoutBom))
                        continue;
                    File.WriteAllText(path, content, utf8WithoutBom);
                }
            }
            DeleteUnUsedFile();
        }

        private string FormatPath(string path)
        {
            path = Path.GetFullPath(path).Replace("\\", "/");
            if (!path.EndsWith("/"))
                path = path + "/";
            return path;
        }

        private void DeleteUnUsedFile()
        {
            foreach (var kv in genFiles)
            {
                var files = Directory.GetFiles(kv.Key, "*.cs", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string name = file.Replace('\\', '/').Replace(kv.Key, "");
                    if (!kv.Value.Exists(obj=>obj.Name == name))
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        private FileGenerator CreateFile(string name, string path)
        {
            if (!genFiles.TryGetValue(path, out var files))
            {
                files = new List<FileGenerator>();
                genFiles.Add(path, files);
            }
            FileGenerator file = new FileGenerator(name + ".cs");
            files.Add(file);
            return file;
        }
    }
}
