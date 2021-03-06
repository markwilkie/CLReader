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
            //Console.WriteLine($"Last Scan date set to: {LastScanDate}");
            matchDict = new Dictionary<string, Item>();
            currentIgnoreList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText("TitlesToIgnore.json"));
            newIgnoreList = new List<string>();

/* 
            //one time conversion
            foreach(string title in currentIgnoreList)
            {
                string newtitle = title;
                newtitle = System.Net.WebUtility.UrlDecode(newtitle);
                newtitle = newtitle.Replace("&#x0024;","$");
                newIgnoreList.Add(newtitle);
            }
            SaveNewIgnoreList();
            */
        }

        public void SaveNewIgnoreList()
        {
            //Let's make sure every ignore entry is given two attempts to work due to the interwebs being flaky
            //This way, something has to "miss" twice before falling off

            //First, let's load the existing 2nd chance list
            string secondChanceFileName="TitlesToIgnore2ndChance.json";
            List<string> secondChanceList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(secondChanceFileName));

            //Time to compare.  (entries in 2nd chance, but not in newIgnoreList)  In other words, these are the entires which would have fallen off if not not for 2nd chance
            secondChanceList = secondChanceList.Except(newIgnoreList).ToList();

            //Save 2nd chance as the current (pre add) ignore list.  This way, if the entry is STILL not in the current ignore list next time, it WILL fall off 
            //In other words, we're serializing something that's different than what'll be in memory.
            System.IO.File.WriteAllText(secondChanceFileName, JsonConvert.SerializeObject(newIgnoreList, Formatting.Indented));

            //Now let's add current 2nd chance list to the new ignore list
            newIgnoreList.AddRange(secondChanceList);

            //Save the current title to ignore - which now include 2nd chance....
            string fileName="TitlesToIgnore.json";
            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(newIgnoreList, Formatting.Indented));
        }
        
        public Item GetItem(string title)
        {
            return matchDict[title];
        }

        public void Save()
        {
            //Save current matches (these become "last" on startup)
            System.IO.File.WriteAllText("LastMatches.json", JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public bool AddItem(Item item,Matches lastMatches)
        {
            //Fix some stuff....
            item.Title=item.Title.Replace("'","");  //This is weird, but I couln't find a better way to do this

            //make sure we don't need to ignore 
            string title = item.Title;

            if(!currentIgnoreList.Contains(title))
            {
                //Bring forward from last scan to preserve date (repostings and entries with no date)
                Item lastItem;
                if(lastMatches != null && lastMatches.matchDict.TryGetValue(title,out lastItem))
                {
                    return matchDict.TryAdd(title,lastItem);
                }
                else
                {
                    return matchDict.TryAdd(title,item);
                }
            }
            else
            {
                //Let's save this ignore list entry because it hit a match
                newIgnoreList.Add(title);

                return false;
            }
        }

        public bool RemoveItem(Item item)
        {
            return RemoveItem(item);
        }

        public bool RemoveItem(string title)
        {
            return matchDict.Remove(title);
        }

        public void MarkClicked(string titleKey,string user)
        {
            //See who it is
            bool jennieFlag = false;
            if(user=="Jennie")
                jennieFlag=true;

            //Get item
            Item item=GetItem(titleKey);

            //Set appropriate flag
            if(jennieFlag)
                item.JennieClicked = true;
            else
                item.MarkClicked = true;

            Save();            
        }

        public void PromoteItem(string titleKey,string user)
        {
            //See who it is
            bool jennieFlag = false;
            if(user=="Jennie")
                jennieFlag=true;

            //Get item
            Item item=GetItem(titleKey);

            //Set appropriate flag
            if(jennieFlag)
            {
                if(!item.JennieInterested && !item.JennieBuy)
                {
                    item.JennieInterested=true;
                }
                else if(item.JennieInterested && !item.JennieBuy) 
                {
                    item.JennieInterested=false;
                    item.JennieBuy=true;
                }
            }
            else
            {
                if(!item.MarkInterested && !item.MarkBuy)
                {
                    item.MarkInterested=true;
                }
                else if(item.MarkInterested && !item.MarkBuy) 
                {
                    item.MarkInterested=false;
                    item.MarkBuy=true;
                }
            }

            Save();
        }

        public bool ItemIsPromoted(string titleKey)
        {
            bool promotedFlag = false;
            Item item=GetItem(titleKey);
            if(item.MarkBuy || item.MarkInterested || item.JennieBuy || item.JennieInterested)
                promotedFlag=true;

            return promotedFlag;
        }

        public void DemoteItem(string titleKey,string user)
        {
            //See who it is
            bool jennieFlag = false;
            if(user=="Jennie")
                jennieFlag=true;

            //Get item
            Item item=GetItem(titleKey);

            //Set appropriate flag
            if(jennieFlag)
            {
                if(item.JennieBuy)
                {
                    item.JennieInterested=true;
                    item.JennieBuy=false;
                }
                else
                {
                    item.JennieInterested=false;
                }
            }
            else
            {
                if(item.MarkBuy)
                {
                    item.MarkInterested=true;
                    item.MarkBuy=false;
                }
                else
                {
                    item.MarkInterested=false;
                }
            }

            Save();
        }

        public List<Item> GetList()
        {
            List<Item> matchesList = matchDict.Values.ToList();
            ItemPublishDateComparer ic = new ItemPublishDateComparer();
            matchesList.Sort(ic);
            return matchesList;
        }
    }

    //So we can sort our lists and dictionaries
    public class ItemPublishDateComparer : IComparer<Item>
    {
        public int Compare(Item item1, Item item2)
        {
            //-1 item1<item2 0-item1=item2, and 1=item1>item2
            // our list is in reverse order (newest on top)

            //First check promotion level
            if(IsItemBuy(item1) && IsItemBuy(item2))
                return CompareDates(item1,item2);
            else if(IsItemBuy(item1) && !IsItemBuy(item2))
                return(-1);  
            else if(!IsItemBuy(item1) && IsItemBuy(item2))
                return(1);
            
            if(IsItemInterested(item1) && IsItemInterested(item2))
                return CompareDates(item1,item2);
            else if(IsItemInterested(item1) && !IsItemInterested(item2))
                return(-1);  
            else if(!IsItemInterested(item1) && IsItemInterested(item2))
                return(1);
            
            //Nothing promoted
            return (CompareDates(item1,item2));
        }

        private int CompareDates(Item item1,Item item2)
        {            
            if(item1.PublishDate < item2.PublishDate)
                return(1);
            else if(item1.PublishDate == item2.PublishDate)
                return(0);
            else
                return(-1);  
        }

        private bool IsItemBuy(Item item)
        {
            return(item.MarkBuy || item.JennieBuy);
        }

        private bool IsItemInterested(Item item)
        {
            return(item.MarkInterested || item.JennieInterested);
        }        
    }    
}