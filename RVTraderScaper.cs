using System;
using System.Globalization;
using System.Xml;
using HtmlAgilityPack;
using System.Linq;

namespace CLReader
{
    static public class RVTraderScraper
    {      
        static public void Scrape(SearchTerm st,Matches matches)
        {
            //If there are multiple terms, loop through them
            foreach(string searchTermToUse in st.RVTraderSearch.Split(',').ToList())
            {
                Scrape(st,matches,searchTermToUse);
            }
        }

        static public void Scrape(SearchTerm st,Matches matches,string searchTermToUse)
        {
            //Nationwide, limited by class B and less than 25'
            var html = $"https://www.rvtrader.com/search-results?type=Class%20B%7C198068&keyword={searchTermToUse}&radius=any&zip=98026&sort=create_date%3Adesc&modelkeyword=1&layoutView=listView&page=1&price={st.MinPrice}%3A{st.MaxPrice}&year={st.MinYear}%3A*&length=*%3A25&";

            HtmlWeb web = new HtmlWeb();

            var _doc = web.Load(html);
            
            var findclasses = _doc.DocumentNode
                .Descendants( "div" )
                .Where( d => 
                    d.Attributes.Contains("class")
                    &&
                    (d.Attributes["class"].Value.Contains("searchResultsMid listingContainer-list")
                    ||
                    d.Attributes["class"].Value.Contains("searchResultsMid feat-listing"))
                );

            //var node = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
            
            foreach(var node in findclasses)
            {
                try
                {
                    //Get title and listing link
                    var title = node
                        .Descendants( "a" )
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("listing-info-title")
                        ).Single();
                            
                    //Get title and link
                    string titleString=title.Attributes["title"].Value;
                    string link=title.Attributes["href"].Value;

                    //Get listing date
                    var listing = node
                        .Descendants( "div" )
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("companyName")
                        ).Single();
                    
                    //Parse date
                    string listingDateStr = listing.InnerText.Substring(listing.InnerText.IndexOf("Created")+8);
                    DateTime publishDate = DateTime.Parse(listingDateStr);

                    //Get Price
                    var price = node
                        .Descendants( "div" )
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("price")
                        ).Single();
                            
                    //Parse currency
                    string priceString=price.Element("span").InnerText;
                    int start=priceString.IndexOf('$');
                    int end=priceString.IndexOf(' ',start+1);
                    string rawPrice=priceString.Substring(start,end-start);
                    decimal decPrice = decimal.Parse(rawPrice, NumberStyles.Any);
                    //Console.WriteLine("Price: " + value.ToString());	

                    //Get and check price (zero if can't parse)
                    bool priceFlag = false;
                    if((((long)decPrice == 0 && !st.IgnoreZeroPrice) || (long)decPrice >= st.MinPrice) && (long)decPrice <= st.MaxPrice)
                        priceFlag=true;                    

                    //Check for exclusions
                    bool excludeKeywordsFlag = Helper.CheckTitle(st.ExcludeKeywords,title.Attributes["title"].Value);
                    bool excludeCharsFlag = Helper.CheckTitle(st.ExcludeChars,title.Attributes["title"].Value);                            

                    //If we're still good to go, let's add the item
                    if(excludeKeywordsFlag && excludeCharsFlag && priceFlag)
                    {
                        //Add 
                        Item item = new Item
                        {
                            Link="https://www.rvtrader.com"+link,
                            Title=titleString + priceString,
                            SearchString=searchTermToUse,
                            WebSite="RVTrader",
                            Starred=st.Starred,
                            PublishDate=publishDate
                        };    

                        matches.AddItem(item);
                    }
                }
                catch (System.Exception e)
                {
                    //Console.WriteLine(e.Message);
                    //Console.WriteLine($"Problem parsing: {node.OuterHtml}");
                }                	
            }
        }       
    }
}