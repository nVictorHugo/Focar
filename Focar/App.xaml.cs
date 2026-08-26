using Focar.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Focar
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			Window janelaInicial;

			{
				janelaInicial = new PomodoroWindow();

				janelaInicial.Show();
			}
		}

	}
}