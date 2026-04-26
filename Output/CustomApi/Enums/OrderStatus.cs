using System.CodeDom;
using System.CodeDom.Compiler;
using System.Runtime;
using System.Runtime.Serialization;

namespace CustomApi
{
    [GeneratedCode("NJsonSchema", "14.7.0.0 (NJsonSchema v11.6.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public enum OrderStatus
    {
        [EnumMember(Value = @"placed")]
        Placed = 0,
        [EnumMember(Value = @"approved")]
        Approved = 1,
        [EnumMember(Value = @"delivered")]
        Delivered = 2,
    }
}