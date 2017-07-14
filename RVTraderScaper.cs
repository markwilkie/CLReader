using System;
using System.Globalization;
using System.Xml;
using HtmlAgilityPack;
using System.Linq;

namespace CLReader
{
    public class RVTraderScraper
    {      
        public void Scrape(SearchTerm st,Matches matches)
        {
            var html = $"https://www.rvtrader.com/search-results?type=Class%20B%7C198068&keyword={st.RVTraderSearch}&radius=any&zip=98026&sort=create_date%3Adesc&modelkeyword=1&layoutView=listView&page=1&price=8000%3A50000&year=2001%3A*&length=*%3A25&";

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
                //Console.WriteLine("Listing: ");

                try
                {
                    var title = node
                        .Descendants( "a" )
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&
                            d.Attributes["class"].Value.Contains("listing-info-title")
                        ).Single();
                            
                    //Console.WriteLine("Title: " + title.Attributes["title"].Value );	
                    //Console.WriteLine("Link: " + title.Attributes["href"].Value );
                    
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
                    bool excludeKeywordsFlag = CheckTitle(st.ExcludeKeywords,title.Attributes["title"].Value);
                    bool excludeCharsFlag = CheckTitle(st.ExcludeChars,title.Attributes["title"].Value);                            

                    if(excludeKeywordsFlag && excludeCharsFlag && priceFlag)
                    {
                        //Add 
                        Item item = new Item
                        {
                            Link="https://www.rvtrader.com"+title.Attributes["href"].Value,
                            Title=title.Attributes["title"].Value + price.Element("span").InnerText,
                            SearchString=st.RVTraderSearch,
                            WebSite="RVTrader",
                            Starred=st.Starred
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

        bool CheckTitle(string excludeString,string title)
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
    }
}