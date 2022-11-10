using CsvHelper;
using CsvHelper.Configuration;
using BCPUtilityDownloadFiles.Models;
using BCPUtilityDownloadFiles.Models.Configs;
using BCPUtilityDownloadFiles.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Data.Tables;
using CsvHelper.TypeConversion;
using System.Collections.Concurrent;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

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
        readonly StorageTableConfig storageTableConfig;
        TableClient tableClient;
        #endregion

        #region Constructor
        public FileDownloadController(SdxConfig config, string url, AuthenticationService service, StorageTableConfig storageTableConfig)
        {
            sdxConfig = config;
            StorageUrl = url;
            authService = service;
            this.storageTableConfig = storageTableConfig;
            tableClient = new TableClient(storageTableConfig.ConnectionString, storageTableConfig.TableName);

        }
        #endregion

        #region Private methods
        List<CsvData> ReadCsv(MemoryStream ms)
        {
            //Dictionary containing the mappings between the CSV column names and the data members of the class
            Dictionary<string, string> Columns = new Dictionary<string, string> {
                { "Name", "Name" },
                { "Title", "Title"},
                { "Revision", "Revision"},
                { "Version", "Version" },
                { "Document Type","Classification" },
                { "Discipline Description", "Discipline" },
                { "File UID","FileUid" },
                { "File OBID", "FileObid" },
                { "File Name", "FileName" },
                { "File Last Updated Date", "FileLastUpdatedDate" },
                { "Plant Code", "PlantCode" },
                { "Unit", "Unit" },
                { "Sub Unit", "SubUnit" },
                { "Document Last Updated Date", "DocumentLastUpdatedDate" },
                { "FileName_Path", "FileNamePath" },
                { "Document Rendition", "DocumentRendition" },
                { "File Rendition", "FileRendition" },
                { "Rendition OBID", "RenditionObid" },
                { "Rendition Path", "RenditionPath" },
                { "BCP Flag", "BCPFlag" }
            };

            using var reader = new StreamReader(ms);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var options = new TypeConverterOptions { Formats = new[] { "dd-MM-yyyy hh:mm:ss" } };
            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

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
            WorksheetPart worksheetPart = null;
            SheetData sheetData = null;
            Dictionary<string, string> Columns = new Dictionary<string, string> {
                { "Name", "Name" },
                { "Title", "Title"},
                { "Revision", "Revision"},
                { "Version", "Version" },
                { "Document Type","Classification" },
                { "Discipline Description", "Discipline" },
                { "File UID","FileUid" },
                { "File OBID", "FileObid" },
                { "File Name", "FileName" },
                { "File Last Updated Date", "FileLastUpdatedDate" },
                { "Plant Code", "PlantCode" },
                { "Unit", "Unit" },
                { "Sub Unit", "SubUnit" },
                { "Document Last Updated Date", "DocumentLastUpdatedDate" },
                { "FileName_Path", "FileNamePath" },
                { "Document Rendition", "DocumentRendition" },
                { "File Rendition", "FileRendition" },
                { "Rendition OBID", "RenditionObid" },
                { "Rendition Path", "RenditionPath" },
                { "BCP Flag", "BCPFlag" }
            };

            try
            {
                var client = new RestClient().UseNewtonsoftJson();

                Console.WriteLine("Retrieving CSV file");
                //Query to obtain the Notification details for BCP Document Extract
                string OdataQueryNotificationList = sdxConfig.ServerBaseUri + "User/Notifications?&$select=OBID,Name,Description,CreationDate&$filter=(contains(Name, 'SPM BCP DOC EXTRACT'))&$top=1&$skip=0&$count=true&$orderby=CreationDate+desc";
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

                //Downloading the data of the CSV file
                Console.WriteLine("Downloading the CSV file");
                WebClient webClient1 = new WebClient();
                string fileName = Path.GetFileName(responseNotificationFile.Url);
                //webClient1.DownloadFile(responseNotificationFile.Url, StorageUrl + fileName);
                MemoryStream ms = new MemoryStream(webClient1.DownloadData(responseNotificationFile.Url));

                //Reading the CSV file to get all the list of files
                Console.WriteLine("Reading the CSV file");
                var records = ReadCsv(ms);

                //Obtaining the existing data from the Storage table
                var tableData = tableClient.Query<CsvData>().ToList();

                
                /*var documentNames = records.Select(x => x.Name).Distinct().ToList();
                var fileNames = records.Select(x => x.FileName).ToList();
                var folderList = Directory.GetDirectories(StorageUrl);
                
                foreach (var directory in folderList)
                {
                    if (!documentNames.Contains(Path.GetFileName(directory)))
                        Directory.Delete(directory, true);
                    else
                    {
                        var fileList = Directory.GetFiles(directory);
                        foreach (var file in fileList)
                        {
                            if (!fileNames.Contains(Path.GetFileName(file)))
                                //Directory.Delete(file, true);
                                System.IO.File.Delete(file);
                        }
                    }
                }*/

                //Creating the index file
                SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(StorageUrl + "BCPDocumentExtract.xlsx", SpreadsheetDocumentType.Workbook);

                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();

                worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                Workbook workbook = new Workbook();
                FileVersion fileVersion = new FileVersion
                {
                    ApplicationName = "Microsoft Office Excel"
                };

                Worksheet worksheet = new Worksheet();
                               
                sheetData = new SheetData();

                //Adding the headers
                Row headerRow = new Row();
                foreach(var column in Columns)
                {
                    Cell cell = new Cell()
                    {
                        CellValue = new CellValue(column.Key.ToUpper()),
                        DataType = CellValues.String
                    };
                    headerRow.AppendChild(cell);
                }
                sheetData.AppendChild(headerRow);

                
                foreach (var record in records)
                {                    

                    Row r = new Row();

                    if (record.FileObid != "")
                    {
                        Console.WriteLine("Retrieving the file details for: " + record.FileName);

                        string DirectoryName = StorageUrl + record.Name + "\\" + "Design_Files_" + record.Name;
                        WebClient webClient = new WebClient();

                        //Checking if directory already exists in the folder
                        if (!Directory.Exists(StorageUrl + record.Name))
                            Directory.CreateDirectory(StorageUrl + record.Name);
                        if (!Directory.Exists(DirectoryName))
                            Directory.CreateDirectory(DirectoryName);

                        //Checking if files area already present for a document

                        if (!System.IO.File.Exists(DirectoryName + "\\" + record.FileName))
                        {
                            //Query to obtain the file details along with its URL
                            string OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.FileObid + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
                            request = new RestRequest(OdataQueryFileUri);
                            request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                            request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                            var response3 = await client.GetAsync<ApiResponse<FileData>>(request);

                            //Downloading the file                        
                            webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "\\" + record.FileName);
                            
                        }
                        record.FileNamePath = DirectoryName + "\\" + record.Name;

                        if (record.RenditionObid != "")
                        {
                            Console.WriteLine("Retrieving the file details for: " + record.FileRendition);

                            DirectoryName = StorageUrl + record.Name + "\\" + "DRnd_" + record.Name;
                            //WebClient webClient = new WebClient();

                            //Checking if directory already exists in the folder
                            if (!Directory.Exists(StorageUrl + record.Name))
                                Directory.CreateDirectory(StorageUrl + record.Name);
                            if (!Directory.Exists(DirectoryName))
                                Directory.CreateDirectory(DirectoryName);

                            //Checking if files area already present for a document

                            if (!System.IO.File.Exists(DirectoryName + "\\" + record.FileRendition))
                            {
                                //Query to obtain the file details along with its URL
                                string OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.RenditionObid + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
                                request = new RestRequest(OdataQueryFileUri);
                                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                                var response3 = await client.GetAsync<ApiResponse<FileData>>(request);

                                //Downloading the file                        
                                webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "\\" + record.FileRendition);
                                
                            }
                            record.RenditionPath = DirectoryName + "\\" + record.FileRendition;
                        }
                        
                    }
                    foreach(var column in Columns)
                    {
                        Cell cell = new Cell()
                        {
                            CellValue = new CellValue(record.GetType().GetProperty(column.Value).GetValue(record).ToString()),
                            DataType = CellValues.String
                        };
                        if (column.Value.Contains("Date"))
                            cell.DataType = CellValues.Date;
                        r.AppendChild(cell);
                    }
                    
                    sheetData.AppendChild(r);

                    record.PartitionKey = Guid.NewGuid().ToString();
                    record.RowKey = Guid.NewGuid().ToString();
                    record.FileLastUpdatedDate = record.FileLastUpdatedDate.ToUniversalTime();
                    record.DocumentLastUpdatedDate = record.DocumentLastUpdatedDate.ToUniversalTime();
                    tableClient.AddEntity(record);

                }
                //appending the sheet data to the Worksheet
                worksheet.AppendChild(sheetData);
                worksheetPart.Worksheet = worksheet;
                worksheetPart.Worksheet.Save();

                //Creating new sheet
                Sheets sheets = new Sheets();
                Sheet sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "EQUIPMENT_ERROR_REPORT"
                };
                sheets.AppendChild(sheet);
                workbook.AppendChild(fileVersion);
                workbook.AppendChild(sheets);

                spreadsheetDocument.WorkbookPart.Workbook = workbook;
                spreadsheetDocument.WorkbookPart.Workbook.Save();
                spreadsheetDocument.Save();
                spreadsheetDocument.Close();
                Console.WriteLine("Done");

                

                //Success
                return Ok("File(s) downloaded successfully");

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #endregion
    }
}
