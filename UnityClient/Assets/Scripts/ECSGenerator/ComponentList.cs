using System.Collections.Generic;

namespace ECSGenerator
{
    public class ComponentList
    {
        public List<ComonentInfo> GameComponents { get; private set; } = new List<ComonentInfo>();
        public List<ComonentInfo> ViewComponents { get; private set; } = new List<ComonentInfo>();
        public int GameComponentCount { get; private set; }
        public int ViewComponentCount { get; private set; }

        public void GenId()
        {
            ViewComponents.Sort((a, b) => { return a.ShowName.CompareTo(b.ShowName); });
            GameComponents.Sort((a, b) => { return a.ShowName.CompareTo(b.ShowName); });

            int index = 0;
            foreach (var component in GameComponents)
            {
                if (!component.IsUnique)
                {
                    component.Id = index++;
                }
            }
            GameComponentCount = index;
            foreach (var component in ViewComponents)
            {
                if (!component.IsUnique)
                {
                    component.Id = index++;
                }
            }
            ViewComponentCount = index;
        }
    }
}
