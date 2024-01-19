using System.Collections.Generic;

namespace Project
{
    public sealed class GravityManager : SingletonBase<GravityManager>
    {
        public List<GravitySource> AllSources { get; private set; } = new List<GravitySource>();

        public static void Register(GravitySource source)
        {
            if (!Instance.AllSources.Contains(source))
                Instance.AllSources.Add(source);
        }

        public static void Deregister(GravitySource source)
        {
            if (Instance != null)
                Instance.AllSources.Remove(source);
        }
    }
}