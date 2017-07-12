using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CLReader;

namespace CLReaderWeb
{
    public class IgnoreTitleController : Controller
    {
        static readonly object _lock = new object();  //used to lock the json load and save portion
        ContextBag contextBag;

        public IgnoreTitleController(ContextBag _contextBag)
        {
            contextBag = _contextBag;
        }

        [HttpGet("home")]
        [HttpGet("")]
        public IActionResult Index()
        {
            ViewBag.LastScanDate=contextBag.Matches.LastScanDate.ToString();
            ViewBag.MatchList= contextBag.Matches.GetMatchList();

            return View();
        }

        [HttpGet("/api/ignoretitle")]
        public object IgnoreTitle(string title)
        {
            var encodedTitle = System.Net.WebUtility.UrlEncode(title);
            Console.WriteLine($"Now ignoring title: {encodedTitle}");

            //Let's load the JSON, add the entry, and then resave
            lock (_lock)
            {
                string fileName="TitlesToIgnore.json";
                List<string> ignoreTitleList;
                if(!System.IO.File.Exists(fileName))
                {
                    System.IO.File.Create(fileName).Close();
                    ignoreTitleList=new List<string>();
                }
                else
                {
                    ignoreTitleList = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(fileName));
                }

                ignoreTitleList.Add(encodedTitle);

                System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(ignoreTitleList, Formatting.Indented));
            }

            //Remove from dictionary
            contextBag.Matches.RemoveItem(encodedTitle);

            //Redirecting to main page again
            return LocalRedirect("/");
        }
    }
}