using System;
using Microsoft.AspNetCore.Components;

namespace WMHCardGenerator.Pages
{
	public partial class Feedback
	{

		[Inject]
		public NavigationManager Navigation { get; set; }

		public string Email { get; set; }

		public string Name { get; set; }

		public string Comments { get; set; }

		void OnValidSubmit()
		{
			Navigation.NavigateTo("/");
		}
	}
}
