using System;
using System.Collections.Generic;
using System.IO;
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
	}
}
