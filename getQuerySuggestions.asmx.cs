using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.IO;
using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

using System.Diagnostics;
using System.Configuration;

namespace WikiSearch
{
    /// <summary>
    /// Summary description for getQuerySuggestions
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class getQuerySuggestions : System.Web.Services.WebService
    {        
        private static StreamReader sr { get; set; }
        private static String filePath;
        private static PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        private static List<String> allTitles = new List<String>();

        [WebMethod]
        public string downloadBlob()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("pa2");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("newtitles.txt");
            filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";

            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    using (
                        var fileStream = System.IO.File.OpenWrite(filePath))
                    {
                        blockBlob.DownloadToStream(fileStream);
                    }
                }
            }

            using (sr = new StreamReader(filePath))
            {
                while (!sr.EndOfStream && GetAvailableMBytes() > 50)
                {
                    String line = sr.ReadLine();
                    allTitles.Add(line);
                }
            }

            return "success: downloaded to " + filePath;
        }


        // Returns 10 suggestions based on user input, called every time a user types something
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<String> returnSuggestions(String searched)
        {
            return StartsWithSearch(searched);
        }
         

        // search through all titles in list for up to 10 suggestions to return to the user
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<String> StartsWithSearch(string searched)
        {
            List<String> suggestions = new List<String>();            
            int index = 0;

            while (suggestions.Count <= 10 && index < allTitles.Count)
            {
                String line = allTitles[index];

                if (line.StartsWith(searched))
                {
                    suggestions.Add(line);
                }
                
                index++;
            }
            
            return suggestions;
        }

        
           
        [WebMethod]
        public static float GetAvailableMBytes()
        {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }
            
    }
}
