using Microsoft.AspNetCore.Components;
using System.IO.Compression;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;

namespace DDUP
{
	public class TemplateDatabase : ExportedTemplateDatabase
	{		
		public async Task Load(HttpClient http, string path)
		{
			await using var responseStream = await http.GetStreamAsync(path);
			using var gzip = new GZipStream(responseStream, CompressionMode.Decompress);
			using var output = new MemoryStream();

			await gzip.CopyToAsync(output);
			byte[] tdb = output.ToArray();
			LoadFromRaw(tdb);			
		}

	}
}
