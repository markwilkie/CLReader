using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Linq;
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
            //ViewBag.PromoteList = contextBag.Matches.GetPromotedList();
            //ViewBag.MatchList = contextBag.Matches.GetNonPromotedList(ViewBag.PromoteList);

            return View();
        }

        [HttpGet("/api/matches")]
        public string GetMatches()
        {
            //return JsonConvert.SerializeObject(contextBag.Matches.matchDict.Values.ToList());
            return JsonConvert.SerializeObject(contextBag.Matches.GetList());
        }

        [HttpGet("/api/marklinkclicked")]
        public object MarkLinkClicked(string title,string user)
        {
            title=System.Net.WebUtility.HtmlDecode(title);
            Console.WriteLine($"Marking link for {title} clicked for user {user}");
            contextBag.Matches.MarkClicked(title,user);

            //Redirecting to main page again
            return LocalRedirect("/");
        }        

        [HttpGet("/api/promotetitle")]
        public object PromoteTitle(string title,string user)
        {
            title=System.Net.WebUtility.HtmlDecode(title);
            Console.WriteLine($"Now promoting title: {title} for user {user}");
            contextBag.Matches.PromoteItem(title,user);

            //Redirecting to main page again
            return LocalRedirect("/");
        }     

        [HttpGet("/api/demotetitle")]
        public object DemoteTitle(string title,string user)
        {
            title=System.Net.WebUtility.HtmlDecode(title);
            Console.WriteLine($"Now demoting title: {title} for user {user}");

            if(contextBag.Matches.ItemIsPromoted(title))
                contextBag.Matches.DemoteItem(title,user);
            else
            {
                contextBag.Matches.RemoveItem(title);
                UpdateTitleToIgnoreJson(title);
            }

            //Redirecting to main page again
            return LocalRedirect("/");
        }   

        private void UpdateTitleToIgnoreJson(string title)
        {
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

                ignoreTitleList.Add(title);

                System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(ignoreTitleList, Formatting.Indented));
            }
        }                      
    }
}