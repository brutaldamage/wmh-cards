using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WMHCardGenerator.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WMHCardGenerator.Core;

namespace WMHCardGenerator.Pages
{
	public partial class Index
	{
		[Inject]
		public HttpClient Http { get; set; }

		[Inject]
		public IJSRuntime jsRuntime { get; set; }

		public string ConflictChamberList { get; set; }

		//public Dictionary<string, string> PDFLinks { get; } = new Dictionary<string, string>();
		public List<GeneratedData> PDFLinks { get; } = new List<GeneratedData>();

		async Task GeneratePDF()
		{
			try
			{
				this.PDFLinks.Clear();

				var links = await new PDFer(this.Http)
					.Generate(this.ConflictChamberList);

				PDFLinks.AddRange(links);

			}
			catch (Exception ex)
			{
				await jsRuntime.InvokeVoidAsync("alert", ex.Message);
			}
			finally
			{
				this.StateHasChanged();
			}
		}
	}
}