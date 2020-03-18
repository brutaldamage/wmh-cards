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

        public string WarRoomText { get; set; }

        public List<GeneratedData> PDFLinks { get; } = new List<GeneratedData>();

        async Task GeneratePDF()
        {
            try
            {
                this.PDFLinks.Clear();

                if(!string.IsNullOrEmpty(ConflictChamberList) &&
                    !string.IsNullOrEmpty(WarRoomText))
                {
                    await this.jsRuntime.InvokeVoidAsync("alert", "Please enter just a CC link or warroom text, not both");
                    return;
                }

                CCInfoResponse listInfo = null;
                if (!string.IsNullOrEmpty(ConflictChamberList)
                    && DataHelper.TryGetListId(ConflictChamberList, out string ccid))
                {
                    listInfo = await DataHelper.GetConflictChamberList(this.Http, ccid);
                }
                else
                {
                    listInfo = await DataHelper.ParseWarroomText(this.WarRoomText);
                }

                var links = await new PDFer(this.Http)
                    .Generate(listInfo);

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