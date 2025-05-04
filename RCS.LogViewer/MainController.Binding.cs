using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using RCS.LogViewer.Model;

namespace RCS.LogViewer;

partial class MainController
{
	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	#region Application

	string? _statusTime = "00:00:00";
	public string? StatusTime
	{
		get => _statusTime;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (newval != _statusTime)
			{
				_statusTime = value;
				OnPropertyChanged(nameof(StatusTime));
			}
		}
	}

	string? _statusMessage = "Loading...";
	public string? StatusMessage
	{
		get => _statusMessage;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (newval != _statusMessage)
			{
				_statusMessage = value;
				OnPropertyChanged(nameof(StatusMessage));
			}
		}
	}

	int _appFontSize = 13;
	public int AppFontSize
	{
		get => _appFontSize;
		set
		{
			if (value != _appFontSize)
			{
				_appFontSize = value;
				OnPropertyChanged(nameof(AppFontSize));
			}
		}
	}

	string? _busyMessage;
	public string? BusyMessage
	{
		get => _busyMessage;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_busyMessage != newval)
			{
				_busyMessage = newval;
				OnPropertyChanged(nameof(BusyMessage));
			}
		}
	}

	int? _busyType;
	public int? BusyType
	{
		get => _busyType;
		set
		{
			if (_busyType != value)
			{
				_busyType = value;
				OnPropertyChanged(nameof(BusyType));
			}
		}
	}

	#endregion

	AppSettings _activeSettings;
	public AppSettings ActiveSettings
	{
		get => _activeSettings;
		set
		{
			if (_activeSettings != value)
			{
				_activeSettings = value;
				OnPropertyChanged(nameof(ActiveSettings));
			}
		}
	}

	ObservableCollection<AppNode> _obsNodes = [];
	public ObservableCollection<AppNode> ObsNodes
	{
		get => _obsNodes;
		set
		{
			if (_obsNodes != value)
			{
				_obsNodes = value;
				OnPropertyChanged(nameof(ObsNodes));
			}
		}
	}

	AppNode? _selectedNode;
	public AppNode? SelectedNode
	{
		get => _selectedNode;
		set
		{
			if (_selectedNode != value)
			{
				_selectedNode = value;
				OnPropertyChanged(nameof(SelectedNode));
				Application.Current.Dispatcher.InvokeAsync(async () => await AfterSelectedNodeChanged());
			}
		}
	}

	string? _selectedAccountName;
	public string? SelectedAccountName
	{
		get => _selectedAccountName;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_selectedAccountName != newval)
			{
				_selectedAccountName = newval;
				OnPropertyChanged(nameof(SelectedAccountName));
			}
		}
	}

	string? _selectedTableName;
	public string? SelectedTableName
	{
		get => _selectedTableName;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_selectedTableName != newval)
			{
				_selectedTableName = newval;
				OnPropertyChanged(nameof(SelectedTableName));
				OnPropertyChanged(nameof(SelectedFullTableName));
			}
		}
	}

	public string? SelectedFullTableName => $"{_selectedAccountName} : {_selectedTableName}";

	DataView? _rowsView;
	public DataView? RowsView
	{
		get => _rowsView;
		set
		{
			if (_rowsView != value)
			{
				_rowsView = value;
				OnPropertyChanged(nameof(RowsView));
			}
		}
	}

	object? _propsSource;
	public object? PropsSource
	{
		get => _propsSource;
		set
		{
			if (_propsSource != value)
			{
				_propsSource = value;
				OnPropertyChanged(nameof(PropsSource));
			}
		}
	}

	#region Analysis

	ObservableCollection<AnalItem>? _analItems;
	public ObservableCollection<AnalItem>? AnalItems
	{
		get => _analItems;
		set
		{
			if (_analItems != value)
			{
				_analItems = value;
				OnPropertyChanged(nameof(AnalItems));
				MinAnalRowKey = _analItems?.Min(x => x.MinRowKey);
				OnPropertyChanged(nameof(MinAnalRowKey));
				MaxAnalRowKey = _analItems?.Max(x => x.MaxRowKey);
				OnPropertyChanged(nameof(MaxAnalRowKey));
				MinAnalTime = _analItems?.Min(x => x.MinTime);
				OnPropertyChanged(nameof(MinAnalTime));
				MaxAnalTime = _analItems?.Max(x => x.MaxTime);
				OnPropertyChanged(nameof(MaxAnalTime));
			}
		}
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

	string? _purgeCountMessage;
	public string? PurgeCountMessage
	{
		get => _purgeCountMessage;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_purgeCountMessage != newval)
			{
				_purgeCountMessage = newval;
				OnPropertyChanged(nameof(PurgeCountMessage));
			}
		}
	}

	int? _purgeCount;
	public int? PurgeCount
	{
		get => _purgeCount;
		set
		{
			if (_purgeCount != value)
			{
				_purgeCount = value;
				OnPropertyChanged(nameof(PurgeCount));
			}
		}
	}

	string? _purgeDoneMessage;
	public string? PurgeDoneMessage
	{
		get => _purgeDoneMessage;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_purgeDoneMessage != newval)
			{
				_purgeDoneMessage = newval;
				OnPropertyChanged(nameof(PurgeDoneMessage));
			}
		}
	}

	#endregion

	#region Table Search Parameters

	bool _useSearchDateLow;
	public bool UseSearchDateLow
	{
		get => _useSearchDateLow;
		set
		{
			if (_useSearchDateLow != value)
			{
				_useSearchDateLow = value;
				OnPropertyChanged(nameof(UseSearchDateLow));
			}
		}
	}

	DateTime _searchDateLow;
	public DateTime SearchDateLow
	{
		get => _searchDateLow;
		set
		{
			if (_searchDateLow != value)
			{
				_searchDateLow = value;
				OnPropertyChanged(nameof(SearchDateLow));
			}
		}
	}

	bool _useSearchDateHigh;
	public bool UseSearchDateHigh
	{
		get => _useSearchDateHigh;
		set
		{
			if (_useSearchDateHigh != value)
			{
				_useSearchDateHigh = value;
				OnPropertyChanged(nameof(UseSearchDateHigh));
			}
		}
	}

	DateTime _searchDateHigh;
	public DateTime SearchDateHigh
	{
		get => _searchDateHigh;
		set
		{
			if (_searchDateHigh != value)
			{
				_searchDateHigh = value;
				OnPropertyChanged(nameof(SearchDateHigh));
			}
		}
	}

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

	string? _searchSearchRawEventIds;
	public string? SearchRawEventIds
	{
		get => _searchSearchRawEventIds;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_searchSearchRawEventIds != newval)
			{
				_searchSearchRawEventIds = newval;
				OnPropertyChanged(nameof(SearchRawEventIds));
			}
		}
	}

	string? _searchPK;
	public string? SearchPK
	{
		get => _searchPK;
		set
		{
			string? newval = string.IsNullOrEmpty(value) ? null : value;
			if (_searchPK != newval)
			{
				_searchPK = newval;
				OnPropertyChanged(nameof(SearchPK));
			}
		}
	}

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
