using CustomApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("store")]
public class StoreController : ControllerBase
{
    [HttpGet("inventory")]
    public Task<System.Collections.Generic.IDictionary<string, int>> GetInventory()
    {
        var response = new Dictionary<string, int>
        {
            {
                "test",
                1
            }
        };
        return Task.FromResult<IDictionary<string, int>>(response);
    }

    [HttpPost("order")]
    public Task<Order> PlaceOrder([FromBody][BindRequired] Order body)
    {
        var response = new Order
        {
            Id = 1,
            PetId = 1,
            Quantity = 1,
            ShipDate = new DateTimeOffset(),
            Status = new OrderStatus(),
            Complete = true
        };
        return Task.FromResult<Order>(response);
    }

    [HttpGet("order/{orderId}")]
    public Task<Order> GetOrderById([BindRequired] long orderId)
    {
        var response = new Order
        {
            Id = 1,
            PetId = 1,
            Quantity = 1,
            ShipDate = new DateTimeOffset(),
            Status = new OrderStatus(),
            Complete = true
        };
        return Task.FromResult<Order>(response);
    }

    [HttpDelete("order/{orderId}")]
    public Task DeleteOrder([BindRequired] long orderId)
    {
        return Task.CompletedTask;
    }
}