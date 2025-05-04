using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Xml.Linq;
using WF = System.Windows.Forms;

namespace RCS.LogViewer;

public partial class AnalyseResultsWindow : Window, WF.IWin32Window
{
	public AnalyseResultsWindow()
	{
		InitializeComponent();
		Loaded += AnalyseResultsWindow_Loaded;
	}
	public nint Handle => new WindowInteropHelper(this).Handle;

	void AnalyseResultsWindow_Loaded(object sender, RoutedEventArgs e)
	{
		if (Controller.AnalPkOverflowCount > 0)
		{
			int total = Controller.AnalItems!.Count + Controller.AnalPkOverflowCount;
			string message = $"There are too many unique Partition Keys to display. The grid shows the maximum of {Controller.AnalItems.Count} items and {Controller.AnalPkOverflowCount} are not displayed.";
			Pop.Information(this, "Table Analysis", "Too many Partition Keys", message);
		}
	}

	MainController Controller => (MainController)DataContext;

	void AnalyseWindow_PreviewKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape && Controller.BusyMessage == null)
		{
			Close();
		}
	}

	void AnalCopyCSV_Click(object sender, RoutedEventArgs e)
	{
		CopyTextCommon(",");
	}

	private void AnalCopyTSV_Click(object sender, RoutedEventArgs e)
	{
		CopyTextCommon("\t");
	}

	void CopyTextCommon(string join)
	{
		var sb = new StringBuilder();
		string line = string.Join(join, ["PK,Count", "MinRK", "MaxRK", "MinTime", "MaxTime"]);
		foreach (var item in Controller.AnalItems!)
		{
			line = string.Join(join, [item.PK, item.Count, item.MinRowKey, item.MaxRowKey, item.MinTime, item.MaxTime]);
			sb.AppendLine(line);
		}
		Clipboard.SetText(sb.ToString());
	}

	void AnalCopyXML_Click(object sender, RoutedEventArgs e)
	{
		var elem = new XDocument(
			new XElement("analysis",
				Controller.AnalItems!.Select(x => new XElement("pk", new XAttribute("value", x.PK),
					new XElement("count", x.Count),
					new XElement("minRK", x.MinRowKey),
					new XElement("maxRK", x.MaxRowKey),
					new XElement("minTime", x.MinTime),
					new XElement("maxTime", x.MaxTime)
					)
				)
			)
		);
		Clipboard.SetText(elem.ToString());
	}

	void AnalCopyHTML_Click(object sender, RoutedEventArgs e)
	{
		static string Td(object? s) => $"<td>{WebUtility.HtmlEncode(s?.ToString())}</td>";
		static string Tdd(DateTime? d) => $"<td>{AppUtility.LogTime(d)}</td>";
		string filename = Path.Combine(Path.GetTempPath(), $"_logviewer_{Controller.SelectedAccountName}_{Controller.SelectedTableName}.htm");
		using (var writer = new StreamWriter(filename))
		{
			writer.WriteLine($$"""
				<html lang="en">
				<head>
				  <meta charset="utf-8">
				  <title>{{Controller.SelectedFullTableName}}</title>
				  <style type="text/css">
					body { background-color: ghostwhite; font-family: sans-serif;}
					table { border-collapse: collapse; }
					.t1, .t2 { background-color: white; }
					.t1 th, .t1 td, .t2 td { padding: 0.2rem 0.5rem; border: 1px solid silver; }
					.t1 th { font-weight: normal; background-color: gainsboro; }
					.t1 td { font-family: monospace; font-size: 120%; }
					.t1 td:nth-child(2) { text-align: right; }
					.t2 td:nth-child(2) { font-family: monospace; font-size: 120%; }
				  </style>
				</head>
				<body>
				  <h1>{{Controller.SelectedFullTableName}}</h1>
				  <h2>PartitionKey Limits</h2>
				  <table class="t1">
					<tr><th>PK</th><th>Count</th><th>Min RowKey</th><th>Max RowKey</th><th>Min Timestamp</th><th>Max Timestamp</th></tr>
				""");
			foreach (var anal in Controller.AnalItems!)
			{
				writer.WriteLine($"    <tr>{Td(anal.PK)}{Td(anal.Count)}{Td(anal.MinRowKey)}{Td(anal.MaxRowKey)}{Tdd(anal.MinTime)}{Tdd(anal.MaxTime)}</tr>");
			}
			writer.WriteLine($"""
				  </table>
				  <h2>Table Limits</h2>
				  <table class="t2">
				    <tr><td>Minimum Timestamp</td><td>{AppUtility.LogTime(Controller.MinAnalTime)}</td></tr>
				    <tr><td>Maximum Timestamp</td><td>{AppUtility.LogTime(Controller.MaxAnalTime)}</td></tr>
				    <tr><td>Minimum RowKey</td><td>{Controller.MinAnalRowKey}</td></tr>
				    <tr><td>Maximum RowKey</td><td>{Controller.MaxAnalRowKey}</td></tr>
				  </table>
				""");
			writer.WriteLine("</body>");
			writer.WriteLine("</html>");
		}
		Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
	}
}
