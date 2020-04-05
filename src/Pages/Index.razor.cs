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
		public DataHelper DataHelper { get; set; }

		[Inject]
		public PDFer PDFer { get; set; }

        [Inject]
        public IJSRuntime jsRuntime { get; set; }

        public string ConflictChamberList { get; set; }

        public string WarRoomText { get; set; }

        public List<GeneratedData> PDFLinks { get; } = new List<GeneratedData>();

        void ClearInputs()
        {
            this.ConflictChamberList = null;
            this.WarRoomText = null;
            this.PDFLinks.Clear();

            this.StateHasChanged();
        }

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

                if(!string.IsNullOrWhiteSpace(WarRoomText))
                {
                    listInfo = await DataHelper.ParseWarroomText(this.WarRoomText);
                }
                else if (!string.IsNullOrEmpty(ConflictChamberList)
                    && DataHelper.TryGetListId(ConflictChamberList, out string ccid))
                {
                    listInfo = await DataHelper.GetConflictChamberList(ccid);
                }
                else
                {
                    throw new Exception("Please enter a valid conflict chamber permalink or WarRoom list text.");
                }

                var links = await this.PDFer.Generate(listInfo);

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