using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CustomApi
{
    public partial class Pet
    {
        public long? Id { get; set; }
        public Category Category { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string? Name { get; set; }

        [Required]
        public List<string> PhotoUrls { get; set; } = new List<string>();
        public List<Tag> Tags { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PetStatus? Status { get; set; }
    }
}