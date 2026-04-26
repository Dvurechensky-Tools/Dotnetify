using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CustomApi
{
    public partial class Order
    {
        public long? Id { get; set; }
        public long? PetId { get; set; }
        public int? Quantity { get; set; }
        public DateTimeOffset? ShipDate { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatus? Status { get; set; }
        public bool? Complete { get; set; }
    }
}