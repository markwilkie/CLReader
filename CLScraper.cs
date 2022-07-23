using System;
using System.Globalization;
using System.Xml;
using HtmlAgilityPack;
using System.Linq;

namespace CLReader
{
    static public class CLScraper
    {      
        static public void Scrape(SearchTerm st,string cityList,string clDomain,Matches matches,Matches lastMatches)
        {
            //If there are multiple terms, loop through them
            foreach(string searchTermToUse in st.CLSearch.Split(',').ToList())
            {
                foreach(string city in cityList.Split(',').ToList())
                {
                    var url = $"https://{city}.craigslist{clDomain}/search/boo?query={searchTermToUse}&min_price={st.MinPrice}&max_price={st.MaxPrice}&srchType=T";
                    CLScrape(url,st,matches,lastMatches,searchTermToUse);
                }
            }
        }

        static private void CLScrape(string url,SearchTerm st,Matches matches,Matches lastMatches,string searchTermToUse)
        {
            HtmlWeb web = new HtmlWeb();

            var _doc = web.Load(url);
            
            var findclasses = _doc.DocumentNode
                .Descendants()
                .Where( d => 
                    d.Attributes.Contains("class")
                    &&
                    (d.Attributes["class"].Value.Contains("result-row"))
                );
          
            foreach(var node in findclasses)
            {
                try
                {
                    //Get title and listing link
                    var title = node
                        .Descendants()
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("result-title")
                        ).Single();

                    //Get title and link
                    string link=title.Attributes["href"].Value;
                    string titleString= System.Net.WebUtility.HtmlDecode(title.InnerText);

                    //Get location
                    var meta = node
                        .Descendants()
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("result-meta")
                        ).FirstOrDefault();

                    //Parse location
                    string locationString="(unknown)";
                    string rawLoc=meta.InnerText;
                    int start=rawLoc.IndexOf('(');
                    if(start>=0)
                    {
                        int end=rawLoc.IndexOf(')');
                        locationString=System.Net.WebUtility.HtmlDecode(rawLoc.Substring(start,(end-start)+1));
                    }

                    //Get listing date
                    var listing = node
                        .Descendants()
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("result-date")
                        ).Single();
                    
                    //Parse date
                    string listingDateStr = listing.Attributes["datetime"].Value;
                    DateTime publishDate = DateTime.Parse(listingDateStr);

                    //Get Price
                    var price = node
                        .Descendants()
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("result-price")
                        ).FirstOrDefault();
   
                    //Parse currency
                    string priceString=price.InnerText;
                    start=priceString.IndexOf('$');
                    string rawPrice=priceString.Substring(start);
                    decimal decPrice = decimal.Parse(rawPrice, NumberStyles.Any);

                    //Get and check price (zero if can't parse)
                    bool priceFlag = false;
                    if((((long)decPrice == 0 && !st.IgnoreZeroPrice) || (long)decPrice >= st.MinPrice) && (long)decPrice <= st.MaxPrice)
                        priceFlag=true;      
                               

                    //Check for exclusions
                    bool excludeKeywordsFlag = Helper.CheckTitle(st.ExcludeKeywords,titleString);
                    bool excludeCharsFlag = Helper.CheckTitle(st.ExcludeChars,titleString);                            

                    //If we're still good to go, let's add the item
                    if(excludeKeywordsFlag && excludeCharsFlag && priceFlag)
                    {
                        //Add 
                        Item item = new Item
                        {
                            Link=link,
                            Title=titleString + " " + locationString + " " + priceString,
                            SearchString=searchTermToUse,
                            WebSite="CList",
                            Starred=st.Starred,
                            PublishDate=publishDate
                        };    

                        matches.AddItem(item,lastMatches);
                    }
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                    //Console.WriteLine($"Problem parsing: {node.OuterHtml}");
                }                	
            }
        }       
    }
}