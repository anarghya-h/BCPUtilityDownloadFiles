using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DownloadFiles.Models
{
    public class CsvData
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Revision { get; set; }
        public string Version { get; set; }
        public string Classification { get; set; }
        public string Discipline { get; set; }
        public string FileUid { get; set; }
        public string FileObid { get; set; }
        public string FileName { get; set; }
        public string FileLastUpdatedDate { get; set; }
        public string PlantCode { get; set; }
        public string Unit { get; set; }
        public string SubUnit { get; set; }
        public string DocumentLastUpdatedDate { get; set; }

    }
}
