using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace RCS.LogViewer;

public partial class App : Application
{
	static App()
	{
		Assembly asm = typeof(App).Assembly;
		Company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company;
		Product = asm.GetCustomAttribute<AssemblyProductAttribute>()!.Product;
		Title = asm.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;
		Description = asm.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description;
		Copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()!.Copyright;
		AsmVersion = asm.GetName().Version!;
		FileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
		HomeFolder = new DirectoryInfo(Path.GetDirectoryName(asm.Location)!);
	}

	public static string Company { get; set; }
	public static string Product { get; set; }
	public static string Title { get; set; }
	public static string Description { get; set; }
	public static string Copyright { get; set; }
	public static Version AsmVersion { get; set; }
	public static string FileVersion { get; set; }
	public static DirectoryInfo HomeFolder { get; set; }
}
