using Windows.ApplicationModel.Core;
using WinRT;

namespace WASForCoreApp
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
