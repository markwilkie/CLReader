using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
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

        public List<Item> GetSortedItems()
        {
            List<Item> sortedList = new List<Item>();
            
            foreach(KeyValuePair<string, Item> kvp in matchDict)
            {
                Item item=(Item)kvp.Value;

                bool addFlag=true;
                Item[] itemArray = sortedList.ToArray();
                for(int i=0;i<itemArray.Length;i++)
                {
                    if(itemArray[i].PublishDate <= item.PublishDate)
                    {
                        addFlag=false;
                        sortedList.Insert(i,item);
                        break;
                    }
                }

                if(addFlag)
                    sortedList.Add(item);
            }

            return sortedList;
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

        public List<Item> GetMatchList()
        {
            return GetSortedItems();
        }

        public void DumpItems()
        {
            //Open file
            string outputPath=Path.Combine("wwwroot","SearchResults.html");
            FileStream fileHandle = new FileStream (outputPath, FileMode.Create, FileAccess.Write);
            StreamWriter htmlStream = new StreamWriter(fileHandle);
            htmlStream.AutoFlush = true;

            htmlStream.WriteLine($"Search current as of {DateTime.Now}<p>");

            List<Item> itemList = GetSortedItems();
            foreach(Item item in itemList)
            {
                string starred=" ";
                if(item.Starred)
                    starred = "*";

                string encodedTitle = System.Net.WebUtility.UrlEncode($"http://localhost:5001/api/ignoretitle?title={item.Title}");
                htmlStream.WriteLine($"{starred} {item.PublishDate} - <a href={item.Link}>{item.Title}</a> ({item.WebSite}:{item.SearchString}) <a href={encodedTitle}>ignore</a><br>");
            }

            //Close up file
            fileHandle.Close();

        }
    }
}