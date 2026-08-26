using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Focar.Views
{
	/// <summary>
	/// Interaction logic for PomodoroRelatorioWindow.xaml
	/// </summary>
	public partial class PomodoroRelatorioWindow : Window
	{
		public PomodoroRelatorioWindow()
		{
			InitializeComponent();
		}

		private void BotaoFechar_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
