using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WMHCardGenerator.Models;

namespace WMHCardGenerator.Core
{
    public class PDFer
    {
        private HttpClient Http { get; }

        public PDFer(HttpClient http)
        {
            Http = http;
        }

        public async Task<List<Models.GeneratedData>> Generate(string cclink)
        {
            List<Models.GeneratedData> PDFLinks = new List<Models.GeneratedData>();

            if (GetListId(cclink, out string ccid))
            {
                var url = $"https://api.conflictchamber.com/list/{ccid}.JSON";

                var ccListInfo = JsonConvert.DeserializeObject<CCInfoResponse>(await Http.GetStringAsync(url));

                var data = GetLookupData();
                string cardUrl = "http://cards.privateerpress.com?card_items_to_pdf=";

                foreach (var list in ccListInfo.Lists)
                {
                    var caster = list.Models.First(x => x.Type == "Warcaster" || x.Type == "Warlock" || x.Type == "Master");

                    List<string> modelNames = new List<string>();
                    foreach (var model in list.Models)
                    {
                        AddModelName(modelNames, model.Name);

                        if (model.Attached != null)
                        {
                            foreach (var attached in model.Attached)
                            {
                                AddModelName(modelNames, attached.Name);
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

                    PDFLinks.Add(new GeneratedData
                    {
                        CasterName = caster.Name,
                        PDFUrl = $"{cardUrl}{queryString}",
                        ListOutput = CreateListDisplayText(ccListInfo.Faction, list)
                    });
                }
            }

            else
            {
                throw new Exception("Unable to parse conflict chamber link. Make sure it is a valid link");
            }
            return PDFLinks;
        }

        public bool GetListId(string cclist, out string ccId)
        {
            try
            {
                var uri = new Uri(cclist);
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

        public string CreateListDisplayText(string faction, CCListInfoResponse cCInfo)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(faction);
            sb.AppendLine();
            sb.AppendLine($"Theme: {cCInfo.Theme}");
            sb.AppendLine($"Points: {cCInfo.Points}");

            var index = 0;
            foreach (var model in cCInfo.Models)
            {
                if (index == 0)
                {
                    sb.AppendLine($"[{model.Desc}] {model.Name} [{model.Cost}]");
                }
                else
                {
                    sb.AppendLine($"{model.Name} [{model.Cost}]");
                }

                if (model.Attached != null && model.Attached.Length > 0)
                {
                    foreach (var attached in model.Attached)
                        sb.AppendLine($"- {attached.Name} [{attached.Cost}]");
                }

                index++;
            }

            return sb.ToString();
        }

        List<DataModel> GetLookupData()
        {
            var assembly = typeof(PDFer).Assembly;
            var resourceName = "WMHCardGenerator.Core.data.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();

                return JsonConvert.DeserializeObject<List<DataModel>>(result);
            }
        }

        void AddModelName(List<string> modelNames, string name)
        {
            var n = name;
            if (name.Contains("(min)"))
                n = name.Replace("(min)", string.Empty);
            else if (name.Contains("(max)"))
                n = name.Replace("(max)", string.Empty);

            // handle req point cards
            else if (name.EndsWith("(2)"))
                n = name.Replace("(2)", string.Empty);
            else if (name.EndsWith("(3)"))
                n = name.Replace("(3)", string.Empty);
            else if (name.EndsWith("(4)"))
                n = name.Replace("(4)", string.Empty);
            else if (name.EndsWith("(5)"))
                n = name.Replace("(5)", string.Empty);
            else if (name.EndsWith("(6)"))
                n = name.Replace("(6)", string.Empty);

            modelNames.Add(n.Trim());
        }
    }
}