using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CLReader;

namespace CLReaderWeb
{
    public class CLSearchController : Controller
    {
        static readonly object _lock = new object();  //used to lock the json load and save portion
        ContextBag contextBag;

        public CLSearchController(ContextBag _contextBag)
        {
            contextBag = _contextBag;
        }

        [HttpGet("home")]
        [HttpGet("")]
        public IActionResult Index()
        {
            ViewBag.LastScanDate = contextBag.Matches.LastScanDate.ToString();
            ViewBag.MatchList = contextBag.Matches.GetMatchList();
            ViewBag.PromoteList = contextBag.Matches.GetPromotedList();

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

        [HttpGet("/api/promotetitle")]
        public object PromoteTitle(string title)
        {
            var encodedTitle = System.Net.WebUtility.UrlEncode(title);
            Console.WriteLine($"Now promoting title: {encodedTitle}");

            //Let's load the JSON, add the entry, and then resave
            lock (_lock)
            {
                string fileName="TitlesToPromote.json";
                List<string> promoteTitleList;
                if(!System.IO.File.Exists(fileName))
                {
                    System.IO.File.Create(fileName).Close();
                    promoteTitleList=new List<string>();
                }
                else
                {
                    promoteTitleList = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(fileName));
                }

                promoteTitleList.Add(encodedTitle);

                System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(promoteTitleList, Formatting.Indented));
            }

            //Redirecting to main page again
            return LocalRedirect("/");
        }     

        [HttpGet("/api/demotetitle")]
        public object DemoteTitle(string title)
        {
            var encodedTitle = System.Net.WebUtility.UrlEncode(title);
            Console.WriteLine($"Now demoting title: {encodedTitle}");

            //Let's load the JSON, add the entry, and then resave
            lock (_lock)
            {
                string fileName="TitlesToPromote.json";
                List<string> promoteTitleList;
                if(!System.IO.File.Exists(fileName))
                {
                    System.IO.File.Create(fileName).Close();
                    promoteTitleList=new List<string>();
                }
                else
                {
                    promoteTitleList = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(fileName));
                }

                promoteTitleList.Remove(encodedTitle);

                System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(promoteTitleList, Formatting.Indented));
            }

            //Redirecting to main page again
            return LocalRedirect("/");
        }            
    }
}