using System;
using System.Globalization;
using System.Xml;
using HtmlAgilityPack;
using System.Linq;

namespace CLReader
{
    static public class RTVScraper
    {      
        static public void Scrape(SearchTerm st,Matches matches,Matches lastMatches)
        {
            //If there are multiple terms, loop through them
            foreach(string searchTermToUse in st.RTVSearch.Split(',').ToList())
            {
                Scrape(st,matches,lastMatches,searchTermToUse);
            }
        }

        static public void Scrape(SearchTerm st,Matches matches,Matches lastMatches,string searchTermToUse)
        {
            //Nationwide, limited by class B and less than 25'
            var html = $"http://www.rvt.com/New-and-Used-Class-B-RVs-For-Sale-On-RVT.com/results?year1={st.MinYear}&year2={st.MaxYear}&type=Class+B&pricemax={st.MaxPrice}&keywords={st.RTVSearch}&s_by=5&ads_per_page=100";

            HtmlWeb web = new HtmlWeb();

            var _doc = web.Load(html);
            
            var findclasses = _doc.DocumentNode
                .Descendants( "li" )
                .Where( d => 
                    d.Attributes.Contains("itemscope")
                    &&
                    d.Attributes["itemtype"].Value.Contains("http://schema.org/Product")
                );

                
         
            foreach(var node in findclasses)
            {              
                try
                {
                    //Get title 
                    var title = node
                        .Descendants( "meta" )
                        .Where( d => 
                            d.Attributes.Contains("itemprop")
                            &&
                            d.Attributes["itemprop"].Value.Contains("name")
                        ).Single();
                            
                    string titleString=title.Attributes["content"].Value;
                    //Console.WriteLine($"Title: {titleString}");

                    //Get URL
                    var url = node
                        .Descendants( "a" )
                        .Where( d => 
                            d.Attributes.Contains("itemprop")
                            &&
                            d.Attributes["class"].Value.Contains("result-link")
                            ).Single();	
                    
                    string link=url.Attributes["href"].Value;
                    //Console.WriteLine($"URL: {link}");

                    //Get Price
                    var price = url
                        .Descendants( "span" )
                        .Where( d => 
                            d.Attributes.Contains("itemprop")
                            &&
                            d.Attributes["itemprop"].Value.Contains("price")
                            ).Single();                    
                          
                    //Parse currency
                    string priceString=price.InnerText;
                    decimal decPrice = decimal.Parse(priceString, NumberStyles.Any);
                    //Console.WriteLine("Price: " + decPrice.ToString());	

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
                            Title=titleString + " " + priceString,
                            SearchString=searchTermToUse,
                            WebSite="RTV",
                            Starred=st.Starred
                        };    

                        matches.AddItem(item,lastMatches);
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