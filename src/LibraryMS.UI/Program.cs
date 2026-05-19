using System.Windows.Forms;
using LibraryMS.UI.Forms;

namespace LibraryMS.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Boot through the login gate — MainForm only opens after authentication
            Application.Run(new LoginForm());
        }
    }
}
