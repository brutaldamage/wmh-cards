using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenScraping;
using OpenScraping.Config;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;
using WMHCardGenerator.Core;
using WMHCardGenerator.Models;

namespace WarmachineUniversity_Data_Retriever
{
    class Program
    {
        const string rootUrl = "https://warmachineuniversity.com";
        static string url => $"{rootUrl}/mw/index.php";

        static async Task Main(string[] args)
        {
            try
            {
                List<DataModel> modelList = new List<DataModel>();

                using (var http = new HttpClient())
                {
                    var lookupData = DataHelper.GetLookupData();

                    List<WMUModel> models = new List<WMUModel>();
                    foreach (var lookup in lookupData)
                    {
                        var index = lookupData.IndexOf(lookup);
                        if (index != 13)
                            continue;

                        var modelName = lookup.Name;

                        Console.WriteLine($"Getting info for '{modelName}'");

                        var model = await TryGetWarmachineUniversityInfo_HtmlScrape(http, modelName);

                        model.CardId = lookup.CardId;

                        modelList.Add(model);

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

        public static async Task<DataModel> TryGetWarmachineUniversityInfo_HtmlScrape(HttpClient http, string modelName)
        {
            var encodedName = modelName.Replace(" ", "_")
                .Replace("&", "%26")
                .Replace("'", "%27");

            var response = await http.GetAsync($"{url}/{encodedName}");

            // model name might not match url, if so, try to find model via search
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // exeucte search page
                var searchurl = $"{url}?search={HttpUtility.UrlEncode(modelName)}";
                //https://warmachineuniversity.com/mw/index.php?search=Master+Preceptor+Orin+Midwinter
                response =  await http.GetAsync(searchurl);

                var nextUrl = await GetUrlFromSearchReults(response);

                response = await http.GetAsync(nextUrl);

                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await ParseModelPage(response, true);
                }

                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return await ParseModelPage(response);
            }

            throw new NotImplementedException();
        }

        public static async Task<string> GetUrlFromSearchReults(HttpResponseMessage response)
        {
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var href = doc.DocumentNode.SelectSingleNode("//ul[contains(@class, \'mw-search-results\')]//li//a").GetAttributes().FirstOrDefault(x => x.Name == "href")?.Value;

            return $"{rootUrl}{href}";
        }

        public static async Task<DataModel> ParseModelPage(HttpResponseMessage response, bool isRedirect = false)
        {
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, \'firstHeading\')]")?.InnerText;

            //
            var statTableBlocks = doc.DocumentNode.SelectNodes("//div[@id='mw-content-text']//table[@style='float: right; margin: 10px; margin-top: 0px; border: 1px solid black; border-collapse: collapse; background-color: #f2f2f2;']");

            if(statTableBlocks.Count > 2)
            {
                throw new Exception("don't know how to hanle more than 2 stat block tables");
            }

            // this is currently selecting ALL stat cells from all tables.
            var statTableCells = statTableBlocks
                .ElementAt(isRedirect ? 1 : 0)
                .SelectNodes("//table[@style='width: 130px; text-align: right; margin: 0px; border-collapse: collapse;']//tr[not(contains(@style,'display: none'))]//td");

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

            return new DataModel
            {
                Name = title,
                StatBlock = stats
            };
        }
    }
}
