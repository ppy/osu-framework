using osu.Framework.Platform;
using osu.Framework;
using Template.Game;

namespace Template.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (var host = Host.GetSuitableHost(@"TemplateGame"))
            using (var game = new Application())
                host.Run(game);
        }
    }
}
