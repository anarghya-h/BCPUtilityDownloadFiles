using Azure.Data.Tables;
using BCPUtilityDownloadFiles.Models;
using BCPUtilityDownloadFiles.Models.Configs;
using BCPUtilityDownloadFiles.Services;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
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
using Hyperlink = DocumentFormat.OpenXml.Spreadsheet.Hyperlink;
using X14 = DocumentFormat.OpenXml.Office2010.Excel;

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
        readonly StorageAccountConfig storageConfig;
        readonly TableClient tableClient;
        readonly BCPUtilityDBContext dBContext;
        readonly BlobStorageService storageService;
        #endregion

        #region Constructor
        public FileDownloadController(SdxConfig config, string url, AuthenticationService service, StorageAccountConfig storageTableConfig, BCPUtilityDBContext dBContext)
        {
            sdxConfig = config;
            StorageUrl = url;
            authService = service;
            this.storageConfig = storageTableConfig;
            //tableClient = new TableClient(storageTableConfig.ConnectionString, storageTableConfig.TableName);
            this.dBContext = dBContext;
            storageService = new BlobStorageService(storageConfig.ConnectionString, storageConfig.Container);
        }
        #endregion

        #region Private methods
        private static Stylesheet CreateStylesheet()
        {
            Stylesheet styleSheet = new Stylesheet();
            //Cell Fonts
            Fonts fonts = new Fonts() { Count = (UInt32Value)0U, KnownFonts = true };

            Font font1 = new Font();
            FontSize fontSize1 = new FontSize() { Val = 11D };
            Color color1 = new Color() { Theme = (UInt32Value)1U };
            FontName fontName1 = new FontName() { Val = "Calibri" };
            FontFamilyNumbering fontFamilyNumbering1 = new FontFamilyNumbering() { Val = 2 };
            FontScheme fontScheme1 = new FontScheme() { Val = FontSchemeValues.Minor };

            font1.AppendChild(fontSize1);
            font1.AppendChild(color1);
            font1.AppendChild(fontName1);
            font1.AppendChild(fontFamilyNumbering1);
            font1.AppendChild(fontScheme1);
            fonts.AppendChild(font1);
            fonts.Count++;

            Font font2 = new Font();
            FontSize fontSize2 = new FontSize() { Val = 11D };
            Color color2 = new Color() { Rgb = HexBinaryValue.FromString("0065B3") };
            FontName fontName2 = new FontName() { Val = "Calibri" };
            FontFamilyNumbering fontFamilyNumbering2 = new FontFamilyNumbering() { Val = 2 };
            FontScheme fontScheme2 = new FontScheme() { Val = FontSchemeValues.Minor };
            Underline fontUnderline2 = new Underline();

            font2.AppendChild(fontSize2);
            font2.AppendChild(color2);
            font2.AppendChild(fontName2);
            font2.AppendChild(fontFamilyNumbering2);
            font2.AppendChild(fontScheme2);
            font2.AppendChild(fontUnderline2);
            fonts.AppendChild(font2);
            fonts.Count++;

            //Cell Fills
            Fills fills = new Fills() { Count = (UInt32Value)0U };

            // FillId = 0
            Fill fill1 = new Fill();
            PatternFill patternFill1 = new PatternFill() { PatternType = PatternValues.None };
            fill1.AppendChild(patternFill1);

            // FillId = 1
            Fill fill2 = new Fill();
            PatternFill patternFill2 = new PatternFill() { PatternType = PatternValues.Gray125 };
            fill2.AppendChild(patternFill2);

            fills.AppendChild(fill1);
            fills.Count++;
            fills.AppendChild(fill2);
            fills.Count++;

            //Cell Borders
            Borders borders = new Borders() { Count = (UInt32Value)1U };

            Border border1 = new Border();
            LeftBorder leftBorder1 = new LeftBorder();
            RightBorder rightBorder1 = new RightBorder();
            TopBorder topBorder1 = new TopBorder();
            BottomBorder bottomBorder1 = new BottomBorder();
            DiagonalBorder diagonalBorder1 = new DiagonalBorder();

            border1.AppendChild(leftBorder1);
            border1.AppendChild(rightBorder1);
            border1.AppendChild(topBorder1);
            border1.AppendChild(bottomBorder1);
            border1.AppendChild(diagonalBorder1);

            borders.AppendChild(border1);

            //Cell Formats
            CellStyleFormats cellStyleFormats1 = new CellStyleFormats() { Count = (UInt32Value)1U };
            CellFormat cellFormat1 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)0U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U };

            cellStyleFormats1.AppendChild(cellFormat1);

            CellFormats cellFormats = new CellFormats() { Count = (UInt32Value)0U };
            CellFormat cellFormat2 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)0U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U, FormatId = (UInt32Value)0U };
            CellFormat cellFormat3 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)1U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U, FormatId = (UInt32Value)0U, ApplyFill = true };
            CellFormat cellFormat4 = new CellFormat() { NumberFormatId = (UInt32Value)0U, FontId = (UInt32Value)0U, FillId = (UInt32Value)0U, BorderId = (UInt32Value)0U, FormatId = (UInt32Value)0U, Alignment = new Alignment { WrapText = true }, ApplyAlignment = true };


            cellFormats.AppendChild(cellFormat2);
            cellFormats.Count++;
            cellFormats.AppendChild(cellFormat3);
            cellFormats.Count++;
            cellFormats.AppendChild(cellFormat4);
            cellFormats.Count++;

            CellStyles cellStyles = new CellStyles() { Count = (UInt32Value)1U };
            CellStyle cellStyle1 = new CellStyle() { Name = "Normal", FormatId = (UInt32Value)0U, BuiltinId = (UInt32Value)0U };

            cellStyles.AppendChild(cellStyle1);
            DifferentialFormats differentialFormats1 = new DifferentialFormats() { Count = (UInt32Value)0U };
            TableStyles tableStyles = new TableStyles() { Count = (UInt32Value)0U, DefaultTableStyle = "TableStyleMedium2", DefaultPivotStyle = "PivotStyleMedium9" };

            StylesheetExtensionList stylesheetExtensionList1 = new StylesheetExtensionList();

            StylesheetExtension stylesheetExtension1 = new StylesheetExtension() { Uri = "{EB79DEF2-80B8-43e5-95BD-54CBDDF9020C}" };
            X14.SlicerStyles slicerStyles1 = new X14.SlicerStyles() { DefaultSlicerStyle = "SlicerStyleLight1" };

            stylesheetExtension1.AppendChild(slicerStyles1);

            stylesheetExtensionList1.AppendChild(stylesheetExtension1);

            //Appending the fonts, fills and cell formats to the style sheet
            styleSheet.AppendChild(fonts);
            styleSheet.AppendChild(fills);
            styleSheet.AppendChild(borders);
            styleSheet.AppendChild(cellStyleFormats1);
            styleSheet.AppendChild(cellFormats);
            styleSheet.AppendChild(cellStyles);
            styleSheet.AppendChild(differentialFormats1);
            styleSheet.AppendChild(tableStyles);
            styleSheet.AppendChild(stylesheetExtensionList1);
            return styleSheet;
        }

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

            var options = new TypeConverterOptions { Formats = new[] { "dd-MM-yyyy HH:mm:ss" } };
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

        private async Task UpdateDocumentFilesAsync(BCPDocData tdata, BCPDocData record, RestClient client)
        {
            //Deleting the older file which is no longer relevant
            /*System.IO.File.Delete(StorageUrl + tdata.Name + "\\" + "Design_Files_" + tdata.Name + "\\" + tdata.File_Name);
            if (tdata.Rendition_OBID != null)
                System.IO.File.Delete(StorageUrl + tdata.Name + "\\" + "DRnd_" + tdata.Name + "\\" + tdata.File_Rendition);*/
            storageService.DeleteBlob(/*StorageUrl +*/ tdata.Name + "/" + "Design_Files_" + tdata.Name + "/" + tdata.File_Name);
            if (tdata.Rendition_OBID != null)
                storageService.DeleteBlob(/*StorageUrl +*/ tdata.Name + "/" + "DRnd_" + tdata.Name + "/" + tdata.File_Rendition);

            //Downloading the updated file
            string DirectoryName = /*StorageUrl +*/ record.Name + "/" + "Design_Files_" + record.Name;
            WebClient webClient = new();
            
            //Query to obtain the file details along with its URL
            string OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.File_OBID + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
            var request = new RestRequest(OdataQueryFileUri);
            request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
            request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

            var response3 = await client.GetAsync<ApiResponse<FileData>>(request);
            //webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "\\" + record.File_Name);

            MemoryStream ms = new MemoryStream(webClient.DownloadData(response3.Value[0].Uri));
            var fileUrl = storageService.UploadFileToBlob(DirectoryName + "/" + record.File_Name, ms);
            record.FileName_Path = fileUrl.ToString();

            //record.FileName_Path = DirectoryName + "\\" + record.Name;

            if (record.Rendition_OBID != null)
            {
                //Downloading the updated rendition file
                Console.WriteLine("Retrieving the file details for: " + record.File_Rendition);

                DirectoryName = /*StorageUrl +*/ record.Name + "/" + "DRnd_" + record.Name;
                
                //Checking if directory already exists in the folder
                /*if (!Directory.Exists(StorageUrl + record.Name))
                    Directory.CreateDirectory(StorageUrl + record.Name);
                if (!Directory.Exists(DirectoryName))
                    Directory.CreateDirectory(DirectoryName);*/

                //Query to obtain the file details along with its URL
                OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.Rendition_OBID + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
                request = new RestRequest(OdataQueryFileUri);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                response3 = await client.GetAsync<ApiResponse<FileData>>(request);                       
                //webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "\\" + record.File_Rendition);

                ms = new MemoryStream(webClient.DownloadData(response3.Value[0].Uri));
                fileUrl = storageService.UploadFileToBlob(DirectoryName + "/" + record.File_Rendition, ms);
                record.Rendition_Path = fileUrl.ToString();
                record.Rendition_Path = DirectoryName + "/" + record.File_Rendition;
            }

            /*record.PartitionKey = tdata.PartitionKey;
            record.RowKey = tdata.RowKey;

            tableClient.UpdateEntity(record, tdata.ETag);*/
            record.DocId = tdata.DocId;
            dBContext.SPM_JOB_DETAILS.Update(record);
            dBContext.SaveChanges();

        }

        private async Task DownloadFileAsync(BCPDocData record, RestClient client)
        {
            Console.WriteLine("Retrieving the file details for: " + record.File_Name);

            string DirectoryName = /*StorageUrl +*/ record.Name + "/" + "Design_Files_" + record.Name;
            WebClient webClient = new();

            //Checking if directory already exists in the folder
            /*if (!Directory.Exists(StorageUrl + record.Name))
                Directory.CreateDirectory(StorageUrl + record.Name);
            if (!Directory.Exists(DirectoryName))
                Directory.CreateDirectory(DirectoryName);*/

            //Checking if files are already present for a document
            //if (!System.IO.File.Exists(DirectoryName + "\\" + record.File_Name))
            
            //Query to obtain the file details along with its URL
            string OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.File_OBID + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
            var request = new RestRequest(OdataQueryFileUri);
            request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
            request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

            var response3 = await client.GetAsync<ApiResponse<FileData>>(request);

            //Downloading the file                        
            //webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "\\" + record.File_Name);
            MemoryStream ms = new MemoryStream(webClient.DownloadData(response3.Value[0].Uri));
            var fileUrl = storageService.UploadFileToBlob(DirectoryName + "/" + record.File_Name, ms);
            record.FileName_Path = fileUrl.ToString();
           
            //record.FileName_Path = DirectoryName + "\\" + record.Name;

            if (record.Rendition_OBID != null)
            {
                Console.WriteLine("Retrieving the file details for: " + record.File_Rendition);

                DirectoryName = /*StorageUrl + */record.Name + "/" + "DRnd_" + record.Name;
                
                //Checking if directory already exists in the folder
                /*if (!Directory.Exists(StorageUrl + record.Name))
                    Directory.CreateDirectory(StorageUrl + record.Name);
                if (!Directory.Exists(DirectoryName))
                    Directory.CreateDirectory(DirectoryName);*/

                //Checking if files area already present for a document

                //if (!System.IO.File.Exists(DirectoryName + "\\" + record.File_Rendition))
                //Query to obtain the file details along with its URL
                OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.Rendition_OBID + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
                request = new RestRequest(OdataQueryFileUri);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                response3 = await client.GetAsync<ApiResponse<FileData>>(request);

                //Downloading the file                        
                //webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "\\" + record.File_Rendition);
                ms = new MemoryStream(webClient.DownloadData(response3.Value[0].Uri));
                fileUrl = storageService.UploadFileToBlob(DirectoryName + "/" + record.File_Rendition, ms);
                record.Rendition_Path = fileUrl.ToString();                
                //record.Rendition_Path = DirectoryName + "\\" + record.File_Rendition;
            }
        }

        private async Task UpdateRenditionFilesAsync(BCPDocData tdata, BCPDocData record, RestClient client)
        {
            if(tdata.Rendition_OBID != null)
                System.IO.File.Delete(StorageUrl + tdata.Name + "/" + "DRnd_" + tdata.Name + "/" + tdata.File_Rendition);

            if (record.Rendition_OBID != null)
            {
                //Downloading the updated rendition file
                Console.WriteLine("Retrieving the file details for: " + record.File_Rendition);
                WebClient webClient = new();

                var DirectoryName = StorageUrl + record.Name + "/" + "DRnd_" + record.Name;

                //Checking if directory already exists in the folder
                if (!Directory.Exists(StorageUrl + record.Name))
                    Directory.CreateDirectory(StorageUrl + record.Name);
                if (!Directory.Exists(DirectoryName))
                    Directory.CreateDirectory(DirectoryName);

                //Query to obtain the file details along with its URL
                var OdataQueryFileUri = sdxConfig.ServerBaseUri + "Files('" + record.Rendition_OBID + "')/Intergraph.SPF.Server.API.Model.RetrieveFileUris";
                var request = new RestRequest(OdataQueryFileUri);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                var response3 = await client.GetAsync<ApiResponse<FileData>>(request);
                webClient.DownloadFile(response3.Value[0].Uri, DirectoryName + "/" + record.File_Rendition);
                record.Rendition_Path = DirectoryName + "/" + record.File_Rendition;
            }

            /*record.PartitionKey = tdata.PartitionKey;
            record.RowKey = tdata.RowKey;

            tableClient.UpdateEntity(record, tdata.ETag);*/
            record.DocId = tdata.DocId;
            dBContext.SPM_JOB_DETAILS.Update(record);
            dBContext.SaveChanges();

        }
        
        #endregion

        #region Public methods

        [HttpGet]
        public async Task<IActionResult> GetFilesAsync()
        {
            int i = 0;
            WorksheetPart worksheetPart = null;
            SpreadsheetDocument spreadsheetDocument = null;
            SheetData sheetData = null;
            List<BCPDocData> records = new();
            
            char[] reference = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
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

            //var columnList = Columns.Values.ToList();
            
            try
            {
                var client = new RestClient().UseNewtonsoftJson();

                Console.WriteLine("Retrieving the details of BCP documents");
                //Query to obtain the BCP documents
                string OdataQueryBcpDocsCount = sdxConfig.ServerBaseUri + "BCPDocuments?$filter=contains(Title, 'BCP')&$count=true";
                string OdataQueryBcpDocs = sdxConfig.ServerBaseUri + "BCPDocuments?$filter=contains(Title, 'BCP')&$count=true";
                var request = new RestRequest(OdataQueryBcpDocsCount);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                var countResponse = await client.GetAsync<ODataQueryResponse>(request);

                //OdataQueryBcpDocs += countResponse.Count;
                request = new RestRequest(OdataQueryBcpDocs);
                request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                var DocDetailsResponse = await client.GetAsync<ODataQueryResponse>(request);
                records.AddRange(DocDetailsResponse.Value);

                while(DocDetailsResponse.NextLink != null)
                {
                    request = new RestRequest(DocDetailsResponse.NextLink);
                    request.AddHeader("Authorization", "Bearer " + authService.tokenResponse.AccessToken);
                    request.AddHeader("X-Ingr-OnBehalfOf", sdxConfig.OnBehalfOfUser);

                    DocDetailsResponse = await client.GetAsync<ODataQueryResponse>(request);
                    records.AddRange(DocDetailsResponse.Value);
                }

                //Obtaining the existing data from the table
                //var tableData = tableClient.Query<CsvData>().ToList();
                var DbTableData = dBContext.SPM_JOB_DETAILS.ToList();


                //Creating the index file
                MemoryStream ms = new();
                spreadsheetDocument = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook);

                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();

                worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                Workbook workbook = new Workbook();
                FileVersion fileVersion = new FileVersion
                {
                    ApplicationName = "Microsoft Office Excel"
                };

                Worksheet worksheet = new Worksheet();
                WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                workbookStylesPart.Stylesheet = CreateStylesheet();
                workbookStylesPart.Stylesheet.Save();

                Columns sheetColumns = new Columns();
                Column sheetColumn = new Column()
                {
                    BestFit = true,
                    Min = 1,
                    Max = Convert.ToUInt32(Columns.Count),
                    CustomWidth = true,
                    Width = 30
                };
                sheetColumns.AppendChild(sheetColumn);
                worksheet.AppendChild(sheetColumns);

                sheetData = new SheetData();

                Hyperlinks hyperlinks = new Hyperlinks();
                var columnsList = records[0].GetType().GetProperties();

                //Adding the headers
                Row headerRow = new Row();
                foreach(var column in records[0].GetType().GetProperties())
                {
                    if (column.Name == "DocId" || column.Name == "Config" || column.Name == "Id")
                        continue;
                    Cell cell = new Cell()
                    {
                        CellValue = new CellValue(column.Name.ToUpper()),
                        DataType = CellValues.String
                    };
                    headerRow.AppendChild(cell);
                }
                sheetData.AppendChild(headerRow);

                
                foreach (var record in records)
                {
                    
                    //record.File_Last_Updated_Date = record.File_Last_Updated_Date.ToUniversalTime();
                    //record.Document_Last_Updated_Date = record.Document_Last_Updated_Date.ToUniversalTime();
                    record.FileName_Path = "";
                    record.Rendition_Path = "";
                    Row r = new();

                    var tdata = DbTableData.Find(x => x.File_UID == record.File_UID);
                    if (tdata != null)
                    {
                        if (tdata.Revision == record.Revision && tdata.Version == record.Version && DateTime.Equals(tdata.File_Last_Updated_Date,record.File_Last_Updated_Date))
                        {
                            record.FileName_Path = tdata.FileName_Path;
                            record.Rendition_Path = tdata.Rendition_Path;
                            /*if (tdata.RenditionObid == record.RenditionObid)
                                record.RenditionPath = tdata.RenditionPath;
                            else
                            {
                                await UpdateRenditionFilesAsync(tdata, record, client);
                            }*/
                        }
                        else if(DateTime.Compare(record.File_Last_Updated_Date, tdata.File_Last_Updated_Date) > 0)
                        {
                            await UpdateDocumentFilesAsync(tdata, record, client);
                        }  
                        else if(string.Compare(record.Revision, tdata.Revision) > 0)
                         {
                            await UpdateDocumentFilesAsync(tdata, record, client);
                        }
                        else if (record.Version > tdata.Version)
                        {
                            await UpdateDocumentFilesAsync(tdata, record, client);
                        }
                    }

                    else
                    {
                        if (record.File_OBID != null)
                        {
                            await DownloadFileAsync(record, client);
                        }

                        /*record.PartitionKey = Guid.NewGuid().ToString();
                        record.RowKey = Guid.NewGuid().ToString();                        
                        tableClient.AddEntity(record);*/
                        dBContext.SPM_JOB_DETAILS.Add(record);
                        dBContext.SaveChanges();
                    }
                    
                    /*record.FileLastUpdatedDate = record.FileLastUpdatedDate.ToLocalTime();
                    record.DocumentLastUpdatedDate = record.DocumentLastUpdatedDate.ToLocalTime();*/
                    

                    foreach(var column in records[0].GetType().GetProperties())
                    {
                        if (column.Name == "DocId" || column.Name == "Config" || column.Name == "Id")
                            continue;
                        Cell cell = new()
                        {
                            //CellValue = new CellValue(record.GetType().GetProperty(column.Name).GetValue(record).ToString()),
                            DataType = CellValues.String,
                            StyleIndex = 0
                        };
                        if (record.GetType().GetProperty(column.Name).GetValue(record) == null)
                            cell.CellValue = new CellValue("");
                        else
                            cell.CellValue = new CellValue(record.GetType().GetProperty(column.Name).GetValue(record).ToString());

                        if (column.Name == "FileName_Path" && record.File_OBID != null)
                        {
                            int index = Array.FindIndex(columnsList, x => x.Name == column.Name) - 1;
                            Hyperlink hyperlink = new Hyperlink()
                            {
                                Reference = reference[index].ToString() + (records.IndexOf(record) + 2),
                                Id = "HYP" + i,
                                Display = "Click here"
                            };
                            hyperlinks.AppendChild(hyperlink);
                            cell.CellValue = new CellValue(hyperlink.Display.Value);
                            cell.StyleIndex = 1;
                            worksheetPart.AddHyperlinkRelationship(new Uri(record.Name + "/" + "Design_Files_" + record.Name, UriKind.Relative), true, hyperlink.Id);
                            i++;
                        }

                        if (column.Name == "Rendition_Path" && record.Rendition_OBID != null)
                        {
                            int index = Array.FindIndex(columnsList, x => x.Name == column.Name) - 1;
                            Hyperlink hyperlink = new()
                            {
                                Reference = reference[index].ToString() + (records.IndexOf(record) + 2),
                                Id = "HYP" + i,
                                Display = "Click here"
                            };
                            hyperlinks.AppendChild(hyperlink);
                            cell.CellValue = new CellValue(hyperlink.Display.Value);
                            cell.StyleIndex = 1;
                            worksheetPart.AddHyperlinkRelationship(new Uri(record.Name + "/" + "DRnd_" + record.Name, UriKind.Relative), true, hyperlink.Id);
                            i++;
                        }
                        r.AppendChild(cell);
                    }                    
                    sheetData.AppendChild(r);

                }

                //appending the sheet data to the Worksheet
                worksheet.AppendChild(sheetData);
                worksheet.AppendChild(hyperlinks);
                worksheetPart.Worksheet = worksheet;
                worksheetPart.Worksheet.Save();

                //Creating new sheet
                Sheets sheets = new Sheets();
                Sheet sheet = new Sheet()
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "BCP_DOCUMENT_EXTRACT"
                };
                sheets.AppendChild(sheet);
                workbook.AppendChild(fileVersion);
                workbook.AppendChild(sheets);

                spreadsheetDocument.WorkbookPart.Workbook = workbook;
                spreadsheetDocument.WorkbookPart.Workbook.Save();
                spreadsheetDocument.Save();
                spreadsheetDocument.Close();

                ms.Position = 0;
                storageService.UploadFileToBlob("BCPDocumentExtract.xlsx", ms);

                //tableData = tableClient.Query<CsvData>().ToList();
                DbTableData = dBContext.SPM_JOB_DETAILS.ToList();
                foreach(var tdata in DbTableData)
                {
                    var record = records.Find(x => x.File_UID == tdata.File_UID);
                    if(record == null)
                    {
                        //tableClient.DeleteEntity(tdata.PartitionKey, tdata.RowKey);                        
                        //if(Directory.Exists(StorageUrl + tdata.Name))
                        if(storageService.CheckExists(tdata.Name + "/" + "Design_Files_" + tdata.Name + "/" + tdata.File_Name))
                        {
                            /*System.IO.File.Delete(StorageUrl + tdata.Name + "\\" + "Design_Files_" + tdata.Name + "\\" + tdata.File_Name);
                            if (tdata.Rendition_OBID != null)
                                System.IO.File.Delete(StorageUrl + tdata.Name + "\\" + "DRnd_" + tdata.Name + "\\" + tdata.File_Rendition);
                            var fileList = Directory.GetFiles(StorageUrl + tdata.Name + "\\" + "Design_Files_" + tdata.Name);
                            if (fileList.Length == 0)
                                Directory.Delete(StorageUrl + tdata.Name, true);*/

                            storageService.DeleteBlob(/*StorageUrl +*/ tdata.Name + "/" + "Design_Files_" + tdata.Name + "/" + tdata.File_Name);
                            if (storageService.CheckExists(tdata.Name + "/" + "DRnd_" + tdata.Name + "/" + tdata.File_Rendition))
                                storageService.DeleteBlob(/*StorageUrl +*/ tdata.Name + "/" + "DRnd_" + tdata.Name + "/" + tdata.File_Rendition);

                        }
                        dBContext.SPM_JOB_DETAILS.Remove(tdata);
                        dBContext.SaveChanges();

                    }
                }
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
