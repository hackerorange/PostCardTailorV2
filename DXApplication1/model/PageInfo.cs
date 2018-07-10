using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace PostCardTailor.model
{
    public class PageInfo<T>
    {
        [JsonProperty("page")]
        public List<T> Page { get; set; }
        
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("pageCount")]
        public int PageCount { get; set; }

    }
}
