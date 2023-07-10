using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Xml;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml.Linq;


namespace DealingWithOPML.Pages;
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    public List<RssItem> RssList { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int PageCount { get; set; }
    public int PageSize { get; private set; } = 20;


    public async Task OnGet(int pageNumber = 1)
    {

        var httpClient = _httpClientFactory.CreateClient();
        HttpResponseMessage opmlResponse = await httpClient.GetAsync("https://blue.feedland.org/opml?screenname=dave");


        if (opmlResponse.IsSuccessStatusCode)
        {
            string opmlContent = await opmlResponse.Content.ReadAsStringAsync();
            var feedUrls = ExtractFeedUrlsFromOpml(opmlContent);

            var tasks = feedUrls.Select(url => FetchAndParseRssFeedAsync(httpClient, url));
            var rssResponses = await Task.WhenAll(tasks);
            RssList = rssResponses.SelectMany(r => r).ToList();

            TotalPages = (int)Math.Ceiling((double)RssList.Count / PageSize);
            PageNumber = pageNumber;

            RssList = RssList
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

        }
        else
        {
            StatusCode((int)Response.StatusCode);
        }
    }
    private List<string> ExtractFeedUrlsFromOpml(string opmlContent)
    {
        List<string> feedUrls = new List<string>();

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(opmlContent);

        XmlNodeList outlineNodes = xmlDocument.GetElementsByTagName("outline");
        foreach (XmlNode outlineNode in outlineNodes)
        {
            if (outlineNode.Attributes["xmlUrl"] != null)
            {
                string feedUrl = outlineNode.Attributes["xmlUrl"].Value;
                feedUrls.Add(feedUrl);
            }
        }
        return feedUrls;

    }
    async Task<List<RssItem>> FetchAndParseRssFeedAsync(HttpClient httpClient, string url)
    {
        List<RssItem> rssItems = new List<RssItem>();
        HttpResponseMessage response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            string xmlContent = await response.Content.ReadAsStringAsync();
            rssItems = ParseXmlContent(xmlContent);
        }
        return rssItems;
    }
    private List<RssItem> ParseXmlContent(string xmlContent)
    {
        List<RssItem> rssItems = new List<RssItem>();
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(xmlContent);

        XmlNodeList itemNodes = xmlDocument.GetElementsByTagName("item");
        foreach (XmlNode itemNode in itemNodes)
        {
            RssItem rssItem = new RssItem
            {
                Title = itemNode.SelectSingleNode("title")?.InnerText,
                PubDate = itemNode.SelectSingleNode("pubDate")?.InnerText,
                Description = itemNode.SelectSingleNode("description")?.InnerText,
                Link = itemNode.SelectSingleNode("link")?.InnerText,

            };
            rssItems.Add(rssItem);
        }
        return rssItems;

    }

}

public class RssItem
{
    public string? Title { get; set; }
    public string? PubDate { get; set; }
    public string? Description { get; set; }
    public string? Link { get; set; }

}
