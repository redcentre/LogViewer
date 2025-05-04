using System.Windows;

namespace RCS.LogViewer;

public partial class AppSettingsWindow : Window
{
	public AppSettingsWindow()
	{
		InitializeComponent();
		Loaded += AppSettingsWindow_Loaded;
	}

	void AppSettingsWindow_Loaded(object sender, RoutedEventArgs e)
	{
		AppUtility.HideMinimizeAndMaximizeButtons(this);
	}

	void SettingsOK_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}
}
