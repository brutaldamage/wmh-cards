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

        private List<DataModel> CardData { get; }

        public PDFer(HttpClient http)
        {
            Http = http;

            this.CardData = DataHelper.GetLookupData();
        }

        public async Task<List<Models.GeneratedData>> Generate(string cclink)
        {
            List<Models.GeneratedData> PDFLinks = new List<Models.GeneratedData>();

            if (TryGetListId(cclink, out string ccid))
            {
                var listApiUrl = $"https://api.conflictchamber.com/list/{ccid}.JSON";

                var ccListInfo = JsonConvert.DeserializeObject<CCInfoResponse>(await Http.GetStringAsync(listApiUrl));

                string cardUrl = "http://cards.privateerpress.com?card_items_to_pdf=";

                foreach (var list in ccListInfo.Lists)
                {
                    var caster = list.Models.First(x => x.Type == "Warcaster" || x.Type == "Warlock" || x.Type == "Master");

                    var models = new List<ModelInfoWrapper>();

                    foreach (var model in list.Models)
                    {
                        AddModelName(models, model);

                        if (model.Attached != null)
                        {
                            foreach (var attached in model.Attached)
                            {
                                AddModelName(models, attached);
                            }
                        }
                    }

                    var grouped = models.GroupBy(x => x.Name);

                    string queryString = null;
                    foreach (var group in grouped)
                    {
                        var name = group.Key;

                        var card = group.FirstOrDefault()?.Card;
                        var model = group.FirstOrDefault()?.CCModel;

                        if (card != null)
                        {
                            int count = group.Count();

                            if (model.Type == "Solo")
                                count = 1;

                            queryString += $"${card.CardId},{count}";
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


        bool TryGetListId(string cclistUrl, out string ccId)
        {
            try
            {
                var uri = new Uri(cclistUrl);
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

        string CreateListDisplayText(string faction, CCListInfoResponse cCInfo)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(faction);
            sb.AppendLine($"Theme: {cCInfo.Theme}");
            sb.AppendLine($"Points: {cCInfo.Points}");

            var validation = cCInfo.Validation?.ToList() ?? new List<string>();
            if (validation.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("WARNING:");

                // display custom message for CID entries, remove it so it doesn't get printed twice.
                if (validation.Contains("This army contains CID entries."))
                {
                    validation.Remove("This army contains CID entries.");
                    sb.AppendLine("This army contains CID entries. Any CID entries will not have a card included in the PDF.");
                }

                if (validation.Count > 0)
                    foreach (var msg in validation)
                        sb.AppendLine(msg);

                sb.AppendLine();
            }

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

        void AddModelName(List<ModelInfoWrapper> models, CCAttachedModelInfoResponse model)
        {
            var name = model.Name;
            if (name.Contains("(min)"))
                name = name.Replace("(min)", string.Empty);
            else if (name.Contains("(max)"))
                name = name.Replace("(max)", string.Empty);

            // handle req point cards
            else if (name.EndsWith("(2)"))
                name = name.Replace("(2)", string.Empty);
            else if (name.EndsWith("(3)"))
                name = name.Replace("(3)", string.Empty);
            else if (name.EndsWith("(4)"))
                name = name.Replace("(4)", string.Empty);
            else if (name.EndsWith("(5)"))
                name = name.Replace("(5)", string.Empty);
            else if (name.EndsWith("(6)"))
                name = name.Replace("(6)", string.Empty);

            name = name.Trim();

            models.Add(new ModelInfoWrapper
            {
                Name = name,
                CCModel = model,
                // need to trim the name to match,
                // otherwise things like (min) & (max) will match
                Card = this.CardData.FirstOrDefault(x => x.Name == name),
            });
        }

        class ModelInfoWrapper
        {
            public string Name { get; set; }

            public CCAttachedModelInfoResponse CCModel { get; set; }

            public DataModel Card { get; set; }

            public bool IsAttached => this.CCModel?.GetType() == typeof(CCAttachedModelInfoResponse);
        }
    }
}