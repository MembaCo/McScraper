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
    // Sonuçları noSQL veritabanına eklemek için
    public class Database
    {
        MongoClient client;
        IMongoDatabase database;
        IMongoCollection<BsonDocument> collection;

        // Veritabanını başlat
        public Database()
        {
            this.client = new MongoClient("mongodb://localhost:27017");
            this.database = client.GetDatabase("McScraper");
            this.collection = database.GetCollection<BsonDocument>("Cekilenler");
        }

        // PasteBin eklemek için
        public void insertPaste(string text, string url)
        {
            var document = new BsonDocument
                                {
                                    { "url", url },
                                    { "text", text },
                                    { "date", DateTime.Now.ToString("yyyyMMddHHmmss") },
                                };
            try
            {
                collection.InsertOne(document);
            }
            catch
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Failed to write to database, is MongoDB running? Check it.");
                Console.ResetColor();
            }
        }
    }

}
