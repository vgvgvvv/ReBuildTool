using NiceIO;

namespace ReBuildTool.Service.Global;

/// <summary>
/// Minimal HTTP GET to a file. The first piece of networking in rbt that is not a shell-out to
/// git, so it deliberately stays small: one shared <see cref="HttpClient"/>, redirects followed by
/// the handler, and no retry policy - a package fetch that fails should say so rather than stall a
/// build behind silent retries.
/// </summary>
public static class Downloader
{
	// One client for the process: a new HttpClient per call leaks sockets in TIME_WAIT.
	private static readonly HttpClient Client = new()
	{
		Timeout = TimeSpan.FromMinutes(10)
	};

	/// <summary>
	/// Downloads <paramref name="url"/> to <paramref name="destination"/>, writing through a
	/// temporary file so an interrupted transfer never leaves a truncated artifact that a later
	/// run would mistake for a complete one.
	/// </summary>
	public static void Download(string url, NPath destination)
	{
		destination.EnsureParentDirectoryExists();
		var temporary = destination.Parent.Combine($"{destination.FileName}.partial");
		temporary.DeleteIfExists();

		try
		{
			using (var response = Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
				       .GetAwaiter().GetResult())
			{
				if (!response.IsSuccessStatusCode)
				{
					throw new IOException(
						$"downloading {url} failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
				}

				using var source = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
				using var target = File.Create(temporary.ToString());
				source.CopyTo(target);
			}

			destination.DeleteIfExists();
			temporary.Move(destination);
		}
		finally
		{
			temporary.DeleteIfExists();
		}
	}
}
