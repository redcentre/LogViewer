using System;

namespace RCS.LogViewer.Model;

public sealed record AnalItem(string PK, string MinRowKey, DateTime? MinTime)
{
	public int Count { get; set; }
	public string? MaxRowKey { get; set; }
	public DateTime? MaxTime { get; set; }
}
