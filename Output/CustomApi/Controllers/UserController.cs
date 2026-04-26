using CustomApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    [HttpPost("createWithArray")]
    public Task CreateUsersWithArrayInput([FromBody][BindRequired] IEnumerable<User> body)
    {
        return Task.CompletedTask;
    }

    [HttpPost("createWithList")]
    public Task CreateUsersWithListInput([FromBody][BindRequired] IEnumerable<User> body)
    {
        return Task.CompletedTask;
    }

    [HttpGet("{username}")]
    public Task<User> GetUserByName([BindRequired] string username)
    {
        var response = new User
        {
            Id = 1,
            Username = "test",
            FirstName = "test",
            LastName = "test",
            Email = "test",
            Password = "test",
            Phone = "test",
            UserStatus = 1
        };
        return Task.FromResult<User>(response);
    }

    [HttpPut("{username}")]
    public Task UpdateUser([BindRequired] string username, [FromBody][BindRequired] User body)
    {
        return Task.CompletedTask;
    }

    [HttpDelete("{username}")]
    public Task DeleteUser([BindRequired] string username)
    {
        return Task.CompletedTask;
    }

    [HttpGet("login")]
    public Task<string> LoginUser([FromQuery][BindRequired] string username, [FromQuery][BindRequired] string password)
    {
        var response = "test";
        return Task.FromResult<string>(response);
    }

    [HttpPost("user")]
    public Task CreateUser([FromBody][BindRequired] User body)
    {
        return Task.CompletedTask;
    }

    [HttpGet("logout")]
    public Task LogoutUser()
    {
        return Task.CompletedTask;
    }
}