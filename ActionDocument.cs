using System;
using Square9.CustomNode;


namespace Square9.SubmitForm
{
	public class ActionDocument
	{
		RestRequests restRequests;
        try
        {
            restRequests = new RestRequests(ActionEngine.GetSquare9ApiClient());
        }
        catch (Exception ex)
        {
            LogHistory("Unable to initialize Square 9 API connection: " + ex.Message);
            Process.SetStatus(ProcessStatus.Errored);
            return;
        }
        try
        {
                        
            int databaseId = actionProcess.Document.DatabaseId;
            int archiveId = actionProcess.Document.ArchiveId;
            int documentId = actionProcess.Document.DocumentId;
            string documentSecureId = restRequests.GetDocumentSecureID(databaseId, archiveId, documentId);
            restRequests.GetDocument(databaseId, archiveId, documentId, documentSecureId);
        }
        catch (Exception ex)
        {
            LogHistory(ex.Message);
            Process.SetStatus(ProcessStatus.Errored);
        }
	}
}


