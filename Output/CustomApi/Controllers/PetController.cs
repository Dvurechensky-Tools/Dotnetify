using CustomApi;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("pet")]
public class PetController : ControllerBase
{
    [HttpPost("{petId}/uploadImage")]
    public Task<ApiResponse> UploadFile([BindRequired] long petId, string additionalMetadata, FileParameter file)
    {
        var response = new ApiResponse
        {
            Code = 1,
            Type = "test",
            Message = "test"
        };
        return Task.FromResult<ApiResponse>(response);
    }

    [HttpPost("pet")]
    public Task AddPet([FromBody][BindRequired] Pet body)
    {
        return Task.CompletedTask;
    }

    [HttpPut("pet")]
    public Task UpdatePet([FromBody][BindRequired] Pet body)
    {
        return Task.CompletedTask;
    }

    [HttpGet("findByStatus")]
    public Task<System.Collections.Generic.ICollection<Pet>> FindPetsByStatus([FromQuery][BindRequired] IEnumerable<Anonymous> status)
    {
        var response = new List<Pet>
        {
            new Pet
            {
                Id = 1,
                Category = new Category
                {
                    Id = default!,
                    Name = default!
                },
                Name = "test",
                PhotoUrls = new List<string>
                {
                    default!
                },
                Tags = new List<Tag>
                {
                    default!
                },
                Status = new PetStatus()
            }
        };
        return Task.FromResult<ICollection<Pet>>(response);
    }

    [HttpGet("findByTags")]
    public Task<System.Collections.Generic.ICollection<Pet>> FindPetsByTags([FromQuery][BindRequired] IEnumerable<string> tags)
    {
        var response = new List<Pet>
        {
            new Pet
            {
                Id = 1,
                Category = new Category
                {
                    Id = default!,
                    Name = default!
                },
                Name = "test",
                PhotoUrls = new List<string>
                {
                    default!
                },
                Tags = new List<Tag>
                {
                    default!
                },
                Status = new PetStatus()
            }
        };
        return Task.FromResult<ICollection<Pet>>(response);
    }

    [HttpGet("{petId}")]
    public Task<Pet> GetPetById([BindRequired] long petId)
    {
        var response = new Pet
        {
            Id = 1,
            Category = new Category
            {
                Id = 1,
                Name = "test"
            },
            Name = "test",
            PhotoUrls = new List<string>
            {
                "test"
            },
            Tags = new List<Tag>
            {
                new Tag
                {
                    Id = default!,
                    Name = default!
                }
            },
            Status = new PetStatus()
        };
        return Task.FromResult<Pet>(response);
    }

    [HttpPost("{petId}")]
    public Task UpdatePetWithForm([BindRequired] long petId, string name, string status)
    {
        return Task.CompletedTask;
    }

    [HttpDelete("{petId}")]
    public Task DeletePet([FromHeader] string api_key, [BindRequired] long petId)
    {
        return Task.CompletedTask;
    }
}