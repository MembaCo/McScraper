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
    interface IScraper
    {
        List<string> getRecentPaste(); // Son yeni Gönderileri alıyoruz http://pastebin.com/archive adresinden
        string getPasteText(string url); 
        bool isKeywordPresent(string text, string regex); // Gönderilerin aradığımız anahtar kelimelerden birini içerip içermediği kontrolü
    }

    // Pastebin için sınıf tanımı
    class Scraper : IScraper
    {
        // Web Adresinde Değişiklik olursa Burdan Ayar Yapılabilir.
        public string pasteBinUrl = "http://pastebin.com/archive";
        public string pasteBinArchiveUrl = "http://pastebin.com/archive";
        public string pasteBinRawUrl = "http://pastebin.com/raw";
        public int timeOut = 10000; // ms cinsinden istekler arasında bekleme süresi  (Engellenmemek için Ayarlama Yapılır. Şuanda 10Saniye))
        private int idScraper;
        public static int numberOfScrapers = 0;
        ScrapingBrowser scraperBrowser;

        public Scraper()
        {
            // Son Gönderileri Alabilmek için Tarayıcı Oluşturuyorum
            this.scraperBrowser = new ScrapingBrowser();
            this.scraperBrowser.AllowAutoRedirect = true;
            this.scraperBrowser.AllowMetaRedirect = true;
            this.idScraper = numberOfScrapers;
            numberOfScrapers++;
        }

        // Son yeni Gönderiler alınıyor http://pastebin.com/archive
        public List<string> getRecentPaste()
        {
            List<string> pastebins = new List<string>();

            WebPage responsePage = scraperBrowser.NavigateToPage(new Uri("http://pastebin.com/archive"));

            var pastebinsTable = responsePage.Html.CssSelect(".maintable").First();
            // Tablo üyelerini seç
            var row = pastebinsTable.SelectNodes("tr/td");
            // Her çekim için üç elementten oluşur
            // İlk öğe <a href="/page"> biçimindeki bağlantıdır
            // İkincisi, ne kadar süre önce oluşturuldu?
            // Üçüncü, eğer varsa, gönderide kullanılan programlama dilini gösterir.
            // Bu nedenle 3 element ile döngü oluşturdum
            for (int i = 0; i < row.Count; i += 3)
            {
                // Bağlantıyı tırnakların içinden aldım
                string s = row[i].LastChild.OuterHtml;
                int start = s.IndexOf('"') + 1;
                int end = s.IndexOf('"', start);
                string actualLink = s.Substring(start, end - start);

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
            // Gelen Verinin pastebin.com/raw/url adresinden saf metin olarak indirdik
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

        // İzlemeye başlamak için
        public void startScraping(string regex, Database mongoDB)
        {
            decimal i = 0;
            string paste;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Regex : {0}...", regex);
            Console.ResetColor();
            // kullanıcı ctrl + c basana kadar sonsuz döngüde dönüyor
            //TODO: Burada Sonsuz Döngü Olduğu İçin CPU Sıkışıyor Düzeltilecek
            for (;;)
            {
                Console.WriteLine("");
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("İstek numarası {0}...", i + 1);
                Console.WriteLine("Engellenmeyi önlemek için 30s zaman aşımı Mevcuttur.");
                Console.WriteLine("Çıkmak için ctrl + c Tuşlarına Basınız");
                Console.ResetColor();
                // Her istek grubu arasında, biraz bekle
                Thread.Sleep(30000);
                List<string> pasteList = getRecentPaste();
                Console.WriteLine("Bulunan {0} adet PasteBin Seçildi !", pasteList.Count());          
                
                for (int j = 0; j < pasteList.Count(); j++)
                {
                    Thread.Sleep(timeOut); //çok fazla trafik oluşturmamak için istekler arasında bekle
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
                        // Arama kriterlerine karşılık gelen gönderileri veritabanına girin
                        mongoDB.insertPaste(paste, actualUrl);
                        Console.WriteLine("");
                    }
                }
                // Kaç tane istek gönderdiğimi Not alıyorum
                i++;
            }

        }

    }
}
