using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace CLReader
{
    class Program
    {
        static Matches matches;
        static void Main(string[] args)
        {
            //Start up web server to server resulting html files
            StartWebServer();

            //Loop forever
            while(true)
            {
                //Dictionary to hold all matches (key is title)
                matches = new Matches();

                //Read json with search term list (but first get default)
                SearchTerm defaultSearchTerm = JsonConvert.DeserializeObject<SearchTerm>(File.ReadAllText("CLSearchDefaultTerm.json"));
                List<SearchTerm> searchTermList = JsonConvert.DeserializeObject<List<SearchTerm>>(File.ReadAllText("CLSearchTerms.json"));

                Console.WriteLine($"{DateTime.Now}: Woke up and am searching again...");

                //Loop through and search
                foreach(SearchTerm st in searchTermList)
                {
                    //Load empty slots w/ default search term
                    st.SetDefaults(defaultSearchTerm);

                    Console.WriteLine($"Price: {st.MinPrice}-{st.MaxPrice} Years: {st.MinYear}-{st.MaxYear} Search: {st.CLSearch}");

                    //Craigslist??
                    if(st.CLSearch != null)
                    {
                        //Search all US and CA cities asked for
                        if(st.USCities != null && st.USCities.Length>0)
                            SearchCL(st,st.USCities,".org");
                        if(st.CACities != null && st.CACities.Length>0)
                            SearchCL(st,st.CACities,".ca");
                    }

                    //Samba??
                    if(st.SambaSearch != null)
                    {
                        SearchSamba(st);
                    }
                }
                
                //dump
                matches.DumpItems();

                //wait for 3 hours during the day
                int hoursToWait = 3;
                if(DateTime.Now.Hour >= 21)
                    hoursToWait = 9;
                Console.WriteLine($"{DateTime.Now}: Waiting {hoursToWait} hours....");
                TimeSpan timeToWait = new TimeSpan(hoursToWait,0,0);
                Thread.Sleep(timeToWait);
            }
        }

        static void StartWebServer()
        {
            Console.WriteLine($"{DateTime.Now}: Starting Kestrel...");
            var webHostBuilder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://*:5001");
            var host = webHostBuilder.Build();
            host.Start();
        }

        static void SearchCL(SearchTerm st,string cityList,string clDomain)
        {
            foreach(string city in cityList.Split(',').ToList())
            {
                string clURL = $"https://{city}.craigslist{clDomain}/search/sss?excats=5-2-13-22-2-24-1-4-19-1-1-1-1-1-1-3-6-10-1-1-1-2-10-1-1-1-1-1-4-1-7-1-1-1-1-7-1-1-1-1-1-1-1-2-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1-1&format=rss&query={st.CLSearch}&sort=rel";
                //Console.WriteLine($"City: <a href={clURL}>{city}</a><br>");

                FeedParser parser = new FeedParser();
                var items = parser.Parse(clURL,FeedType.RDF);
                foreach(Item item in items)
                {
                    //Check for title exclude keywords and chars
                    bool excludeKeywordsFlag = CheckTitle(st.ExcludeKeywords+" ",item.Title);
                    bool excludeCharsFlag = CheckTitle(st.ExcludeChars,item.Title);

                    //Get and check year (if it can parse)
                    bool yearFlag = false;
                    long year = GetYear(item.Title);
                    if((year == 0 || year >= st.MinYear) && year <= st.MaxYear)
                        yearFlag=true;

                    //Get and check price (zero if can't parse)
                    bool priceFlag = false;
                    long price = GetPrice(item.Title,';');
                    if(((price == 0 && !st.IgnoreZeroPrice) || price >= st.MinPrice) && price <= st.MaxPrice)
                        priceFlag=true;

                    //If we're still ok
                    if(excludeKeywordsFlag && excludeCharsFlag && yearFlag && priceFlag)
                    {
                        //Update item
                        item.Starred = st.Starred;
                        item.SearchString = st.CLSearch;
                        item.WebSite = "CraigsList";

                        //Add item to list
                        bool added=matches.AddItem(item);
                        //if(added)
                        //    Console.WriteLine($"{price} - <a href={item.Link}>{item.Title}</a><br>");
                    }
                }
            }
        }

        static void SearchSamba(SearchTerm st)
        {
            string clURL = $"https://www.thesamba.com/vw/classifieds/rss/search.php?type=text&stype=all&keywords={st.SambaSearch}&yearfrom={st.MinYear}&yearto={st.MaxYear}&section%5B%5D=55&section%5B%5D=75&country=USA&wanted=hide&sort=date&sort_order=DESC";
            
            FeedParser parser = new FeedParser();
            var items = parser.Parse(clURL,FeedType.RSS);
            foreach(Item item in items)
            {
                //Check for title exclude keywords and chars
                bool excludeKeywordsFlag = CheckTitle(st.ExcludeKeywords+" ",item.Title);
                bool excludeCharsFlag = CheckTitle(st.ExcludeChars,item.Title);

                //Get and check price (zero if can't parse)
                bool priceFlag = false;
                long price = GetPrice(item.Title,'$');
                if(((price == 0 && !st.IgnoreZeroPrice) || price >= st.MinPrice) && price <= st.MaxPrice)
                    priceFlag=true;

                //If we're still ok
                if(excludeKeywordsFlag && excludeCharsFlag && priceFlag)
                {
                    //Update item
                    item.Starred = st.Starred;
                    item.SearchString = st.SambaSearch;
                    item.WebSite = "Samba";

                    //Add item to list
                    bool added=matches.AddItem(item);
                    //if(added)
                    //    Console.WriteLine($"{price} - <a href={item.Link}>{item.Title}</a><br>");
                }
            }
        }

        static bool CheckTitle(string excludeString,string title)
        {
            bool excludeFlag = true;
            foreach(string exclKey in excludeString.Split(',').ToList())
            {
                if(exclKey.Length > 1 && title.ToLower().Contains(exclKey.ToLower()))
                {
                    excludeFlag=false;
                    break;
                }
            }     

            return excludeFlag;       
        }

        static long GetYear(string title)
        {
            long year = 0;
            try
            {
                string yearStr = title.Substring(0,title.IndexOf(' '));
                year = Convert.ToInt32(yearStr);
            }
            catch {}

            return year;
        }
        static long GetPrice(string title,char delimeter)
        {
            long price = 0;
            try
            {
                string priceStr = title.Substring(title.IndexOf(delimeter)+1);
                price = Convert.ToInt32(priceStr);
            }
            catch {}

            return price;
        }
    }
}
