using System.Windows;

namespace EasySave.GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GUIProgram.Start();
        }
    }
}