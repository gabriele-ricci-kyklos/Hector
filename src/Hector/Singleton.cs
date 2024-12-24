namespace Hector
{
    // credits: C# in Depth by Jon Skeet - http://csharpindepth.com/Articles/General/Singleton.aspx
    public sealed class Singleton<T> where T : class, new()
    {
        public static T Instance { get; } = new T();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Singleton()
        {
        }

        private Singleton()
        {
        }
    }
}
