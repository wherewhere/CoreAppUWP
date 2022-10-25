using Windows.ApplicationModel.Core;
using WinRT;

namespace UWPForCoreApp
{
    public class Program
    {
        private static void Main(string[] args)
        {
            ComWrappersSupport.InitializeComWrappers();
            CoreApplication.Run(new App());
        }
    }
}
