using System.Windows;
using LaboratoryModule.Models;

namespace LaboratoryModule
{
    public partial class App : Application
    {
        public static string ApiBaseUrl = "http://localhost:5134";
        public static string AuthToken = "";
        public static User CurrentUser = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}