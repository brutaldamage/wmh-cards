using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenScraping;
using OpenScraping.Config;
using WMHCardGenerator.Core;
using WMHCardGenerator.Models;

namespace WarmachineUniversity_Data_Retriever
{
	class Program
	{
		static async Task Main(string[] args)
		{
				List<DataModel> modelList = new List<DataModel>();

				using (var http = new HttpClient())
				{
					var lookupData = DataHelper.GetLookupData();

					List<WMUModel> models = new List<WMUModel>();
					foreach (var lookup in lookupData)
					{
						var modelName = lookup.Name;

						Console.WriteLine($"Getting info for '{modelName}'");

						var url = "https://warmachineuniversity.com/mw/index.php/";
						var encodedName = modelName.Replace(" ", "_")
							.Replace("&", "%26")
							.Replace("'", "%27");

						var html = await http.GetStringAsync($"{url}{encodedName}");
						var doc = new HtmlDocument();
						doc.LoadHtml(html);

						var title = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, \'firstHeading\')]")?.InnerText;

						var statTableCells = doc.DocumentNode.SelectNodes("//div[@id='mw-content-text']//table[3]//tr//table[1]//tr[not(contains(@style,'display: none'))]//td");

						Dictionary<string, string> stats = new Dictionary<string, string>();
						try
						{
							var text = statTableCells
								.Select(x => x.InnerText)
								.Where(x => !string.IsNullOrWhiteSpace(x))
								.ToList();

							// remove the "understanding the stat block" item
							text.Remove(text.Last());

							foreach (var t in text)
							{
								var index = text.IndexOf(t);
								// if even, its a key
								if (index % 2 == 0)
									stats.Add(t, null);
								// if odd, its a value
								else
									stats[stats.Last().Key] = t;
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Error: {ex.Message}");
						}

						modelList.Add(new DataModel
						{
							Name = title,
							StatBlock = stats,
							CardId = lookup.CardId
						});


						// break
						Console.WriteLine();
					}
				}

				var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var datapath = System.IO.Path.Combine(directory, Path.DirectorySeparatorChar.ToString(), "data.json");
				System.IO.File.WriteAllText(datapath, JsonConvert.SerializeObject(modelList));
			}
			catch (Exception ex)
			{
			}
		}
	}
}
