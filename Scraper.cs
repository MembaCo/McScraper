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

namespace McScraper
{
    /* Kazıyıcı için arabirim
     * Aşağıdaki 3 işlevi olacak */
    interface IScraper
    {
        List<string> getRecentPaste(); // Son yeni pastaları alın http://pastebin.com/archive
        string getPasteText(string url); // Asmak için belirtilen URL'de (/string) yapıştırın metnini alın http://pastebin.com/raw
        bool isKeywordPresent(string text, string regex); // Yapıştırmanın aradığımız anahtar kelimelerden birini içerip içermediğini kontrol edin
    }

    // Pastebin için sınıf tanımı
    class Scraper : IScraper
    {
        // web arayüzü değişiklikleri durumunda değiştirilecek url
        public string pasteBinUrl = "http://pastebin.com/archive";
        public string pasteBinArchiveUrl = "http://pastebin.com/archive";
        public string pasteBinRawUrl = "http://pastebin.com/raw";
        public int timeOut = 10000; // ms cinsinden ardışık istekler arasında beklemek için zaman (engellenmemek ve TOS'lara uymak için))
        private int idScraper;
        public static int numberOfScrapers = 0;
        ScrapingBrowser scraperBrowser;

        public Scraper()
        {
            // Son makarnaların sayfasını indirmek için bir tarayıcı oluşturuyorum
            this.scraperBrowser = new ScrapingBrowser();
            this.scraperBrowser.AllowAutoRedirect = true;
            this.scraperBrowser.AllowMetaRedirect = true;
            this.idScraper = numberOfScrapers;
            numberOfScrapers++;
        }

        // Son yeni pastaları alın http://pastebin.com/archive
        public List<string> getRecentPaste()
        {
            List<string> pastebins = new List<string>();
            // İsteği ben yapıyorum
            WebPage responsePage = scraperBrowser.NavigateToPage(new Uri("http://pastebin.com/archive"));
            // HTML sayfası öğesini temel sınıfla seçmek için HtmlAgilityPack'i kullanın
            // Bu, son macunlara ait linklerin yer aldığı tablo
            var pastebinsTable = responsePage.Html.CssSelect(".maintable").First();
            // Tablo üyelerini seç
            var row = pastebinsTable.SelectNodes("tr/td");
            // Her macun üç elementten oluşur
            // İlk öğe <a href="/page"> biçimindeki bağlantıdır
            // İkincisi, ne kadar süre önce oluşturuldu?
            // Üçüncü, eğer varsa, yapıştırıcıda kullanılan programlama dilini gösterir.
            // Bu nedenle 3 icnrementni ile döngü
            for (int i = 0; i < row.Count; i += 3)
            {
                // Bağlantıyı tırnakların içinden aldım
                string s = row[i].LastChild.OuterHtml;
                int start = s.IndexOf('"') + 1;
                int end = s.IndexOf('"', start);
                string actualLink = s.Substring(start, end - start);
                // Zamanım var (gelecekte faydalı olabilir)
                string timeAgo = row[i + 1].LastChild.OuterHtml;
                // Kullanılan dili alıyorum
                string languageUsed = row[i + 2].LastChild.OuterHtml;
                // Bağlantıyı listeye ekle
                pastebins.Add(actualLink);
            }
            return pastebins;
        }

        public string getPasteText(string url)
        {
            // Yapıştır macunu pastebin.com/raw/url adresinden saf metinden indirin
            string actualUrl = pasteBinRawUrl + url;
            try
            {
                WebPage responsePage = scraperBrowser.NavigateToPage(new Uri(actualUrl));
                return responsePage.Content;
            }
            catch
            {
                string response = "404 Not Found";
                return response;
            }


        }

        // Dizeyi normal ifadeyle karşılaştırmak için işlev
        public bool isKeywordPresent(string text, string regex)
        {
            Regex r = new Regex(regex);
            bool isKeywordHere = r.IsMatch(text);
            return isKeywordHere;
        }

        // İzlemeye başlamak için GÜZERGAH
        public void startScraping(string regex, Database mongoDB)
        {
            decimal i = 0;
            string paste;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Regex : {0}...", regex);
            Console.ResetColor();
            // kullanıcı ctrl + c basana kadar sonsuz döngü
            for (;;)
            {
                Console.WriteLine("");
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("İstek numarası {0}...", i + 1);
                Console.WriteLine("Engellenmeyi önlemek için 30s zaman aşımı Mevcuttur.");
                Console.WriteLine("Çıkmak için ctrl + c Tuşlarına Basınız");
                Console.ResetColor();
                // Her istek grubu arasında, biraz daha bekle
                Thread.Sleep(30000);
                List<string> pasteList = getRecentPaste();
                Console.WriteLine("Bulunan {0} adet PasteBin Seçildi !", pasteList.Count());          
                for (int j = 0; j < pasteList.Count(); j++)
                {
                    Thread.Sleep(timeOut); //çok fazla trafik oluşturmamak, istekler arasında beklemek
                    paste = getPasteText(pasteList[j]);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("\rŞuan {0} ila {1} Arasında PasteBin Taranıyor  ", j+1, pasteList.Count());
                    Console.ResetColor();
                    if (isKeywordPresent(paste, regex))
                    {
                        Console.WriteLine("");
                        string actualUrl = pasteBinRawUrl + pasteList[j];
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine("### Bir eşleşme bulundu! URL: {0} ###", actualUrl);
                        Console.ResetColor();
                        // Scoeprto'ya sahip olan ve nsotir arama kriterlerine karşılık gelen pastaları veritabanına girin
                        mongoDB.insertPaste(paste, actualUrl);
                        Console.WriteLine("");
                    }
                }
                // Kaç tane isteğimi hatırladığımı hatırlatıyorum
                i++;
            }

        }

    }
}
