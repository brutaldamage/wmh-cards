using System;
using System.Linq;

namespace WMHCardGenerator.Models
{
	public class CCInfoResponse
	{
		public string Url { get; set; }
		public string Faction { get; set; }
		public CCListInfoResponse[] Lists { get; set; }
	}

	public class CCListInfoResponse
	{
		public CCModelInfoResponse[] Models { get; set; }
		public string Theme { get; set; }
		public int Points { get; set; }

        public string[] Validation { get; set; }
	}

	public class CCModelInfoResponse : CCAttachedModelInfoResponse
	{
		public string Desc { get; set; }
		public CCAttachedModelInfoResponse[] Attached { get; set; }
	}

	public class CCAttachedModelInfoResponse
	{
		public string Name { get; set; }
		public string Cost { get; set; }
		public string Type { get; set; }
	}
}
