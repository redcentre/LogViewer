using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using CommunityToolkit.Mvvm.ComponentModel;
using Orthogonal.Common.Basic;
using Orthogonal.NSettings;
using RCS.Azure.StorageAccount.Shared;
using RCS.LogViewer.Extensions;
using RCS.LogViewer.Model;

namespace RCS.LogViewer;

sealed partial class MainController : ObservableObject
{
	public ISettingsProcessor SettingStore { get; }
	readonly string CacheFilename = "LogViewer-V1";
	readonly JsonSerializerOptions NavSerOpts = new() { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve, PropertyNameCaseInsensitive = false };
	string[]? stringColumnNames;

	public MainController()
	{
		SettingStore = new RegistrySettings();
		ActiveSettings = new AppSettings();
		ActiveSettings!.SubscriptionId = SettingStore.Get(null, nameof(ActiveSettings.SubscriptionId));
		ActiveSettings!.TenantId = SettingStore.Get(null, nameof(ActiveSettings.TenantId));
		ActiveSettings!.ApplicationId = SettingStore.Get(null, nameof(ActiveSettings.ApplicationId));
		ActiveSettings!.ClientSecret = SettingStore.Get(null, nameof(ActiveSettings.ClientSecret));
		_searchSearchRowsMaximum = SettingStore.GetInt(null, nameof(SearchRowsMaximum), 500);
		ResetApp();
	}

	#region Public API

	public void TimeTick()
	{
		StatusTime = DateTime.Now.ToString("HH:mm:ss");
	}

	public void SaveSettings()
	{
		foreach (var prop in ActiveSettings.WalkProps())
		{
			SettingStore.Put(null, prop.Name, prop.GetValue(ActiveSettings));
		}
	}

	public void ResetApp()
	{
		ClearDisplays();
		ObsNodes.Clear();
		SearchPK = null;
		SearchRawEventIds = null;
		SearchDateLow = DateTime.Now;
		UseSearchDateLow = true;
		SearchDateHigh = DateTime.Now;
		UseSearchDateHigh = false;
		_subutil = null;
		StatusMessage = "Application reset due to Azure settings change";
	}

	public async Task ScanSubscription(string scanParam)
	{
		using var busy = Busy.Show("Scanning the Azure subscription for tables\nThis may take some time.", this);
		ClearDisplays();
		ObsNodes.Clear();
		string? cacheJson = scanParam == "Force" ? null : SimpleFileCache.Get(CacheFilename, 30);
		if (cacheJson != null)
		{
			// Quick load the navigation tree from a cache of serialized nodes.
			AppNode[] nodes = JsonSerializer.Deserialize<AppNode[]>(cacheJson, NavSerOpts)!;
			foreach (var node in nodes)
			{
				ObsNodes.Add(node);
			}
			StatusMessage = "Subscription tree loaded from cache";
			return;
		}
		// A scan of the susbcription is required to build the navigation tree.
		var subnode = new AppNode(NodeType.Subscription, 0, "Subscription", null);
		ObsNodes.Add(subnode);
		await foreach (SubscriptionAccount sa in SubUtil.ListAccounts().Where(x => x.ConnectionString?.Length > 0))
		{
			var acc = new AccountData() { Name = sa.Name, Connect = sa.ConnectionString };
			var sanode = new AppNode(NodeType.StorageAccount, 0, sa.Name, acc);
			subnode.AddChild(sanode);
			subnode.IsExpanded = true;
			try
			{
				await foreach (string name in SubUtil.ListTableNamesAsync(sa.ConnectionString!))
				{
					var tnode = new AppNode(NodeType.Table, 0, name, name);
					sanode.AddChild(tnode);
					sanode.IsExpanded = true;
				}
			}
			catch (RequestFailedException ex) when (ex.Status == 501)
			{
				Trace.WriteLine($"|  **** {sa.Name} -> {ex.Message}");
			}
		}
		// Cache the navigation tree.
		string json = JsonSerializer.Serialize(ObsNodes.ToArray(), NavSerOpts);
		SimpleFileCache.Put(CacheFilename, json);
		StatusMessage = "Subscription tree loaded from Azure";
	}

	public async Task SearchTable()
	{
		using var busy = Busy.Show($"Searching table {_selectedNode!.TableName}\nThis may take some time.", this);
		TableClient tclient = MakeTableRef();
		// ┌────────────────────────────────┐
		// │  Build the table query.        │
		// └────────────────────────────────┘
		var qlist = new List<string>();
		if (SearchPK != null)
		{
			string cond = TableClient.CreateQueryFilter($"(PartitionKey eq {SearchPK})");
			qlist.Add(cond);
		}
		if (UseSearchDateLow)
		{
			string cond = TableClient.CreateQueryFilter($"(Timestamp ge {SearchDateLow})");
			qlist.Add(cond);
		}
		if (UseSearchDateHigh)
		{
			string cond = TableClient.CreateQueryFilter($"(Timestamp ge {SearchDateLow})");
			qlist.Add(cond);
		}
		if (SearchRawEventIds != null)
		{
			int[] ids = [.. SearchRawEventIds
				.Split(",; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				.Select(x => int.TryParse(x, out var id) ? id : int.MinValue)
				.Where(x => x != int.MinValue)
			];
			if (ids.Length > 0)
			{
				var conds = ids.Select(i => TableClient.CreateQueryFilter($"(EventId eq {i})"));
				var concat = "(" + string.Join(" or ", conds) + ")";
				qlist.Add(concat);
			}
		}
		string? where = null;
		if (qlist.Count > 0)
		{
			where = string.Join(" and ", qlist);
		}
		Trace.WriteLine($"#### fullQuery -> {where}");
		// ┌────────────────────────────┐
		// │  Run the table query.      │
		// └────────────────────────────┘
		var query = tclient.QueryAsync<TableEntity>(where).AsPages();
		var ds = new TableSet();
		int rowCounter = 0;
		string? contoken = null;
		// Outermost loop through all table row segments/blocks from the query.
		do
		{
			// Middle through all table entities in the segment.
			await foreach (var page in query)
			{
				foreach (TableEntity te in page.Values)
				{
					if (++rowCounter > _searchSearchRowsMaximum) break;
					var dsrow = ds.Table1.NewTable1Row();
					dsrow.PartitionKey = te.PartitionKey;
					dsrow.RowKey = te.RowKey;
					dsrow.Timestamp = te.Timestamp!.Value.LocalDateTime;
					// Inner loop through all the properties in an entity.
					foreach (var kvp in te)
					{
						Type t = kvp.Value.GetType();
						object? val = kvp.Value;
						if (val is DateTimeOffset dto)
						{
							// A DateTimeOffset must be converted into a DateTime for a DataRow column.
							val = dto.DateTime;
							t = typeof(DateTime);
						}
						DataColumn? col = ds.Table1.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName == kvp.Key);
						if (col == null)
						{
							// A table property name has been found that is not in the table.
							// The column with the correct type must be added to the table.
							col = new DataColumn(kvp.Key, t);
							ds.Table1.Columns.Add(col);
						}
						dsrow[col] = val;
					}
					ds.Table1.AddTable1Row(dsrow);
					dsrow.AcceptChanges();
				}
				contoken = page.ContinuationToken;
			}
		}
		while (contoken?.Length > 0);
		RowsView = ds.Table1.DefaultView;
		// Save the string column names for quick filtering.
		stringColumnNames = [.. ds.Table1.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(string)).Select(c => c.ColumnName)];
		StatusMessage = ds.Table1.Count > 0 ? $"Search result row count {ds.Table1.Count}" : "Search returned no matches";
	}

	/// <summary>
	/// Analysing a table requires reading the keys and times only (skip data properties) of every
	/// entity in the table, which could be quite slow from a desktop app due to network latency.
	/// </summary>
	public async Task AnalyseTable()
	{
		const int PKMax = 10000;
		string saname = _selectedNode!.Parent!.Account!.Name!;
		string tabname = _selectedNode!.TableName!;
		PurgeCountMessage = "Count unknown";
		PurgeDoneMessage = "No data";
		PurgeDate = DateTime.Now.AddMonths(-6);
		using var busy = Busy.Show($"Analyzing table {tabname}\nPlease wait - All data keys must be scanned.", this);
		var tclient = MakeTableRef();
		var query = tclient.QueryAsync<TableEntity>(select: ["PartitionKey", "RowKey", "Timestamp"]).AsPages();
		var list = new List<AnalItem>();
		AnalPkOverflowCount = 0;
		string? lastPK = null;
		string? minRowKey = null;
		string? maxRowKey = null;
		DateTime? minTimestamp = null;
		DateTime? maxTimestamp = null;
		AnalItem? item = null;
		string? contoken = null;
		do
		{
			await foreach (var page in query)
			{
				foreach (TableEntity entity in page.Values)
				{
					var entutc = entity.Timestamp!.Value.DateTime;
					if (entity.PartitionKey != lastPK)
					{
						item = new AnalItem(entity.PartitionKey, entity.RowKey, entutc);
						// It's possible that there are absurd numbers of unique PartitionKeys,
						// so a limit is set on how many are listed. Count overflows.
						if (list.Count == PKMax)
						{
							++AnalPkOverflowCount;
						}
						else
						{
							list!.Add(item);
						}
						lastPK = entity.PartitionKey;
						if (minRowKey == null || string.Compare(entity.RowKey, minRowKey, StringComparison.Ordinal) < 0)
						{
							minRowKey = entity.RowKey;
						}
						if (minTimestamp == null || entutc < minTimestamp)
						{
							minTimestamp = entutc;
						}
					}
					++item!.Count;
					item.MaxRowKey = entity.RowKey;
					item.MaxTime = entity.Timestamp!.Value.DateTime;
					if (maxRowKey == null || string.Compare(entity.RowKey, maxRowKey, StringComparison.Ordinal) > 0)
					{
						maxRowKey = entity.RowKey;
					}
					if (maxTimestamp == null || entutc > maxTimestamp)
					{
						maxTimestamp = entutc;
					}
				}
				contoken = page.ContinuationToken;
			}
		}
		while (contoken?.Length > 0);
		AnalItems = [.. list];
	}

	public async Task PurgeRowCount()
	{
		using var busy = Busy.Show("Counting old rows to delete", this, 1);
		var tclient = MakeTableRef();
		string where = TableClient.CreateQueryFilter($"(Timestamp lt {_purgeDate})");
		PurgeCount = await WalkEntities(tclient, where).CountAsync();
		SetPurgeCountMsg();
	}

	public async Task PurgeRowRun()
	{
		using var busy = Busy.Show("Deleting old rows", this, 2);
		var tclient = MakeTableRef();
		string where = TableClient.CreateQueryFilter($"(Timestamp lt {_purgeDate})");
		int delTotal = 0;
		await foreach (var tup in WalkEntities(tclient, where).ChunkAsync(100).Select((x, i) => new { Chunk = x, Ix = i }))
		{
			var groupqry = tup.Chunk.GroupBy(x => x.PartitionKey).Select(g => new { PK = g.Key, Entities = g.ToArray() }).ToArray();
			foreach (var grp in groupqry)
			{
				var trans = new List<TableTransactionAction>();
				trans.AddRange(grp.Entities.Select(r => new TableTransactionAction(TableTransactionActionType.Delete, r)));
				var result = await tclient.SubmitTransactionAsync(trans);
				int delCount = result.Value.Count(r => r.Status == (int)HttpStatusCode.NoContent);
				delTotal += delCount;
				Trace.WriteLine($"delete chunk {tup.Ix} PK {grp.PK} size {grp.Entities.Length} -> deleted {delCount} total {delTotal}");
			}
		}
		PurgeCount -= delTotal;
		SetPurgeCountMsg();
		PurgeDoneMessage = $"Delete count = {delTotal}";
	}

	#endregion

	#region Helper Members

	void SetPurgeCountMsg()
	{
		PurgeCountMessage = PurgeCount == 0 ? $"No rows were found to delete before {PurgeDate:D}" :
			PurgeCount == 1 ? $"One row was found to delete before {PurgeDate:D}" :
			$"{PurgeCount} rows were found to delete before {PurgeDate:D}";
	}

	void AfterQuickFilterChange()
	{
		if (_searchQuickFilter == null)
		{
			if (RowsView != null)
			{
				RowsView.RowFilter = null;
			}
			return;
		}
		// A SQL query filter is constructed with a comparison clause for every string column in
		// the table so that the quick value can be found contained in any string column's data.
		// The generated filter will be something like:
		// (PartitionKey LIKE '%web%') OR (RowKey LIKE '%web%') OR (Message LIKE '%web%') OR ... OR (ErrorMessage LIKE '%web%')
		char[] BadChars = ['_', '*', '%', '[', ']'];
		var sb = new StringBuilder();
		foreach (var c in _searchQuickFilter)
		{
			if (BadChars.Contains(c)) sb.Append($"[{c}]");
			else sb.Append(c);
		}
		sb.Replace("'", "''");
		string arg = sb.ToString();
		var query1 = stringColumnNames!.Select(x => $"({x} LIKE '%{arg}%')");
		RowsView!.RowFilter = string.Join(" OR ", query1);
		Trace.WriteLine($"#### RowFilter -> {RowsView.RowFilter}");
	}

	void ClearDisplays()
	{
		RowsView = null;
		PropsSource = null;
		SearchQuickFilter = null;
		SelectedAccountName = null;
		SelectedTableName = null;
		AnalItems = null;

	}

	async Task AfterSelectedNodeChanged()
	{
		ClearDisplays();
		if (_selectedNode?.Type == NodeType.Subscription)
		{
			// Nothing to do.
		}
		else if (_selectedNode?.Type == NodeType.StorageAccount)
		{
			// Get the details of the subscription for properties display.
			using var busy = Busy.Show($"Loading storage account {_selectedNode.Account!.Name}", this);
			SelectedAccountName = _selectedNode!.Account.Name;
			PropsSource = null;
			PropsSource = await SubUtil.ListAccounts().FirstOrDefaultAsync(a => a.Name == _selectedNode.Account!.Name);
			return;
		}
		else if (_selectedNode?.Type == NodeType.Table)
		{
			// The search controls will be enabled.
			SelectedAccountName = _selectedNode!.Parent!.Account!.Name!;
			SelectedTableName = _selectedNode!.TableName!;
		}
	}

	static async IAsyncEnumerable<ITableEntity> WalkEntities(TableClient tclient, string where)
	{
		var query = tclient.QueryAsync<TableEntity>(where).AsPages();
		string? contoken = null;
		do
		{
			await foreach (var page in query)
			{
				foreach (ITableEntity dte in page.Values)
				{
					yield return dte;
				}
				contoken = page.ContinuationToken;
			}
		}
		while (contoken?.Length > 0);
	}

	TableClient MakeTableRef() => new(_selectedNode!.Parent!.Account!.Connect, _selectedNode.TableName);

	SubscriptionUtility? _subutil;
	SubscriptionUtility SubUtil => LazyInitializer.EnsureInitialized(ref _subutil, () => new SubscriptionUtility(
		ActiveSettings.SubscriptionId!,
		ActiveSettings.TenantId!,
		ActiveSettings.ApplicationId!,
		ActiveSettings.ClientSecret!)
	);

	#endregion
}
