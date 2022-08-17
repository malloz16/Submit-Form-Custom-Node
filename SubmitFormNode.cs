using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Square9.CustomNode;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace Square9.SubmitForm
{
    public class SubmitFormActionNode : ActionNode
    {
        public override void Run()
        {
            var client = new RestClient(Settings.GetStringSetting("URL"));
            var loginRequest = new RestRequest("/user/login", Method.POST);
            loginRequest.AddJsonBody(new { data = new { name = Settings.GetStringSetting("User"), password = Settings.GetStringSetting("Password") } });
            var loginResponse = client.Execute(loginRequest);
            if (loginResponse.StatusCode != System.Net.HttpStatusCode.OK)
                if (null != loginResponse.ErrorException)
                    throw loginResponse.ErrorException;
                else
                    throw new Exception(loginResponse.StatusDescription);
            var token = loginResponse.Headers.FirstOrDefault(h => h.Name == "x-jwt-token")?.Value.ToString();

            var postSubmission = new RestRequest("/" + Settings.GetStringSetting("FormName"), Method.POST);
            postSubmission.AddHeader("x-jwt-token", token);
            postSubmission.AddHeader("content-type", "application/json");

            var dict = new Dictionary<string, object> { };

            List<string> formKeys = Settings.GetListSetting("FormKeys");
            List<string> processFields = Settings.GetListSetting("ProcessFields");

            if (formKeys.Count != processFields.Count)
                LogHistory("There must be a Form API Key for each Process Field provided.");
                SetNextNodeByLinkName("Failed");
            
            for (int index = 0; index < formKeys.Count; ++index)
            {
                if (!string.IsNullOrEmpty(formKeys[index]) && !string.IsNullOrEmpty(processFields[index]) && string.IsNullOrEmpty(Process.Properties.GetSingleValue(processFields[index])) == false)
                {
                    dict.Add(formKeys[index], Process.Properties.GetSingleValue(processFields[index]));
                }
                else
                {
                    formKeys.RemoveAt(index);
                    processFields.RemoveAt(index);
                }
            }

            string attachmentKey = Settings.GetStringSetting("Attachment");

            if (!string.IsNullOrEmpty(attachmentKey))
            {
                RestRequests restRequests;
                Byte[] s9document = null;
                string releaseFile = "";
                var postFile = new RestRequest("/api/files", Method.POST);

                try
                {
                    restRequests = new RestRequests(Engine.GetSquare9ApiClient());
                }
                catch (Exception ex)
                {
                    LogHistory("Unable to initialize Square 9 API connection: " + ex.Message);
                    Process.SetStatus(ProcessStatus.Errored);
                    return;
                }
                try
                {

                    int databaseId = Process.Document.DatabaseId;
                    int archiveId = Process.Document.ArchiveId;
                    int documentId = Process.Document.DocumentId;
                    string documentSecureId = restRequests.GetDocumentSecureID(databaseId, archiveId, documentId);
                    s9document = restRequests.GetDocument(databaseId, archiveId, documentId, documentSecureId);
                    releaseFile = Guid.NewGuid().ToString() + "_db" + databaseId.ToString() + "a" + archiveId.ToString() + "d" + documentId.ToString() + ".pdf";
                }
                catch (Exception ex)
                {
                    LogHistory(ex.Message);
                    Process.SetStatus(ProcessStatus.Errored);
                }

                LogHistory("Uploading GlobalSearch document to GlobalForms...");
                postFile.AddHeader("content-type", "multipart/form-data");
                postFile.AddParameter("dir", "");
                postFile.AddFile("file", s9document, releaseFile, "application/pdf");
                postFile.AddParameter("name", releaseFile);

                var fileResponse = client.Execute(postFile);
                if (fileResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (null != fileResponse.ErrorException)
                        throw fileResponse.ErrorException;
                    else
                        throw new Exception(fileResponse.StatusDescription);
                }
                else
                {
                    LogHistory("Successfully uploaded GlobalSearch document to GlobalForms.");
                    string formFilePath = Settings.GetStringSetting("URL") + "/api/files/" + releaseFile;
                    var fileUploadString = new[] { new { storage = "url", name = releaseFile, url = formFilePath, size = "", type = "", data = new { url = formFilePath }, originalName = releaseFile } };
                    dict.Add(attachmentKey, fileUploadString);
                }
            }

            var body = new Dictionary<string, object> { };
            body.Add("data", dict);

            postSubmission.AddJsonBody(body);

            var response = client.Execute(postSubmission);
            if (response.IsSuccessful)
            {
                JObject jsonResponse = JObject.Parse(response.Content);

                var passFlag = true;
                for (int i = 0; i < formKeys.Count; ++i)
                {
                    string responseValue;
                    try
                    {
                        responseValue = jsonResponse.SelectToken("data." + formKeys[i]).ToString();
                    }
                    catch
                    {
                        responseValue = "";
                    }
                    if (responseValue == "" || responseValue != Process.Properties.GetSingleValue(processFields[i]))
                    {
                        LogHistory("Value does not match the response. Ensure the API Key: " + formKeys[i] + " exists in the form.");
                        passFlag = false;
                        break;
                    }
                }

                if (passFlag == true)
                {
                    LogHistory("Successfully created new GlobalForm submission for: " + Settings.GetStringSetting("FormName"));
                    SetNextNodeByLinkName("Created");
                }
                else
                {
                    SetNextNodeByLinkName("Failed");
                }

            }
            else
            {
                LogHistory("Error posting submission to GlobalForms.");
                LogHistory(response.StatusCode + ": " + response.Content);
                SetNextNodeByLinkName("Failed");
            }
        }
    }

    public class SubmitFormCaptureNode : CaptureNode
    {
        public override void Run()
        {
            var client = new RestClient(Settings.GetStringSetting("URL"));
            var loginRequest = new RestRequest("/user/login", Method.POST);
            loginRequest.AddJsonBody(new { data = new { name = Settings.GetStringSetting("User"), password = Settings.GetStringSetting("Password") } });
            var loginResponse = client.Execute(loginRequest);
            if (loginResponse.StatusCode != System.Net.HttpStatusCode.OK)
                if (null != loginResponse.ErrorException)
                    throw loginResponse.ErrorException;
                else
                    throw new Exception(loginResponse.StatusDescription);
            var token = loginResponse.Headers.FirstOrDefault(h => h.Name == "x-jwt-token")?.Value.ToString();

            var postSubmission = new RestRequest("/" + Settings.GetStringSetting("FormName"), Method.POST);
            postSubmission.AddHeader("x-jwt-token", token);
            postSubmission.AddHeader("content-type", "application/json");

            var dict = new Dictionary<string, object> { };

            List<string> formKeys = Settings.GetListSetting("FormKeys");
            List<string> processFields = Settings.GetListSetting("ProcessFields");
            List<string> actualFormKeys = new List<string>();
            List<string> actualProcessFields = new List<string>();

            if (formKeys.Count != processFields.Count)
                LogHistory("There must be a Form API Key for each Process Field provided.");
            SetNextNodeByLinkName("Failed");
            
            for (int index = 0; index < formKeys.Count; ++index)
            {
                if (!string.IsNullOrEmpty(formKeys[index]) && !string.IsNullOrEmpty(processFields[index]) && string.IsNullOrEmpty(Process.Properties.GetSingleValue(processFields[index])) == false)
                {
                    actualFormKeys.Add(formKeys[index]);
                    actualProcessFields.Add(processFields[index]);
                    dict.Add(formKeys[index], Process.Properties.GetSingleValue(processFields[index]));
                }
            }

            string attachmentKey = Settings.GetStringSetting("Attachment");
            var s9document = "";
            string releaseFile = "";
            var postFile = new RestRequest("api/files", Method.POST);

            if (!string.IsNullOrEmpty(attachmentKey))
            {
                Process.Document.MergePages();
                s9document = Process.Properties.GetSingleValue("FilePath");

                if (!File.Exists(s9document))
                {
                    LogHistory("Unable to get the document.");
                    SetNextNodeByLinkName("Failed");
                }
                else
                {
                    releaseFile = Path.GetFileName(s9document);
                }

                LogHistory("Uploading document to GlobalForms...");
                postFile.AddHeader("content-type", "multipart/form-data");
                postFile.AddParameter("dir", "");
                postFile.AddFile("file", s9document);
                postFile.AddParameter("name", releaseFile);

                var fileResponse = client.Execute(postFile);
                if (fileResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (null != fileResponse.ErrorException)
                        throw fileResponse.ErrorException;
                    else
                        throw new Exception(fileResponse.StatusDescription);
                }
                else
                {
                    LogHistory("Successfully uploaded document to GlobalForms.");
                    string formFilePath = Settings.GetStringSetting("URL") + "/api/files/" + releaseFile;

                    var fileUploadString = new[] { new { storage = "url", name = releaseFile, url = formFilePath, size = "", type = "", data = new { url = formFilePath }, originalName = releaseFile } };

                    dict.Add(attachmentKey, fileUploadString);
                }

            }

            var body = new Dictionary<string, object> { };
            body.Add("data", dict);
            postSubmission.AddJsonBody(body);
            var response = client.Execute(postSubmission);
           
            if (response.IsSuccessful)
            {
                JObject jsonResponse = JObject.Parse(response.Content);

                var passFlag = true;
                for (int i = 0; i < actualFormKeys.Count; ++i)
                {
                    string responseValue;
                    try
                    {
                        responseValue = jsonResponse.SelectToken("data." + actualFormKeys[i]).ToString();
                    }
                    catch
                    {
                        responseValue = "";
                    }
                    if (responseValue == "" || responseValue != Process.Properties.GetSingleValue(actualProcessFields[i]))
                    {
                        LogHistory("Ensure the API Key: " + actualFormKeys[i] + " exists in the form.");
                    }
                }

                if (passFlag == true)
                {
                    LogHistory("Successfully created new GlobalForm submission for: " + Settings.GetStringSetting("FormName"));
                    SetNextNodeByLinkName("Created");
                }
                else
                {
                    SetNextNodeByLinkName("Failed");
                }

            }
            else
            {
                LogHistory("Error posting submission to GlobalForms.");
                LogHistory(response.StatusCode + ": " + response.Content);
                SetNextNodeByLinkName("Failed");
            }
        }
    }
}
