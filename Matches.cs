using System;
using System.IO;
using System.Collections.Generic;

public class Matches
{
    Dictionary<string,Item> matchDict;

    public Matches()
    {
        matchDict = new Dictionary<string, Item>();
    }

    public Item GetItem(string clItemURL) 
    {
        Item item=null;
        matchDict.TryGetValue(clItemURL,out item);
        return item;
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
    public bool AddItem(Item item)
    {
        return matchDict.TryAdd(item.Title,item);
    }

    public void DumpItems()
    {
        //Open file
        FileStream fileHandle = new FileStream ("SearchResults.html", FileMode.CreateNew, FileAccess.Write);
        StreamWriter htmlStream = new StreamWriter(fileHandle);
        htmlStream.AutoFlush = true;

        htmlStream.WriteLine($"Search current as of {DateTime.Now}<p>");

        List<Item> itemList = GetSortedItems();
        foreach(Item item in itemList)
        {
            htmlStream.WriteLine($"{item.PublishDate} - <a href={item.Link}>{item.Title}</a><br>");
        }

        //Close up file
        fileHandle.Close();

    }
}