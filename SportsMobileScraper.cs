using System;
using System.Globalization;
using System.Xml;
using HtmlAgilityPack;
using System.Linq;

namespace CLReader
{
    static public class SportsMobileScraper
    {      
        static public void Scrape(SearchTerm st,Matches matches,Matches lastMatches)
        {
            //Nationwide, limited by class B and less than 25'
            var html = $"http://sportsmobile.com/inventory/cars-for-sale/";

            HtmlWeb web = new HtmlWeb();

            var _doc = web.Load(html);
            
            var findclasses = _doc.DocumentNode
                .Descendants( "div" )
                .Where( d => 
                    d.Attributes.Contains("class")
                    &&
                    d.Attributes["class"].Value.Contains("random_description inventory_description")
                    );

            foreach(var node in findclasses)
            {              
                try
                {
                    //Get title 
                    var title = node
                        .Descendants( "span" )
                        .Where( d => 
                            d.Attributes.Contains("class")
                            &&					   
                            d.Attributes["class"].Value.Contains("random_title")
                            ).Single();		
                    
                    string titleString=title.InnerText;
                    //Console.WriteLine($"Title: {titleString}");

                    //Get URL
                    var linkNode = node
                        .Descendants( "a" )
                        .Where( d => 
                            d.Attributes.Contains("href")
                            ).Single();		
                    
                    string link=linkNode.Attributes["href"].Value;
                    //Console.WriteLine($"URL: {link}");

                    //See if sold
                    bool soldFlag=false;
                    try
                    {
                        var sold = node
                            .Descendants( "div" )
                            .Where( d => 
                                d.Attributes.Contains("class")
                                &&					   
                                d.Attributes["class"].Value.Contains("car_sold")
                                ).Single();	
                        
                        soldFlag=true;					
                    }
                    catch
                    {
                    }                    

                    //Parse currency
                    decimal decPrice=new Decimal(0);
                    int start=titleString.IndexOf('$');
                    if(start>=0)
                    {
                        string rawPrice=titleString.Substring(start);
                        decPrice = decimal.Parse(rawPrice, NumberStyles.Any);
                    }
                    //Console.WriteLine("Price: " + decPrice.ToString());	

                    //Get and check price (zero if can't parse)
                    bool priceFlag = false;
                    if((((long)decPrice == 0 && !st.IgnoreZeroPrice) || (long)decPrice >= st.MinPrice) && (long)decPrice <= st.MaxPrice)
                        priceFlag=true;     

                    //Get and check year (if it can parse)
                    bool yearFlag = false;
                    long year = GetYearFromTitle(titleString);
                    if((year == 0 || year >= st.MinYear) && year <= st.MaxYear)
                        yearFlag=true;                                             

                    //Check for exclusions
                    bool excludeKeywordsFlag = Helper.CheckTitle(st.ExcludeKeywords,titleString);
                    bool excludeCharsFlag = Helper.CheckTitle(st.ExcludeChars,titleString);    

                    //Console.WriteLine($"Price Flag: {priceFlag} Sold: {soldFlag} Year: {yearFlag} Exc {excludeKeywordsFlag} {excludeCharsFlag} for {titleString}");                                               

                    //If we're still good to go, let's add the item
                    if(excludeKeywordsFlag && excludeCharsFlag && priceFlag && yearFlag && !soldFlag)
                    {
                        //Add 
                        Item item = new Item
                        {
                            Link=link,
                            Title=titleString,
                            SearchString="ALL",
                            WebSite="SportsMobile",
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
    }
}