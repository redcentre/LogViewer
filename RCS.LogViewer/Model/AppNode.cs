using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RCS.LogViewer.Model;

public enum NodeType
{
	Subscription,
	StorageAccount,
	Table
}

// NOTE An empty constructor and all properties must be both get/set to allow Json serialization.

sealed class AppNode : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public AppNode()
	{
	}

	public AppNode(NodeType type, int id, string label, object? data)
	{
		Type = type;
		Id = id;
		Label = label;
		if (data is AccountData account) Account = account;
		else if (data is string tableName) TableName = tableName;
	}

	public int Id { get; set; }

	public NodeType Type { get; set; }

	public AppNode? Parent { get; set; }

	public AccountData? Account { get; set; }

	public string? TableName { get; set; }

	public void AddChild(AppNode node)
	{
		Children ??= [];
		node.Parent = this;
		Children.Add(node);
	}

	ObservableCollection<AppNode>? _children;
	public ObservableCollection<AppNode>? Children
	{
		get => _children;
		set
		{
			if (_children != value)
			{
				_children = value;
				OnPropertyChanged(nameof(Children));
			}
		}
	}

	string? _label;
	public string? Label
	{
		get => _label;
		set
		{
			if (_label != value)
			{
				_label = value;
				OnPropertyChanged(nameof(Label));
			}
		}
	}

	bool _isExpanded;
	public bool IsExpanded
	{
		get { return _isExpanded; }
		set
		{
			if (_isExpanded != value)
			{
				_isExpanded = value;
				OnPropertyChanged(nameof(IsExpanded));
			}
		}
	}

	bool _isSelected;

	public bool IsSelected
	{
		get { return _isSelected; }
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChanged(nameof(IsSelected));
			}
		}
	}

	public override string ToString() => $"AppNode({Type},{Id},{Label},{Parent?.Id},{(IsSelected ? "\u261b" : "")},{(IsExpanded ? "\u25bc" : "")},{Account},{TableName},{Children?.Count})";
}
