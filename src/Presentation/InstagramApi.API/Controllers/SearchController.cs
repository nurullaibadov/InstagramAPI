using AutoMapper;
using InstagramApi.Application.DTOs.Post;
using InstagramApi.Application.DTOs.User;
using InstagramApi.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[Authorize]
public class SearchController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public SearchController(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    /// <summary>Search users and hashtags</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q,
        [FromQuery] string type = "all", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q)) return ApiBadRequest("Query is required");

        object result = type switch
        {
            "users" => (object)await SearchUsers(q, page, pageSize),
            "hashtags" => await SearchHashtags(q, pageSize),
            _ => new
            {
                users = await SearchUsers(q, page, pageSize),
                hashtags = await SearchHashtags(q, 10)
            }
        };

        return ApiOk(result);
    }

    /// <summary>Get trending hashtags</summary>
    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrending([FromQuery] int count = 20)
    {
        var hashtags = await _uow.Hashtags.GetTrendingHashtagsAsync(count);
        var result = hashtags.Select(h => new { h.Name, h.PostsCount });
        return ApiOk(result);
    }

    private async Task<List<UserSummaryDto>> SearchUsers(string q, int page, int pageSize)
    {
        var users = await _uow.Users.SearchUsersAsync(q, page, pageSize);
        var dtos = new List<UserSummaryDto>();

        foreach (var u in users)
        {
            var dto = _mapper.Map<UserSummaryDto>(u);
            if (CurrentUser.IsAuthenticated)
                dto.IsFollowing = await _uow.Users.IsFollowingAsync(CurrentUserId, u.Id);
            dtos.Add(dto);
        }

        return dtos;
    }

    private async Task<List<object>> SearchHashtags(string q, int count)
    {
        var hashtags = await _uow.Hashtags.SearchHashtagsAsync(q, count);
        return hashtags.Select(h => (object)new { h.Name, h.PostsCount }).ToList();
    }
}
