using System.Windows;
using HomeCharts.View;
using HomeCharts.Views;

namespace HomeCharts
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected void ApplicationStart(object sender, StartupEventArgs e)
        {
            // var loginView = new LoginView();
            // loginView.Show();
            // loginView.IsVisibleChanged += (s, ev) =>
            // {
            //     if (loginView.IsVisible == false && loginView.IsLoaded)
            //     {
                    var mainView = new MainView();
                    mainView.Show();

            //         loginView.Close();
            //     }
            // };
        }
    }
}