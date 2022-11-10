using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BCPUtilityDownloadFiles.Models
{
    public class ApiResponse<T>
    {
        [JsonProperty(PropertyName = "value")]
        public List<T> Value { get; set; }
    }
}
