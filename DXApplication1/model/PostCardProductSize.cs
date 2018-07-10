using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace PostCardTailor.model
{
    public class PostCardProductSize
    {
        [JsonProperty("name")]
        public string SizeName { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

    }
}
