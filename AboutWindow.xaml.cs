using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace DesktopIconsConsolidator
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			this.InitializeComponent();
		}
		private void OpenBrowser(object sender, RoutedEventArgs e)
		{
			Hyperlink link = sender as Hyperlink;
			if (link == null)
			{
				return;
			}

			Process.Start(link.NavigateUri.ToString());
		}
	}
}