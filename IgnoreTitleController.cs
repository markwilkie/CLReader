using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public class IgnoreTitleController : Controller
{
    static readonly object _lock = new object();  //used to lock the json load and save portion

    [HttpGet("/api/ignoretitle")]
    public object IgnoreTitle(string title)
    {
        title = System.Net.WebUtility.UrlDecode(title);
        Console.WriteLine($"Now ignoring title: {title}");

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

        //Redirecting to main page again
        return LocalRedirect("/SearchResults.html");
    }
}