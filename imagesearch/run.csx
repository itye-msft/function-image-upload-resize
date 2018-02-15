using System;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
 
static string bingAccessKey = System.Environment.GetEnvironmentVariable("bingAccessKey");

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");
    
    try {
        // parse query parameter
        string term = req.GetQueryNameValuePairs()
            .FirstOrDefault(q => string.Compare(q.Key, "term", true) == 0)
            .Value;
            log.Info(term);

        if(term == null)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a search term on the query string or in the request body");
        }

        log.Info(term);
        var imageResults = await BingImageSearch(term);
        //return req.CreateResponse(HttpStatusCode.OK, imageResults);
        var arr =(JArray)imageResults["value"];
        if ( arr.Count > 0)
        {
            var firstImageResult = arr.First;
            var img = firstImageResult["thumbnailUrl"].Value<string>();
            log.Info(img);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent("<html><body><img src='"+img+"'/></body></html>"); 
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }
        else
        {
            return req.CreateResponse(HttpStatusCode.OK, "Couldn't find image results!");
        }
    }
    catch(Exception e){
        log.Error(e.ToString());
        return req.CreateResponse(HttpStatusCode.BadRequest, e);
    }
}

/// <summary>
/// Performs a Bing Image search and return the results as a SearchResult.
/// </summary>
static async Task<JObject> BingImageSearch(string searchQuery)
{
    string uriBase = "https://api.cognitive.microsoft.com/bing/v7.0/images/search";
    // Construct the URI of the search request
    var uriQuery = uriBase + "?q=" + Uri.EscapeDataString(searchQuery);

    // Perform the Web request and get the response
    WebRequest request = HttpWebRequest.Create(uriQuery);
    request.Headers["Ocp-Apim-Subscription-Key"] = bingAccessKey;
    HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
    string json = new StreamReader(response.GetResponseStream()).ReadToEnd();

    return JObject.Parse(json);
}
