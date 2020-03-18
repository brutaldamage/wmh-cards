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
    public class DataHelper
    {
        public static List<DataModel> GetLookupData()
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

        public static bool TryGetListId(string cclistUrl, out string ccId)
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


        public static async Task<CCInfoResponse> GetConflictChamberList(HttpClient http, string ccid)
        {
            var listApiUrl = $"https://api.conflictchamber.com/list/{ccid}.JSON";

            var ccListInfo = JsonConvert.DeserializeObject<CCInfoResponse>(await http.GetStringAsync(listApiUrl));

            return ccListInfo;
        }

        public static async Task<CCInfoResponse> ParseWarroomText(string text)
        {
            var info = new CCInfoResponse()
            {
                Lists = new CCListInfoResponse[]
                {
                    new CCListInfoResponse()
                }
            };

            var reader = new StringReader(text);

            string line = null;
            int lineCount = 0;
            bool end = false;

            List<CCModelInfoResponse> models = new List<CCModelInfoResponse>();

            do
            {
                line = await reader.ReadLineAsync();

                if (line != null)
                {
                    lineCount++;
                    if (lineCount == 1)
                    {
                        if (line != "War Room Army")
                            throw new InvalidDataException("List text must start with 'War Room Army'. Do not modify the text after copying from WarRoom2.");
                    }
                    else if (lineCount == 3)
                    {
                        // Crucible Guard - list name
                        info.Faction = line.Split(" - ")[0];
                    }
                    else if (lineCount == 5)
                    {
                        // Theme: Magnum Opus
                        info.Lists[0].Theme = line.Substring(7);
                    }
                    else if (lineCount == 6)
                    {
                        int.TryParse(line.Split(" / ")[0], out int points);
                        info.Lists[0].Points = points;
                    }
                    else if (lineCount >= 9)
                    {
                        // empty line between stuff
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        // end of list text
                        else if (line == "---" || line.StartsWith("THEME:"))
                        {
                            end = true;
                            continue;
                        }
                        // regular model (not attached)
                        else if (!line.StartsWith("-    "))
                        {
                            string type = null;
                            string l = line;

                            var costString = l.Split(" ").LastOrDefault();
                            bool costIntSuccess = int.TryParse(costString, out int costInt);

                            if (l.Contains("WJ:"))
                            {
                                // infernals uses WJ points
                                type = info.Faction == "Infernals" ? "Master" : "Warcaster";
                            }
                            else if (l.Contains("WB:"))
                            {
                                type = "Warlock";
                            }
                            else if (l.Contains("PC:"))
                            {
                                // not sure a better way to do this with out model types in the lookup
                                type = costIntSuccess && costInt >= 10 ? "Battle Engine/Structure" : "Solo";
                            }
                            else
                            {
                                type = "Unit"; // assume unit if doesn't contain WJ/WB/PC
                            }

                            var model = new CCModelInfoResponse()
                            {
                                Name = l.Split(" - ").FirstOrDefault(),
                                Type = type,
                                Cost = costString?.Trim()
                            };

                            models.Add(model);
                        }
                        // attached models, caster/command attachments & jacks/beasts
                        else if (line.StartsWith("-    "))
                        {
                            var l = line.Replace("-    ", string.Empty);

                            // (Battlegroup Points Used XX)
                            if (l.Contains("Battlegroup Points Used"))
                            {
                                l = l.RemoveBetween('(', ')')
                                    .Replace("()", string.Empty);
                            }

                            var parent = models.Last();

                            if (parent.Attached == null)
                                parent.Attached = new CCAttachedModelInfoResponse[0];

                            var attached = parent.Attached.ToList();

                            var costString = l.Split(": ").LastOrDefault();

                            bool costIntSuccess = int.TryParse(costString, out int costInt);

                            if (!costIntSuccess)
                                continue;

                            var model = new CCAttachedModelInfoResponse
                            {
                                Name = l.Split(" - ").FirstOrDefault(),
                                Type = parent.Type == "Unit" ? "Command Attachement"
                                    : parent.Type == "Warcaster" ? "Warjack"
                                    : parent.Type == "Warlock" ? "Warbeast"
                                    : parent.Type == "Master" ? "Horror"
                                    : null,
                                Cost = l.Split(": ").LastOrDefault()?.Trim()
                            };

                            // assume solo attacement for caster if 5 or less.
                            if ((parent.Type == "Warcaster" || parent.Type == "Warlock" || parent.Type == "Master")
                                && costIntSuccess && costInt <= 5)
                            {
                                model.Type = "Solo";
                            }

                            attached.Add(model);

                            parent.Attached = attached.ToArray();
                        }
                    }
                }
            }
            while (!end && line != null);

            info.Lists[0].Models = models.ToArray();

            return info;
        }
    }
}
