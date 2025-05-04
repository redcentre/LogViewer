using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using RCS.LogViewer.Model;

namespace RCS.LogViewer;

internal class MainConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string convarg = (string)parameter;
		if (convarg == "None")
		{
			return value == null;
		}
		if (convarg == "Some")
		{
			return value != null;
		}
		if (convarg == "Not")
		{
			return !(bool)value;
		}
		if (convarg == "LogTimestamp")
		{
			return AppUtility.LogTime((DateTime?)value);
		}
		if (convarg == "LogSeconds")
		{
			return (value is double d) ? d.ToString("F3") : null;
		}
		if (convarg == "LogErrorStack")
		{
			var stack = value as string;
			if (stack == null) return null;
			string[] lines = stack.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
			return $"{lines[0]} [{lines.Length - 1} omitted]";
		}
		if (convarg == "SomeVisible")
		{
			return value != null ? Visibility.Visible : Visibility.Collapsed;
		}
		if (convarg == "NoneVisible")
		{
			return value == null ? Visibility.Visible : Visibility.Collapsed;
		}
		string[] tokens = convarg.Split("|".ToCharArray());
		if (tokens[0] == "AnalTotal")
		{
			var items = value as IEnumerable<AnalItem>;
			return tokens[1] switch
			{
				"MinRK" => items?.Min(x => x.MaxRowKey),
				"MaxRK" => items?.Max(x => x.MaxRowKey),
				"MinTime" => AppUtility.LogTime(items?.Min(x => x.MinTime)),
				"MaxTime" => AppUtility.LogTime(items?.Max(x => x.MaxTime)),
				_ => null,
			};
		}
		if (tokens[0] == "EnumBool")
		{
			return tokens[1] == value.ToString();
		}
		if (tokens[0] == "IntEqVisible")
		{
			return (int?)value == int.Parse(tokens[1]) ? Visibility.Visible : Visibility.Collapsed;
		}
		throw new ArgumentException($"MainConverter.Convert {parameter}");
	}

	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string convarg = (string)parameter;
		string[] tokens = convarg.Split("|".ToCharArray());
		if (tokens[0] == "EnumBool")
		{
			var enumval = Enum.Parse(targetType, tokens[1]);
			return (bool)value ? enumval : Binding.DoNothing;
		}
		throw new ArgumentException($"MainConverter.ConvertBack {parameter}");
	}
}
