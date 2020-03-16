using System;
using System.Collections.Generic;

namespace WMHCardGenerator.Models
{
	public class DataModel
	{
		public string Name { get; set; }

		public string CardId { get; set; }

		public Dictionary<string, string> StatBlock { get; set; }
	}
}
