using System;
using System.Threading.Tasks;
using WMHCardGenerator.Core;

namespace ListTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Enter List Url");
                string url = Console.ReadLine().Trim();

               var output =await new PDFer(new System.Net.Http.HttpClient())
                    .Generate(url);

                Console.WriteLine();
                foreach (var link in output)
                    Console.WriteLine(link.PDFUrl);

                Console.ReadLine();
            }
			catch(Exception ex)
			{
			}
        }
    }
}
