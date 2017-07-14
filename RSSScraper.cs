using System;
using System.Globalization;
using System.Linq;
using System.Net;

namespace CLReader
{
    static public class RSSScraper
    {
        static public void SearchCL(SearchTerm st,string cityList,string clDomain,Matches matches)
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
                    bool excludeKeywordsFlag = Helper.CheckTitle(st.ExcludeKeywords+" ",item.Title);
                    bool excludeCharsFlag = Helper.CheckTitle(st.ExcludeChars,item.Title);

                    //Get and check year (if it can parse)
                    bool yearFlag = false;
                    long year = GetYearFromTitle(item.Title);
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

        static public void SearchSamba(SearchTerm st,Matches matches)
        {
            //Searches only within eurovan
            string clURL = $"https://www.thesamba.com/vw/classifieds/rss/search.php?type=text&stype=any&keywords={st.SambaSearch}&yearfrom={st.MinYear}&yearto={st.MaxYear}&model%5B%5D=&section%5B%5D=75&country=USA&wanted=hide&sort=date&sort_order=DESC";

            FeedParser parser = new FeedParser();
            var items = parser.Parse(clURL,FeedType.RSS);
            foreach(Item item in items)
            {
                //Check for title exclude keywords and chars
                bool excludeKeywordsFlag = Helper.CheckTitle(st.ExcludeKeywords+" ",item.Title);
                bool excludeCharsFlag = Helper.CheckTitle(st.ExcludeChars,item.Title);

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

        static public void SearchEbay(SearchTerm st,Matches matches)
        {
            //enclode search terms
            string encodedSearch = System.Net.WebUtility.UrlEncode(st.EbaySearch);

            //Searches in all of ebay motors  (does'nt limit by year)
            string clURL = $"https://www.ebay.com/sch/eBay-Motors/6000/i.html?_udlo={st.MinPrice}&_udhi={st.MaxPrice}&_mPrRngCbx=1&_from=R40&_sop=10&_nkw={encodedSearch}&LH_PrefLoc=3&_rss=1";

            FeedParser parser = new FeedParser();
            var items = parser.Parse(clURL,FeedType.RSS);
            foreach(Item item in items)
            {
                //Check for title exclude keywords and chars
                bool excludeKeywordsFlag = Helper.CheckTitle(st.ExcludeKeywords+" ",item.Title);
                bool excludeCharsFlag = Helper.CheckTitle(st.ExcludeChars,item.Title);

                //Get and check year (if it can parse)
                bool yearFlag = false;
                long year = GetYearFromLink(item.Link);
                if((year == 0 || year >= st.MinYear) && year <= st.MaxYear)
                    yearFlag=true;                

                //Get and check price (zero if can't parse)
                bool priceFlag = false;
                long price = GetPrice(item.Content,'$','<');
                if(((price == 0 && !st.IgnoreZeroPrice) || price >= st.MinPrice) && price <= st.MaxPrice)
                    priceFlag=true;

                //If we're still ok
                if(excludeKeywordsFlag && excludeCharsFlag && priceFlag && yearFlag)
                {
                    //Update item
                    item.Title = $"{item.Title} ${price} ";
                    item.Starred = st.Starred;
                    item.SearchString = st.EbaySearch;
                    item.WebSite = "Ebay";

                    //Add item to list
                    bool added=matches.AddItem(item);
                    //if(added)
                    //    Console.WriteLine($"{price} - <a href={item.Link}>{item.Title}</a><br>");
                }
            }
        }

        static public long GetYearFromLink(string link)
        {
             long year = 0;
            try
            {
                int startPos=link.IndexOf("itm/")+4;
                int endPos=link.IndexOf('-',startPos);
                string yearStr = link.Substring(startPos,endPos-startPos);
                year = Convert.ToInt32(yearStr);
            }
            catch {}

            return year;           
        }        

        static public long GetYearFromTitle(string title)
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

        static public long GetPrice(string textToParse,char delimeter)
        {
            long price = 0;
            try
            {
                string priceStr = textToParse.Substring(textToParse.IndexOf(delimeter)+1);
                price = (long)decimal.Parse(priceStr, NumberStyles.Any);
                //price = Convert.ToInt32(priceStr);
            }
            catch {}

            return price;
        }

        static public long GetPrice(string textToParse,char delimeter,char endChar)
        {
            long price = 0;
            try
            {
                int startPos=textToParse.IndexOf(delimeter)+1;
                int endPos=textToParse.IndexOf(endChar,startPos);
                string priceStr = textToParse.Substring(startPos,endPos-startPos);
                //Console.WriteLine($"price: {priceStr}");
                price = (long)decimal.Parse(priceStr, NumberStyles.Any);
            }
            catch {}

            return price;
        }        
    }
}