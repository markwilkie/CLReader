using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using CLReaderWeb;

namespace CLReader
{
    class Program
    {
        static void Main(string[] args)
        {
            //Dictionary to hold all matches (key is title)
            ContextBag contextBag = new ContextBag();

            //Start up web server to server resulting html files
            StartWebServer(contextBag);

            //Loop forever
            while(true)
            {
                contextBag.Matches = new Matches();

                //Read json with search term list (but first get default)
                SearchTerm defaultSearchTerm = JsonConvert.DeserializeObject<SearchTerm>(File.ReadAllText("CLSearchDefaultTerm.json"));
                List<SearchTerm> searchTermList = JsonConvert.DeserializeObject<List<SearchTerm>>(File.ReadAllText("CLSearchTerms.json"));

                Console.WriteLine($"{DateTime.Now}: Woke up and am searching again...");

                //Loop through and search for each entry in the search term list
                foreach(SearchTerm st in searchTermList)
                {
                    //Load empty slots w/ default search term
                    st.SetDefaults(defaultSearchTerm);

                    //Craigslist??
                    if(st.CLSearch != null)
                    {
                        Console.WriteLine($"CL: Price: {st.MinPrice}-{st.MaxPrice} Years: {st.MinYear}-{st.MaxYear} Search: {st.CLSearch}");

                        //Search all US and CA cities asked for
                        if(st.USCities != null && st.USCities.Length>0)
                            RSSScraper.SearchCL(st,st.USCities,".org",contextBag.Matches);
                        if(st.CACities != null && st.CACities.Length>0)
                            RSSScraper.SearchCL(st,st.CACities,".ca",contextBag.Matches);
                    }

                    //Samba??
                    if(st.SambaSearch != null)
                    {
                        Console.WriteLine($"Samba: Price: {st.MinPrice}-{st.MaxPrice} Years: {st.MinYear}-{st.MaxYear} Search: {st.SambaSearch}");                        
                        RSSScraper.SearchSamba(st,contextBag.Matches);
                    }

                    //Ebay??
                    if(st.EbaySearch != null)
                    {
                        Console.WriteLine($"Ebay: Price: {st.MinPrice}-{st.MaxPrice} Years: {st.MinYear}-{st.MaxYear} Search: {st.EbaySearch}");                        
                        RSSScraper.SearchEbay(st,contextBag.Matches);                        
                    }

                    //RV Trader
                    if(st.RVTraderSearch != null)
                    {
                        Console.WriteLine($"RVTrader: Price: {st.MinPrice}-{st.MaxPrice} Years: {st.MinYear}-{st.MaxYear} Search: {st.RVTraderSearch}");                        
                        RVTraderScraper.Scrape(st,contextBag.Matches);
                    }
                }
                
                //dump
                //matches.DumpItems();

                //wait for 3 hours during the day
                int hoursToWait = 2;
                if(DateTime.Now.Hour >= 22)
                    hoursToWait = 6;
                Console.WriteLine($"{DateTime.Now}: Waiting {hoursToWait} hours....");
                TimeSpan timeToWait = new TimeSpan(hoursToWait,0,0);
                Thread.Sleep(timeToWait);
            }
        }

        static void StartWebServer(ContextBag contextBag)
        {
            Console.WriteLine($"{DateTime.Now}: Starting Kestrel...");
            var webHostBuilder = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseStartup<Startup>()
                .ConfigureServices(services => services.AddSingleton<ContextBag>(contextBag))
                .UseUrls("http://*:5001");
            var host = webHostBuilder.Build();
            host.Start();
        }
    }
}
