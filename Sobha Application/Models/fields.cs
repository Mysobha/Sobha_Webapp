using Newtonsoft.Json;

namespace Sobha_Application.Models
{
    public class fields
    {
        [JsonProperty(PropertyName = "Title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "LinkTitle")]
        public string LinkTitle { get; set; }

        [JsonProperty(PropertyName = "Body")]
        public string Body { get; set; }

        [JsonProperty(PropertyName = "Section")]
        public string Section { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }

        [JsonProperty(PropertyName = "Image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "Image1")]
        public string Image1 { get; set; }

        [JsonProperty(PropertyName = "Image2")]
        public string Image2 { get; set; }

        [JsonProperty(PropertyName = "Image3")]
        public string Image3 { get; set; }

        [JsonProperty(PropertyName = "Image4")]
        public string Image4 { get; set; }

        [JsonProperty(PropertyName = "Image5")]
        public string Image5 { get; set; }

        public string ImageBase64 { get; set; }
        public string Image1Base64 { get; set; }
        public string Image2Base64 { get; set; }
        public string Image3Base64 { get; set; }
        public string Image4Base64 { get; set; }
        public string Image5Base64 { get; set; }

    }
}
