using System.Collections.Generic;

namespace RCS.LogViewer.Extensions;

static internal class AppExtensions
{
	public static async IAsyncEnumerable<IList<T>> ChunkAsync<T>(this IAsyncEnumerable<T> source, int size)
	{
		var chunk = new List<T>(size);
		await foreach (var item in source)
		{
			chunk.Add(item);
			if (chunk.Count == size)
			{
				yield return chunk;
				chunk = new List<T>(size);
			}
		}
		if (chunk.Count > 0)
		{
			yield return chunk;
		}
	}
}
