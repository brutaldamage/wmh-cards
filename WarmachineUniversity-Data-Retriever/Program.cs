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
            List<DataModel> modelList = new List<DataModel>();
            string currentModelProcessing = null;
            try
            {
                using (var http = new HttpClient())
                {
                    var lookupData = DataHelper.GetLookupData();

                    List<WMUModel> models = new List<WMUModel>();
                    foreach (var lookup in lookupData)
                    {
                        var modelName = lookup.Name;

                        if (modelName != "Cultist Band")
                            continue;

                        currentModelProcessing = modelName;

                        Console.WriteLine($"Getting info for '{modelName}'");

                        var model = await TryGetWarmachineUniversityInfo_HtmlScrape(http, modelName);

                        model.CardId = lookup.CardId;

                        modelList.Add(model);

                        // break
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get info for model '{currentModelProcessing}'\n{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Console.WriteLine();

                var directory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var datapath = directory + Path.DirectorySeparatorChar.ToString() + "data.json";

                Console.WriteLine($"Writing data to file: {datapath}");

                System.IO.File.WriteAllText(datapath, JsonConvert.SerializeObject(modelList, Formatting.None));

                Console.WriteLine();
                Console.WriteLine("Press Any Key To Exit");
                Console.ReadLine();
            }
        }

        public static async Task<DataModel> TryGetWarmachineUniversityInfo_HtmlScrape(HttpClient http, string modelName)
        {
            try
            {
                var encodedName = modelName.Replace(" ", "_")
                    .Replace("&", "%26")
                    .Replace("'", "%27");

                var response = await http.GetAsync($"{url}/{encodedName}");

                // model name might not match url, if so, try to find model via search
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"Page not found for model '{modelName}'");

                    // exeucte search page
                    var searchurl = $"{url}?search={HttpUtility.UrlEncode(modelName)}";
                    //https://warmachineuniversity.com/mw/index.php?search=Master+Preceptor+Orin+Midwinter
                    response = await http.GetAsync(searchurl);

                    var nextUrl = await GetUrlFromSearchReults(response);

                    response = await http.GetAsync(nextUrl);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return await ParseModelPage(response, true);
                    }

                    return null;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return await ParseModelPage(response);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to find info for model: {modelName}.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                // return a default value
                return new DataModel
                {
                    Name = modelName
                };
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

        public static async Task<DataModel> ParseModelPage(HttpResponseMessage response, bool assumeAttachement = false)
        {
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var modelTypesBlock = doc.DocumentNode.SelectNodes("//div[@id='mw-content-text']//table[2]/tr[@valign='top']/td[2]");

            var title = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, \'firstHeading\')]")?.InnerText;            

            var statTableBlocks = doc.DocumentNode.SelectNodes("//div[@id='mw-content-text']//table[@style='float: right; margin: 10px; margin-top: 0px; border: 1px solid black; border-collapse: collapse; background-color: #f2f2f2;']");

            if (statTableBlocks.Count > 2)
            {
                throw new Exception("don't know how to hanle more than 2 stat block tables");
            }

            HtmlNode statTableBlock = null;
            // this is currently selecting ALL stat cells from all tables.
            if (assumeAttachement && statTableBlocks.Count > 1)
            {
                statTableBlock = statTableBlocks.ElementAt(1);
            }
            else
            {
                statTableBlock = statTableBlocks.ElementAt(0);
            }

            var statTableCells = statTableBlock
                // use . to start searching from seelcted node rather than top of document
                .SelectNodes(".//table[@style='width: 130px; text-align: right; margin: 0px; border-collapse: collapse;']//tr[not(contains(@style,'display: none'))]//td[not(@colspan='2')]");

            Dictionary<string, string> stats = new Dictionary<string, string>();

            var text = statTableCells
                .Select(x => x.InnerText)
                .ToList();

            bool skipNextLoop = false;
            for (int index = 0; index <= text.Count - 1; index++)
            {
                try
                {
                    var t = text[index];

                    if (skipNextLoop)
                    {
                        skipNextLoop = false;
                        continue;
                    }

                    // if even, its a value
                    if (index % 2 == 0)
                    {
                        string key = t;
                        if (key == "M.A.")
                            key = "MA";

                        if (string.IsNullOrWhiteSpace(key))
                        {
                            skipNextLoop = true;
                            continue;
                        }
                                                
                        key = key.Replace("&#160;", string.Empty);

                        stats.Add(key, null);
                    }
                    // if odd, its a key
                    else
                    {
                        var value = t;
                        //"19&#160;/&#160;20 (&#9733;)"
                        value = value
                            // handle slash for compound/stacking stats
                            .Replace("&#160;", string.Empty)
                            // remove the ★ designator
                            .Replace("(&#9733;)", string.Empty);

                        stats[stats.Last().Key] = value;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            return new DataModel
            {
                Name = title,
                StatBlock = stats
            };
        }
    }
}
