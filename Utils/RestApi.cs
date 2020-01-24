using System;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;


namespace DataCleaner.Utils
{
    public class RestApi : IRestApi
    {
        HttpClient _httpClient = new HttpClient();
        string _baseUrl;
        public RestApi(string baseUrl)
        {
            _baseUrl = baseUrl;
        }
 
        public async Task<string> GetEndPointAsync(string ep, string qp)
        {
            string url = _baseUrl + ep;
            url = AddQueryParam(url, qp);

            HttpRequestMessage usersRequest = new HttpRequestMessage(HttpMethod.Get, url);
            var resp = await _httpClient.SendAsync(usersRequest);
            string json= "";
            if (resp.IsSuccessStatusCode)
            {
                json = await resp.Content.ReadAsStringAsync();
            }
            else
            {
                 throw new HttpRequestException(resp.ReasonPhrase);
            }
            return json;
        }

        private string AddQueryParam(string url, string param)
        {
            var builder = new UriBuilder(url);
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["id"] = param;
            builder.Query = query.ToString();
            
            return builder.ToString();
        }
    }
}