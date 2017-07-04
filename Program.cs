using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CLReader
{
    class Program
    {
        static Matches matches;
        static void Main(string[] args)
        {
            //Dictionary to hold all matches (key is title)
            matches = new Matches();

            //Read json with search term list (but first get default)
            SearchTerm defaultSearchTerm = JsonConvert.DeserializeObject<SearchTerm>(File.ReadAllText("CLSearchDefaultTerm.json"));
            List<SearchTerm> searchTermList = JsonConvert.DeserializeObject<List<SearchTerm>>(File.ReadAllText("CLSearchTerms.json"));

            Console.WriteLine($"Search current as of {DateTime.Now}");

            //Loop through and search
            foreach(SearchTerm st in searchTermList)
            {
                //Load empty slots w/ default search term
                st.SetDefaults(defaultSearchTerm);

                Console.WriteLine($"Price: {st.MinPrice}-{st.MaxPrice} Years: {st.MinYear}-{st.MaxYear} Search: {st.CLSearch}<br>");

                //Search all US and CA cities asked for
                if(st.USCities != null && st.USCities.Length>0)
                    Search(st,st.USCities,".org");
                if(st.CACities != null && st.CACities.Length>0)
                    Search(st,st.CACities,".ca");
            }
            
            //dump
            matches.DumpItems();
        }

        static void Search(SearchTerm st,string cityList,string clDomain)
        {
            foreach(string city in cityList.Split(',').ToList())
            {
                string clURL = $"https://{city}.craigslist{clDomain}/search/sss?format=rss&query={st.CLSearch}&sort=rel";
                //Console.WriteLine($"City: <a href={clURL}>{city}</a><br>");

                FeedParser parser = new FeedParser();
                var items = parser.Parse(clURL,FeedType.RDF);
                foreach(Item item in items)
                {
                    //Check for title exclude keywords
                    bool excludeFlag = true;
                    foreach(string exclKey in st.ExcludeKeywords.Split(',').ToList())
                    {
                        if(exclKey.Length > 1 && item.Title.ToLower().Contains(exclKey.ToLower()))
                        {
                            excludeFlag=false;
                            break;
                        }
                    }

                    //Get and check year (if it can parse)
                    bool yearFlag = false;
                    long year = GetYear(item.Title);
                    if(year >= st.MinYear && year <= st.MaxYear)
                        yearFlag=true;

                    //Get and check price (zero if can't parse)
                    bool priceFlag = false;
                    long price = GetPrice(item.Title);
                    if(price >= st.MinPrice && price <= st.MaxPrice)
                        priceFlag=true;

                    //If we're still ok
                    if(excludeFlag && yearFlag && priceFlag)
                    {
                        bool added=matches.AddItem(item);
                        //if(added)
                        //    Console.WriteLine($"{price} - <a href={item.Link}>{item.Title}</a><br>");
                    }
                }
            }
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
        static long GetPrice(string title)
        {
            long price = 0;
            try
            {
                string priceStr = title.Substring(title.IndexOf(';')+1);
                price = Convert.ToInt32(priceStr);
            }
            catch {}

            return price;
        }
    }
}
