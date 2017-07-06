using System;

public class SearchTerm
{
    public bool Starred { get; set; }
    public long MinPrice { get; set; }
    public long MaxPrice { get; set; }
    public long MinYear { get; set; }
    public long MaxYear { get; set; }
    public string CLSearch { get; set; }
    public string ExcludeKeywords { get; set; }
    public string USCities { get; set; }
    public string CACities { get; set; }

    public SearchTerm()
    {
        MinPrice=-1;  //-1 means fill in with default
        MaxPrice=-1;
        MinYear=-1;
        MaxYear=-1;
    }

    public void SetDefaults(SearchTerm defaultSearchTerm)
    {
        if(MinPrice<0)
            MinPrice=defaultSearchTerm.MinPrice;
        if(MaxPrice<0)
            MaxPrice=defaultSearchTerm.MaxPrice;
        if(MinYear<0)
            MinYear=defaultSearchTerm.MinYear;
        if(MaxYear<0)
            MaxYear=defaultSearchTerm.MaxYear;
        if(ExcludeKeywords == null)
            ExcludeKeywords=defaultSearchTerm.ExcludeKeywords;
        if(USCities == null)
            USCities=defaultSearchTerm.USCities;
        if(CACities == null)
            CACities=defaultSearchTerm.CACities;
    }
}