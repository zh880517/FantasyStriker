using System.IO;

namespace ECSCore
{
    public static class EntitasResources
    {
        public static string GetVersion()
        {
            var assembly = typeof(Entity).Assembly;
            var stream = assembly.GetManifestResourceStream("version.txt");
            string version;
            using (var reader = new StreamReader(stream))
            {
                version = reader.ReadToEnd();
            }

            return version;
        }
    }
}
