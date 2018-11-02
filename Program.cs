using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScrapySharp.Network;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System.Text.RegularExpressions;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Bson;

// to do: iş parçacığı ve çıkmak için q düğmesine basmak

namespace McScraper
{
    class Program
    {
        // Kolaylık sağlamak için dizelerin bir listesini yazdırın
        static void stampaLista(List<string> lista)
        {
            for (int i = 0; i < lista.Count(); i++)
            {
                Console.WriteLine(lista[i]);
            }
        }

        static void Main(string[] args)
        {
            string banner =
@" ############################################################################ 
 #                      Mc Scraper v1 - Pastebin Scraper                    # 
 #                         Developer Memba Co. 2018                         # 
 #                           Tüm Hakları Saklırdır.                         # 
 ############################################################################ 

";

            var mongoDB = new Database();
            var McScraper = new Scraper();
            // Kullanmak için düzenli ifadeyi seçin
            string regex = @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*";
            Console.SetWindowSize( Math.Min(78, Console.LargestWindowWidth), Math.Min(30, Console.LargestWindowHeight));
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(banner);
            Console.ResetColor();

            McScraper.startScraping(regex, mongoDB);
            Console.ReadKey();

        }
    }
}
