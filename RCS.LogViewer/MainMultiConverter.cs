using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RCS.LogViewer.Model;

namespace RCS.LogViewer;

internal class MainMultiConverter : IMultiValueConverter
{
	public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		string convarg = (string)parameter;
		if (convarg == "TreeIcon")
		{
			var type = values[0] as NodeType?;
			var expand = values[1] as bool?;
			if (type == NodeType.Subscription) return expand == true ? Images.NodeFolderOpen : Images.NodeFolderClosed;
			else if (type == NodeType.StorageAccount) return Images.NodeStorageAccount;
			else if (type == NodeType.Table) return Images.NodeTable;
			else return Images.NodeUnknown;
		}
		string[] tokens = convarg.Split("|".ToCharArray());
		if (tokens[0] == "EnumVisibleFirst")
		{
			return tokens[1] == values[0].ToString() ? Visibility.Visible : Visibility.Hidden;
		}
		if (tokens[0] == "EnumBoolFirst")
		{
			return tokens[1] == values[0].ToString();
		}
		throw new NotImplementedException($"MainMultiConverter.Convert {parameter}");
	}

	public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException($"MainMultiConverter.ConvertBack {parameter}");
	}
}
