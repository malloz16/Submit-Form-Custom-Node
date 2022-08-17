using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Reflection;
using System.Web;

namespace Square9.SubmitForm
{
    public class RestRequests
    {
        public HttpClient Client;
        public string Token;

        public RestRequests(HttpClient client)
        {
            Token = Guid.Empty.ToString();
            Client = client;
            Client.DefaultRequestHeaders.Add("User-Agent", string.Format("GetDocument/{0}", (object)Assembly.GetExecutingAssembly().GetName().Version));
        }

        public string GetDocumentSecureID(int DatabaseID, int ArchiveID, int DocumentID)
        {
            UriBuilder uriBuilder = new UriBuilder(Client.BaseAddress.ToString() + string.Format("dbs/{0}/archives/{1}", (object)DatabaseID, (object)ArchiveID));
            NameValueCollection queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryString[nameof(DocumentID)] = DocumentID.ToString();
            queryString["token"] = Token;
            uriBuilder.Query = queryString.ToString();
            HttpResponseMessage result1 = Client.GetAsync(uriBuilder.ToString()).Result;
            string result2 = result1.Content.ReadAsStringAsync().Result;
            if (result1.IsSuccessStatusCode)
                return result2;
            throw new Exception("Unable to get Document Secure ID: " + result2);
        }

        public Byte[] GetDocument(int DatabaseID, int ArchiveID, int DocumentID, string SecureID)
        {
            UriBuilder uriBuilder = new UriBuilder(Client.BaseAddress.ToString() + string.Format("dbs/{0}/archives/{1}/documents/{2}/Print", (object)DatabaseID, (object)ArchiveID, (object)DocumentID));
            NameValueCollection queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
            queryString["token"] = Token;
            queryString["Secureid"] = SecureID;
            uriBuilder.Query = queryString.ToString();
            HttpResponseMessage result1 = Client.GetAsync(uriBuilder.ToString()).Result;
            Byte[] result2 = result1.Content.ReadAsByteArrayAsync().Result;
            if (result1.IsSuccessStatusCode)
                return result2;
            throw new Exception("Unable to get Document: " + result1.StatusCode);
        }
    }
}