using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

using System.Threading.Tasks;
using CCCardMaker.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCCardMaker.Pages
{
	public partial class Index
	{
		[Inject]
		public HttpClient Http { get; set; }

		[Inject]
		public IJSRuntime jsRuntime { get; set; }

		public string ConflictChamberList { get; set; }

		public Dictionary<string, string> PDFLinks { get; } = new Dictionary<string, string>();

		async Task GeneratePDF()
		{
			if(GetListId(out string ccid))
			{
				var url = $"https://api.conflictchamber.com/list/{ccid}.JSON";

				var ccListInfo = await Http.GetJsonAsync<CCInfoResponse>(url);

				var data = await Http.GetJsonAsync<DataModel[]>("data/data.json");
				string cardUrl = "http://cards.privateerpress.com?card_items_to_pdf=";//$4402,1$4399,1";

				foreach (var list in ccListInfo.Lists)
				{
					var caster = list.Models.First(x => x.Type == "Warcaster" || x.Type == "Warlock" || x.Type == "Master");

					List<string> modelNames = new List<string>();
					foreach (var model in list.Models)
					{
						modelNames.Add(model.Name);
						if (model.Attached != null)
						{
							foreach (var attached in model.Attached)
							{
								modelNames.Add(attached.Name);
							}
						}
					}

					var grouped = modelNames.GroupBy(x => x);
					string queryString = null;

					var distinct = grouped.Select(x => x.Key);

					foreach (var group in grouped)
					{
						var value = group.Select(x => x).Where(x => !string.IsNullOrEmpty(x));

						var card = data.FirstOrDefault(x => x.Name.Trim().ToLower() == group.Key.Trim().ToLower() && !string.IsNullOrEmpty(x.CardId));

						if (card != null)
						{
							queryString += $"${card.CardId},{group.Count()}";
						}
					}

					this.PDFLinks.Add(caster.Name, $"{cardUrl}{queryString}");
					this.StateHasChanged();
				}
			}
			else
			{
				await jsRuntime.InvokeVoidAsync("alert", "Unable to parse conflict chamber link. Make sure it is a valid link");
			}
		}

		public bool GetListId(out string ccId)
		{
			try
			{
				var uri = new Uri(ConflictChamberList);
				var pathAndQuery = uri.PathAndQuery;
				ccId = pathAndQuery.Substring(2);

				return true;
			}
			catch (Exception)
			{
				ccId = null;
				return false;
			}
		}
	}
}