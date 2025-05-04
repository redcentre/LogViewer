using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using RCS.LogViewer.Model;
using WF = System.Windows.Forms;

namespace RCS.LogViewer;

partial class MainWindow : Window, WF.IWin32Window
{
	DispatcherTimer? mainTimer;
	readonly Style centreStyle;
	readonly Style rightStyle;
	readonly IValueConverter verter;
	sealed record ColTup(string[] Names, Style? Style, string? ConvParam = null, int? Width = null);
	readonly ColTup[] ColTups;
	readonly string[] ColSkips;
	AnalyseResultsWindow? analysisWindow;

	#region App Lifetime

	public nint Handle => new WindowInteropHelper(this).Handle;

	public MainWindow()
	{
		InitializeComponent();
		centreStyle = (Style)FindResource("GridColCentreStyle");
		rightStyle = (Style)FindResource("GridColRightStyle");
		verter = (IValueConverter)FindResource("MainVerter");
		ColTups = [
			new ColTup(["Timestamp"], centreStyle, "LogTimestamp"),
			new ColTup(["Method", "Sid", "Source", "EventTime"], centreStyle, null),
			new ColTup(["Seconds"], rightStyle, "LogSeconds"),
			new ColTup(["ErrorStack", "ErrorStackTrace"], null, "LogErrorStack"),
			new ColTup(["Message", "Message1", "Message2"], null, null, 340)
		];
		ColSkips = ["odata.etag"];
		if (!DesignerProperties.GetIsInDesignMode(this))
		{
			Loaded += MainWindow_Loaded;
			Closing += MainWindow_Closing;
			Closed += MainWindow_Closed;
			LoadWindowBounds();
		}
	}

	void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		Controller.PropertyChanged += Controller_PropertyChanged;
		mainTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
		mainTimer.Tick += (s, e2) =>
		{
			Controller.TimeTick();
		};
		mainTimer.Start();
		if (Controller.ActiveSettings.HasAzureSettings)
		{
			// Scan for tables when the app loads and use any cached copy.
			MainCommands.ScanSubscription.Execute("Cache", this);
		}
		else
		{
			// Prompt the user to enter valud credentials.
			MainCommands.LaunchSettings.Execute(null, this);
		}
	}

	void Controller_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Controller.AnalItems))
		{
			analysisWindow?.Close();
		}
	}

	void MainWindow_Closing(object? sender, CancelEventArgs e)
	{
		mainTimer?.Stop();
		e.Cancel = false;
	}

	void MainWindow_Closed(object? sender, EventArgs e)
	{
		SaveWindowBounds();
	}

	MainController Controller => (MainController)DataContext;

	void LoadWindowBounds()
	{
		WindowStartupLocation = WindowStartupLocation.Manual;
		Rect r = SystemParameters.WorkArea;
		r.Inflate(-r.Width / 12, -r.Height / 12);
		r = Controller.SettingStore.Get(null, nameof(System.Windows.WindowStartupLocation), r);
		Top = r.Top;
		Left = r.Left;
		Width = r.Width;
		Height = r.Height;
	}

	void SaveWindowBounds()
	{
		if (WindowState == WindowState.Normal)
		{
			Controller.SettingStore.Put(null, nameof(System.Windows.WindowStartupLocation), new Rect(Left, Top, Width, Height));
		}
	}

	#endregion

	#region Control Event Handlers

	void NavigationTree_SelItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
	{
		Controller.SelectedNode = (AppNode)e.NewValue;
	}

	void EventIds_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		Controller.SearchRawEventIds = null;
	}

	void PK_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		Controller.SearchPK = null;
	}

	void Quick_DoubleClick(object sender, MouseButtonEventArgs e)
	{
		Controller.SearchQuickFilter = null;
	}

	void TrapSearch_KeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			MainCommands.SearchTable.Execute(null, this);
		}
	}

	/// <summary>
	/// As grid columns are dynamically created, certain names and types from Azure Table properties
	/// are recognised and the display properties of the column are adjust accordingly.
	/// </summary>
	void LogGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
	{
		var textcol = (DataGridTextColumn)e.Column;
		if (e.PropertyType == typeof(int) || e.PropertyType == typeof(long))
		{
			// Integer columns are simply right aligned.
			textcol.ElementStyle = rightStyle;
			return;
		}
		if (ColSkips.Contains(e.PropertyName))
		{
			e.Cancel = true;
			return;
		}
		ColTup? tup = ColTups.FirstOrDefault(x => x.Names.Any(n => n == e.PropertyName));
		if (tup != null)
		{
			if (tup.Style != null)
			{
				textcol.ElementStyle = tup.Style;
			}
			if (tup.ConvParam != null)
			{
				var bind = (Binding)textcol.Binding;
				bind.Converter = verter;
				bind.ConverterParameter = tup.ConvParam;
			}
			if (tup.Width != null)
			{
				textcol.Width = tup.Width.Value;
			}
		}
	}

	#endregion
}
