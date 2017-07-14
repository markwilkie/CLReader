using System;
using System.Linq;

namespace CLReader
{
    static public class Helper
    {      
        //Checks title for list of exclusions
        static public bool CheckTitle(string excludeString,string title)
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