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
	string? _busyMessage;

	[ObservableProperty]
	int? _busyType;

	[ObservableProperty]
	AppSettings _activeSettings;

	[ObservableProperty]
	ObservableCollection<AppNode> _obsNodes = [];

	[ObservableProperty]
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
	ObservableCollection<AnalItem>? _analItems;

	partial void OnAnalItemsChanged(ObservableCollection<AnalItem>? value)
	{
		MinAnalRowKey = AnalItems?.Min(x => x.MinRowKey);
		OnPropertyChanged(nameof(MinAnalRowKey));
		MaxAnalRowKey = AnalItems?.Max(x => x.MaxRowKey);
		OnPropertyChanged(nameof(MaxAnalRowKey));
		MinAnalTime = AnalItems?.Min(x => x.MinTime);
		OnPropertyChanged(nameof(MinAnalTime));
		MaxAnalTime = AnalItems?.Max(x => x.MaxTime);
		OnPropertyChanged(nameof(MaxAnalTime));
	}

	public int AnalPkOverflowCount { get; private set; }
	public string? MinAnalRowKey { get; private set; }
	public string? MaxAnalRowKey { get; private set; }
	public DateTime? MinAnalTime { get; private set; }
	public DateTime? MaxAnalTime { get; private set; }

	DateTime _purgeDate;
	public DateTime PurgeDate
	{
		get => _purgeDate;
		set
		{
			if (_purgeDate != value)
			{
				_purgeDate = value;
				OnPropertyChanged(nameof(PurgeDate));
				PurgeCount = null;
				PurgeCountMessage = "Count unknown";
				PurgeDoneMessage = "No data";
			}
		}
	}

	[ObservableProperty]
	string? _purgeCountMessage;

	[ObservableProperty]
	int? _purgeCount;

	[ObservableProperty]
	string? _purgeDoneMessage;

	#endregion

	#region Table Search Parameters

	[ObservableProperty]
	bool _useSearchDateLow;

	[ObservableProperty]
	DateTime _searchDateLow;

	[ObservableProperty]
	bool _useSearchDateHigh;

	[ObservableProperty]
	DateTime _searchDateHigh;

	public int[] RowMaxPicks { get; } = [100, 200, 500, 1000, 2000, 5000, 10000];

	int _searchSearchRowsMaximum = 500;
	public int SearchRowsMaximum
	{
		get => _searchSearchRowsMaximum;
		set
		{
			if (_searchSearchRowsMaximum != value)
			{
				_searchSearchRowsMaximum = value;
				OnPropertyChanged(nameof(SearchRowsMaximum));
				SettingStore.Put(null, nameof(SearchRowsMaximum), value);
			}
		}
	}

	[ObservableProperty]
	string? _searchRawEventIds;

	[ObservableProperty]
	string? _searchPK;

	string? _searchQuickFilter;
	public string? SearchQuickFilter
	{
		get => _searchQuickFilter;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_searchQuickFilter != newval)
			{
				_searchQuickFilter = newval;
				OnPropertyChanged(nameof(SearchQuickFilter));
				AfterQuickFilterChange();
			}
		}
	}

	#endregion
}
