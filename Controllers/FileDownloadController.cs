using DownloadFiles.Models;
using DownloadFiles.Models.Configs;
using DownloadFiles.Services;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using System.Reflection;
using System.Threading;
using Microsoft.SharePoint.Client;

namespace DownloadFiles.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileDownloadController : Controller
    {
        #region Private Members
        readonly SdxConfig sdxConfig;
        readonly string StorageUrl;
        readonly AuthenticationService authService;
        #endregion

        #region Constructor
        public FileDownloadController(SdxConfig config, string url, AuthenticationService service)
        {
            sdxConfig = config;
            StorageUrl = url;
            authService = service;
        }
        #endregion

        #region Private methods
        List<CsvData> ReadCsv(string path)
        {
            //Dictionary containing the mappings between the CSV column names and the data members of the class
            Dictionary<string, string> Columns = new Dictionary<string, string> { 
                { "Name", "Name" }, 
                { "Title", "Title"}, 
                { "Revision", "Revision"}, 
                { "Version", "Version" }, 
                { "Classification","Classification" },
                { "Discipline Description", "Discipline" },
                { "File UID","FileUid" },
                { "File OBID", "FileObid" },
                { "File Name", "FileName" },
                { "File Last Updated Date", "FileLastUpdatedDate" },
                { "Plant Code", "PlantCode" },
                { "Unit", "Unit" },
                { "Sub Unit", "SubUnit" },
                { "Document Last Updated Date", "DocumentLastUpdatedDate" }
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            //Creating a Class map to map the CSV column name to the class properties
            var map = new DefaultClassMap<CsvData>();
            foreach (var c in Columns)
            {
                PropertyInfo prop = typeof(CsvData).GetProperty(c.Value);
                var newMap = MemberMap.CreateGeneric(typeof(CsvData), prop);
                newMap.Data.Names.Add(c.Key);
                map.MemberMaps.Add(newMap);
            }
            csv.Context.RegisterClassMap(map);

            //Fetching the rows from the CSV file
            var records = csv.GetRecords<CsvData>().ToList();
            return records;            
        }
        #endregion

        #region Public methods
        
        [HttpGet]
        public async Task<IActionResult> GetFilesAsync()
        {
            try
            {
                var client = new RestClient().UseNewtonsoftJson();
                
                Console.WriteLine("Retrieving CSV file");
                //Query to obtain the Notification details for BCP Document Extract
                string OdataQueryNotificationList = sdxConfig.ServerBaseUri + "User/Notifications?&$select=OBID,Name,Description,CreationDate&$filter=(contains(Name, 'SPM BCP DOCUMENT EXTRACT'))&$top=1&$skip=0&$count=true&$orderby=CreationDate+desc";
                var request = new RestRequest(OdataQueryNotificationList);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                var responseNotificationListData = await client.GetAsync<ApiResponse<NotificationListData>>(request);


                //Query to obtain the URL of CSV from notification
                string OdataQueryNotificationFile = sdxConfig.ServerBaseUri + "GetReportFileUrlFromNotification()";
                request = new RestRequest(OdataQueryNotificationFile);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                //Initializing the body of the request
                RequestData data = new RequestData
                {
                    pstrNotificationOBID = responseNotificationListData.Value[0].Obid,
                    isMergedRendition = false
                };
                var json = JsonConvert.SerializeObject(data);
                request.AddJsonBody(json);

                //Executing the POST request
                var responseNotificationFile = await client.PostAsync<NotificationFileData>(request);

                //Downloading the CSV file
                Console.WriteLine("Downloading the CSV file");
                WebClient webClient1 = new WebClient();
                string fileName = Path.GetFileName(responseNotificationFile.Url);
                webClient1.DownloadFile(responseNotificationFile.Url, "C:\\Users\\Anarghya.H\\Files\\" + fileName);
                /*var fileContent = webClient1.DownloadData(responseNotificationFile.Url);
                webClient1.UploadData(StorageUrl + "/" + fileName, fileContent);*/

                //Reading the CSV file to get all the list of files
                Console.WriteLine("Reading the CSV file");
                var records = ReadCsv("C:\\Users\\Anarghya.H\\Files\\" + fileName);

                foreach (var record in records)
                {
                    if (record.FileObid != "")
                    {
                        Console.WriteLine("Retrieving the file details for: " + record.FileName);
                        //Query to obtain the file details along with its URL
                        string OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.FileObid + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
                        request = new RestRequest(OdataQueryFileUri);
                        request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                        request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                        var response3 = await client.GetAsync<ApiResponse<FileData>>(request);

                        var DirectoryName = record.Name + "_" + record.Revision + "_" + record.Version;
                        /*if (!Directory.Exists(StorageUrl + DirectoryName))
                            Directory.CreateDirectory(StorageUrl + DirectoryName);*/

                        //Downloading the file
                        WebClient webClient = new WebClient();
                        //webClient.DownloadFile(response3.Value[0].Uri, StorageUrl + DirectoryName + "\\" + record.FileName);
                        
                        var fileContent = webClient.DownloadData(response3.Value[0].Uri);
                        
                        using var targetContext = new ClientContext(StorageUrl);
                        //targetContext.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;

                        FileCreationInformation fileCreationInformation = new FileCreationInformation
                        {
                            Content = fileContent,
                            Url = record.FileName,
                            Overwrite = true
                        };

                        Web web = targetContext.Web;
                        targetContext.Load(web);

                        var targetFolder = web.GetFolderByServerRelativeUrl(StorageUrl);
                        targetContext.Load(targetFolder);
                        var uploadFile = targetFolder.Files.Add(fileCreationInformation);
                        targetContext.Load(uploadFile);
                        targetContext.ExecuteQuery();
                    }
                }
                Console.WriteLine("Done");


                //Success
                return Ok("File(s) downloaded successfully");
                
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
