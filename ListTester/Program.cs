using System;
using System.Net.Http;
using System.Threading.Tasks;
using WMHCardGenerator.Core;
using WMHCardGenerator.Models;

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

                var httpClient = new HttpClient();
                CCInfoResponse info = null;
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri)
                    && DataHelper.TryGetListId(uri.ToString(), out string ccid))
                {
                    info = await DataHelper.GetConflictChamberList(httpClient, ccid);
                }
                else
                {
                    info = await DataHelper.ParseWarroomText(listText);
                }

                var output = await new PDFer(httpClient)
                     .Generate(info);

                Console.WriteLine();
                foreach (var link in output)
                    Console.WriteLine(link.PDFUrl);

                Console.ReadLine();
            }
            catch (Exception ex)
            {
            }
        }

        static string listText = @"War Room Army

Crucible Guard - Syvestro Rocketmen v3

Theme: Magnum Opus
75 / 75 Army


Aurum Adeptus Syvestro - WJ: +28
-    Aurum Ominus Alyce Marc - PC: 5
-    Aurum Ominus Alyce Marc (Big Alyce)
-    Toro - PC: 13 (Battlegroup Points Used: 13)
-    Toro - PC: 13 (Battlegroup Points Used: 13)
-    Liberator - PC: 10 (Battlegroup Points Used: 2)
-    Suppressor - PC: 13

Railless Interceptor - PC: 16

Trancer - PC: 0
Trancer - PC: 0
Trancer - PC: 3
Crucible Guard Mechanik - PC: 2
Hutchuk, Ogrun Bounty Hunter - PC: 0
Rhupert Carvolo, Piper of Ord - PC: 4

Crucible Guard Rocketmen - Leader & 9 Grunts: 15
-    Crucible Guard Rocketman Captain - PC: 4
Dragon's Breath Rocket - Gunner & 2 Grunts: 5
Dragon's Breath Rocket - Gunner & 2 Grunts: 0


THEME: Magnum Opus
---

GENERATED : 03/17/2020 22:23:47
BUILD ID : 2099.20-03-03";
    }
}
