using InstagramApi.Application.Common;
using InstagramApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstagramApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected ICurrentUserService CurrentUser =>
        HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    protected Guid CurrentUserId => CurrentUser.UserId;

    protected IActionResult ApiOk<T>(T data, string? message = null)
        => Ok(ApiResponse<T>.SuccessResult(data, message));

    protected IActionResult ApiOk(string? message = null)
        => Ok(ApiResponse.SuccessResult(message));

    protected IActionResult ApiCreated<T>(T data, string? message = null)
        => StatusCode(201, ApiResponse<T>.SuccessResult(data, message, 201));

    protected IActionResult ApiBadRequest(string error)
        => BadRequest(ApiResponse<object>.FailResult(error));

    protected IActionResult ApiNotFound(string error = "Resource not found")
        => NotFound(ApiResponse<object>.FailResult(error, 404));

    protected IActionResult ApiUnauthorized(string error = "Unauthorized")
        => Unauthorized(ApiResponse<object>.FailResult(error, 401));

    protected IActionResult ApiForbidden(string error = "Forbidden")
        => StatusCode(403, ApiResponse<object>.FailResult(error, 403));
}
