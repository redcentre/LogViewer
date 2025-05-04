using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RCS.LogViewer;

internal static partial class MainCommands
{
	public static RoutedUICommand LaunchSettings = new("Launch Settings", "LaunchSettings", typeof(Window));
	public static RoutedUICommand ScanSubscription = new("Scan Tables", "ScanSubscription", typeof(Window));
	public static RoutedUICommand SearchTable = new("Search Table", "SearchTable", typeof(Window));
	public static RoutedUICommand AnalyseTable = new("Analyse Table", "AnalyseTable", typeof(Window));
	public static RoutedUICommand PurgeCount = new("Purge Count", "PurgeCount", typeof(Window));
	public static RoutedUICommand PurgeRun = new("Purge Run", "PurgeRun", typeof(Window));
}

partial class MainWindow
{
	void CloseCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null;
	void CloseExecute(object sender, ExecutedRoutedEventArgs e) => Close();

	void HelpCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
	void HelpExecute(object sender, ExecutedRoutedEventArgs e) => TrapHelp();

	void LaunchSettingsCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null;
	void LaunchSettingsExecute(object sender, ExecutedRoutedEventArgs e) => TrapLaunchSettings();

	void ScanSubscriptionCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.ActiveSettings.HasAzureSettings;
	async void ScanSubscriptionExecute(object sender, ExecutedRoutedEventArgs e) => await TrapScanSub((string)e.Parameter);

	void SearchTableCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.SelectedNode?.Type == Model.NodeType.Table;
	async void SearchTableExecute(object sender, ExecutedRoutedEventArgs e) => await Controller.SearchTable();

	void AnalyseTableCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.SelectedNode?.Type == Model.NodeType.Table;
	async void AnalyseTableExecute(object sender, ExecutedRoutedEventArgs e) => await TrapAnalysis();

	void PurgeCountCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.AnalItems?.Count > 0;
	async void PurgeCountExecute(object sender, ExecutedRoutedEventArgs e) => await Controller.PurgeRowCount();

	void PurgeRunCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.PurgeCount > 0;
	async void PurgeRunExecute(object sender, ExecutedRoutedEventArgs e) => await Controller.PurgeRowRun();

	void TrapHelp()
	{
		var asm = GetType().Assembly;
		var an = asm.GetName();
		string? company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
		string? copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
		string? framework = asm.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkDisplayName;
		string message = $"Assembly Version – {an.Version}\nCompany – {company}\nCopyright – {copyright}\nFramework – {framework}";
		Pop.Information(this, Application.Current.MainWindow.Title, "About", message);
	}

	void TrapLaunchSettings()
	{
		Controller.ActiveSettings.BeginEdit();
		var window = new AppSettingsWindow()
		{
			Owner = this,
			DataContext = DataContext,
		};
		if (window.ShowDialog() == true)
		{
			Controller.ActiveSettings.CheckedEndEdit(out bool credentialsChanged);
			Controller.SaveSettings();
			if (credentialsChanged)
			{
				Controller.ResetApp();
			}
		}
		else
		{
			Controller.ActiveSettings.CancelEdit();
		}
	}

	async Task TrapScanSub(string scanParam)
	{
		try
		{
			await Controller.ScanSubscription(scanParam);
		}
		catch (Exception ex)
		{
			string message = $"""
				Scanning the Azure subscription to find Tables failed.
				
				{ex.GetType().Name}\n{ex.Message}

				The most common cause of this error is incorrect credentials. Ensure that the correct Azure Subscription credentials are entered in the Settings window.
				""";

			Pop.Error(this, Application.Current.MainWindow.Title, "Table scan filed.", message, ex.ToString());
		}
	}

	async Task TrapAnalysis()
	{
		Controller.PurgeCountMessage = "Count unknown";
		Controller.PurgeDoneMessage = "No data";
		Controller.PurgeDate = DateTime.Now.AddMonths(-6);
		await Controller.AnalyseTable();
		if (analysisWindow == null)
		{
			analysisWindow = new AnalyseResultsWindow()
			{
				Owner = this,
				DataContext = DataContext
			};
			analysisWindow.Closed += (s, e) =>
			{
				analysisWindow = null;
			};
			analysisWindow.Show();  // Floating window
		}
	}
}
