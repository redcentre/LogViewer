using System;
using System.Windows;
using WF = System.Windows.Forms;

namespace RCS.LogViewer;

static class Pop
{
	public static void Information(WF.IWin32Window owner, string caption, string title, string message)
	{
		PrePop();
		var page = new WF.TaskDialogPage
		{
			Caption = caption,
			AllowCancel = true,
			AllowMinimize = false,
			Heading = title,
			Icon = WF.TaskDialogIcon.Information,
			Text = message
		};
		WF.TaskDialog.ShowDialog(owner, page);
	}

	public static void Error(WF.IWin32Window owner, string caption, string title, string message, string? details = null)
	{
		PrePop();
		var page = new WF.TaskDialogPage
		{
			Caption = caption,
			Heading = title,
			Text = message,
			Icon = WF.TaskDialogIcon.ShieldErrorRedBar,
			AllowCancel = true,
			Buttons = { WF.TaskDialogButton.OK },
			DefaultButton = WF.TaskDialogButton.OK,
			SizeToContent = true
		};
		if (details != null)
		{
			string showDetails = details.Length > 1500 ? details[..1500] + "\n[TRUNCATED]" : details;
			page.Expander = new WF.TaskDialogExpander
			{
				Text = showDetails,
				CollapsedButtonText = "Error Details",
			};
			var copyButton = new WF.TaskDialogButton("Copy");
			copyButton.Click += (s, e) =>
			{
				Clipboard.SetText(string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}", Environment.NewLine, caption, DateTime.Now.ToString("f"), title, message, details));
			};
			page.Buttons.Insert(0, copyButton);
		}
		WF.TaskDialog.ShowDialog(owner, page);
	}

	static bool predone = false;

	static void PrePop()
	{
		if (!predone)
		{
			WF.Application.EnableVisualStyles();
			predone = true;
		}
	}
}
