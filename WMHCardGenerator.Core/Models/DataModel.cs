using System;
using Newtonsoft.Json;

namespace WMHCardGenerator.Models
{
	public class DataModel
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("card")]
		public string CardId { get; set; }
	}
}
