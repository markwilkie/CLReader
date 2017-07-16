using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace CLReader
{
    public class Matches
    {
        public Dictionary<string,Item> matchDict { get; set; }
        public DateTime LastScanDate { get; set; }   

        List<string> currentIgnoreList;
        List<string> newIgnoreList;


        public Matches()
        {
            LastScanDate=DateTime.Now;
            Console.WriteLine($"Last Scan date set to: {LastScanDate}");
            matchDict = new Dictionary<string, Item>();
            currentIgnoreList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("TitlesToIgnore.json"));
            newIgnoreList = new List<string>();
        }

        public void SaveNewIgnoreList()
        {
            string fileName="TitlesToIgnore.json";
            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(newIgnoreList, Formatting.Indented));
        }
        
        public Item GetIem(string title)
        {
            string encodedTitle = System.Net.WebUtility.UrlEncode(title);
            return matchDict[encodedTitle];
        }

        public bool AddItem(Item item,Matches lastMatches)
        {
            //make sure we don't need to ignore 
            string encodedTitle = System.Net.WebUtility.UrlEncode(item.Title);
            //Console.WriteLine($"Adding -{encodedTitle}-");
            if(!currentIgnoreList.Contains(encodedTitle))
            {
                //Bring forward from last scan to preserve date (repostings and entries with no date)
                Item lastItem;
                if(lastMatches != null && lastMatches.matchDict.TryGetValue(encodedTitle,out lastItem))
                {
                    return matchDict.TryAdd(encodedTitle,lastItem);
                }
                else
                {
                    return matchDict.TryAdd(encodedTitle,item);
                }
            }
            else
            {
                //Let's save this ignore list entry because it hit a match
                newIgnoreList.Add(encodedTitle);

                return false;
            }
        }

        public bool RemoveItem(Item item)
        {
            string encodedTitle = System.Net.WebUtility.UrlEncode(item.Title);
            return RemoveItem(encodedTitle);
        }

        public bool RemoveItem(string encodedTitle)
        {
            //Console.WriteLine($"Removing -{encodedTitle}-");
            return matchDict.Remove(encodedTitle);
        }

        public List<Item> GetNonPromotedList(List<Item> promotedList)
        {
            List<Item> matchesList = matchDict.Values.ToList();

            foreach(Item promtedItem in promotedList)
            {
                matchesList.Remove(promtedItem);
            }            

            ItemPublishDateComparer ic = new ItemPublishDateComparer();
            matchesList.Sort(ic);
            return matchesList;
        }

        public List<Item> GetPromotedList()
        {
            List<string> titlesToPromoteList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("TitlesToPromote.json"));
            List<Item> promotedList = new List<Item>();
            Item item;

            //Go through promoted titles and add the items to the list
            foreach(string titleKey in titlesToPromoteList)
            {
                if(matchDict.TryGetValue(titleKey,out item))
                    promotedList.Add(item);
            }

            ItemPublishDateComparer ic = new ItemPublishDateComparer();
            promotedList.Sort(ic);
            return promotedList;
        }
    }

    //So we can sort our lists and dictionaries
    public class ItemPublishDateComparer : IComparer<Item>
    {
        public int Compare(Item item1, Item item2)
        {
            //-1 item1<item2 0-item1=item2, and 1=item1>item2
            int retValue=0;
            if(item1.PublishDate < item2.PublishDate)
                retValue=1;
            if(item1.PublishDate == item2.PublishDate)
                retValue=0;
            if(item1.PublishDate > item2.PublishDate)
                retValue=-1;            

            return retValue;
        }
    }    
}