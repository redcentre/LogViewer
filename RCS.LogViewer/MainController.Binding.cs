using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using RCS.LogViewer.Model;

namespace RCS.LogViewer;

partial class MainController
{
	[ObservableProperty]
	string? _statusTime = "00:00:00";

	[ObservableProperty]
	string? _statusMessage = "Loading...";

	[ObservableProperty]
	int _appFontSize = 13;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SearchTableCommand))]
	[NotifyCanExecuteChangedFor(nameof(PurgeRowCountCommand))]
	[NotifyCanExecuteChangedFor(nameof(PurgeRowRunCommand))]
	string ? _busyMessage;

	[ObservableProperty]
	int? _busyType;

	[ObservableProperty]
	AppSettings _activeSettings;

	[ObservableProperty]
	ObservableCollection<AppNode> _obsNodes = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SearchTableCommand))]
	AppNode? _selectedNode;

	partial void OnSelectedNodeChanged(AppNode? value) => Application.Current.Dispatcher.InvokeAsync(async () => await AfterSelectedNodeChanged());

	[ObservableProperty]
	string? _selectedAccountName;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SelectedFullTableName))]
	string? _selectedTableName;

	public string? SelectedFullTableName => $"{SelectedAccountName} : {SelectedTableName}";

	[ObservableProperty]
	DataView? _rowsView;

	[ObservableProperty]
	object? _propsSource;

	#region Analysis

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(PurgeRowCountCommand))]
	ObservableCollection<AnalItem>? _analItems;

	partial void OnAnalItemsChanged(ObservableCollection<AnalItem>? value)
	{
		MinAnalRowKey = AnalItems?.Min(x => x.MinRowKey);
		MaxAnalRowKey = AnalItems?.Max(x => x.MaxRowKey);
		MinAnalTime = AnalItems?.Min(x => x.MinTime);
		MaxAnalTime = AnalItems?.Max(x => x.MaxTime);
	}

	[ObservableProperty]
	int _analPkOverflowCount;

	[ObservableProperty]
	string? _minAnalRowKey;

	[ObservableProperty]
	string? _maxAnalRowKey;

	[ObservableProperty]
	DateTime? _minAnalTime;

	[ObservableProperty]
	DateTime? _maxAnalTime;

	[ObservableProperty]
	DateTime _purgeDate;

	partial void OnPurgeDateChanged(DateTime value)
	{
		PurgeCount = null;
		PurgeCountMessage = "Count unknown";
		PurgeDoneMessage = "No data";
	}

	[ObservableProperty]
	string? _purgeCountMessage;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(PurgeRowRunCommand))]
	int? _purgeCount;

	[ObservableProperty]
	string? _purgeDoneMessage;

	#endregion

	#region Table Search Parameters

	public int[] RowMaxPicks { get; } = [100, 200, 500, 1000, 2000, 5000, 10000];

	[ObservableProperty]
	bool _useSearchDateLow;

	[ObservableProperty]
	DateTime _searchDateLow;

	[ObservableProperty]
	bool _useSearchDateHigh;

	[ObservableProperty]
	DateTime _searchDateHigh;

	[ObservableProperty]
	int _searchRowsMaximum = 500;

	partial void OnSearchRowsMaximumChanged(int value) => SettingStore.Put(null, nameof(SearchRowsMaximum), value);

	[ObservableProperty]
	string? _searchRawEventIds;

	[ObservableProperty]
	string? _searchPK;

	[ObservableProperty]
	string? _searchQuickFilter;

	partial void OnSearchQuickFilterChanged(string? value) => AfterQuickFilterChange();

	#endregion
}
