using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RCS.LogViewer;

internal static partial class MainCommands
{
	public static RoutedUICommand ScanSubscription = new("Scan Tables", "ScanSubscription", typeof(Window));
	public static RoutedUICommand AnalyseTable = new("Analyse Table", "AnalyseTable", typeof(Window));
}

partial class MainWindow
{
	void CloseCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null;
	void CloseExecute(object sender, ExecutedRoutedEventArgs e) => Close();

	void HelpCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
	void HelpExecute(object sender, ExecutedRoutedEventArgs e) => TrapHelp();

	void ScanSubscriptionCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.ActiveSettings.HasAzureSettings;
	async void ScanSubscriptionExecute(object sender, ExecutedRoutedEventArgs e) => await TrapScanSub((string)e.Parameter);

	void AnalyseTableCanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Controller.BusyMessage == null && Controller.SelectedNode?.Type == Model.NodeType.Table;
	async void AnalyseTableExecute(object sender, ExecutedRoutedEventArgs e) => await TrapAnalysis();

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
