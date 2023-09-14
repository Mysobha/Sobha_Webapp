using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sobha_Application.Models
{
    public class SharePointList
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string odataContext { get; set; }
        public string Username { get; set; }
        public string UserPhoto { get; set; }
        public string UserJobTitle { get; set; }
        public List<value> value { get; set; }
    }
}
