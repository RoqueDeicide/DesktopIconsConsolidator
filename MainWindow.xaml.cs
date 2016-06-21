using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace DesktopIconsConsolidator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static MainWindow window;

		private readonly DispatcherTimer refreshTimer;
		private readonly DispatcherTimer logClearTimer;

		public MainWindow()
		{
			this.InitializeComponent();

			window = this;

			var tray = this.FindResource("MainTaskBarIcon") as TaskbarIcon;
			if (tray == null)
			{
				Log("Unable to find the system tray icon object.");
			}
			else
			{
				tray.Visibility = Visibility.Visible;
			}

			this.refreshTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMinutes(15),
				IsEnabled = true
			};
			this.refreshTimer.Start();
			this.refreshTimer.Tick += (sender, args) => Refresh();

			this.logClearTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromHours(2),
				IsEnabled = true
			};
			this.logClearTimer.Start();
			this.logClearTimer.Tick += (sender, args) => ClearLog();

			this.Closing += (sender, args) =>
			{
				this.refreshTimer.Stop();
				this.logClearTimer.Stop();
			};

			Refresh();
		}

		private static void Log(string message)
		{
			window.Dispatcher.BeginInvoke(new Action<string>(window.PostLogMessage), message);
		}
		private void PostLogMessage(string message)
		{
			var par = this.LogTextBox.Document.Blocks.FirstBlock as Paragraph;

			par?.Inlines.Add($"{message}{Environment.NewLine}");
		}
		private static void ClearLog()
		{
			window.Dispatcher.BeginInvoke(new Action(() =>
			{
				var par = window.LogTextBox.Document.Blocks.FirstBlock as Paragraph;

				par?.Inlines.Clear();
			}));
		}

		private static void Refresh()
		{
			Log("Refreshing...");

			string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string usersFolder = Directory.GetParent(userFolder).FullName;

			Log($"Scanning the folder {usersFolder}.");

			var folders = from directory in Directory.EnumerateDirectories(usersFolder)
						  let directoryInfo = new DirectoryInfo(directory)
						  let desktopDir = Path.Combine(directory, "Desktop")
						  where directoryInfo.Name != "Public" && Directory.Exists(desktopDir) &&
								!directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)
						  select desktopDir;

			string[] desktopFolders = folders.ToArray();

			window.Dispatcher.BeginInvoke(new Action<string[]>(window.UpdateFolders),
										  (object)desktopFolders);

			string publicDesktop = Path.Combine(usersFolder, "Public", "Desktop");

			var files = from folder in desktopFolders
						from file in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
						let ext = Path.GetExtension(file)
						where ext == ".lnk" || ext == ".url"
						select file;
			foreach (var file in files)
			{
				Log($"Found {file}.");
				string dest = Path.Combine(publicDesktop, Path.GetFileName(file) ?? "");

				if (File.Exists(dest))
				{
					var result = MessageBox.Show($"The desktop file named {Path.GetFileName(file)} " +
												 $"already exists in /Public/Desktop/ folder. Overwrite it " +
												 $"(Yes) or delete one in the user folder (No), or leave " +
												 $"them both (Cancel) ?",
												 "File already exists.", MessageBoxButton.YesNoCancel,
												 MessageBoxImage.Question);

					switch (result)
					{
						case MessageBoxResult.Cancel:
							continue;
						case MessageBoxResult.Yes:
							File.Delete(dest);
							break;
						case MessageBoxResult.No:
							File.Delete(file);
							continue;
					}
				}

				Log($"Moved to {dest}.");
				File.Move(file, dest);
			}
		}

		private void UpdateFolders(string[] folders)
		{
			var currentFolders = this.FindResource("CurrentFolders") as CurrentFoldersCollection;
			if (currentFolders == null)
			{
				return;
			}

			currentFolders.Clear();
			Array.Sort(folders);

			foreach (string folder in folders)
			{
				Log($"Found {folder}.");

				currentFolders.Add(folder);
			}
		}
		private void ManualRefreshButtonClick(object sender, RoutedEventArgs e)
		{
			Refresh();
		}
		private void ManualLogClear(object sender, RoutedEventArgs e)
		{
			ClearLog();
		}
		private void ShowStatusWindow(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Normal;
		}
		private void ExistApplication(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		private void UpdateTaskBarPresence(object sender, EventArgs e)
		{
			this.ShowInTaskbar = this.WindowState != WindowState.Minimized;
		}
		private void ClearTray(object sender, CancelEventArgs e)
		{
			var tray = this.FindResource("MainTaskBarIcon") as TaskbarIcon;
			tray?.Dispose();
		}
		private void ShowAboutWindow(object sender, RoutedEventArgs e)
		{
			new AboutWindow().Show();
		}
	}
}