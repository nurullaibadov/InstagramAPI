namespace InstagramApi.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null, int statusCode = 200)
        => new() { Success = true, Data = data, Message = message, StatusCode = statusCode };

    public static ApiResponse<T> FailResult(string error, int statusCode = 400)
        => new() { Success = false, Errors = new List<string> { error }, StatusCode = statusCode };

    public static ApiResponse<T> FailResult(List<string> errors, int statusCode = 400)
        => new() { Success = false, Errors = errors, StatusCode = statusCode };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResult(string? message = null, int statusCode = 200)
        => new() { Success = true, Message = message, StatusCode = statusCode };

    public static new ApiResponse FailResult(string error, int statusCode = 400)
        => new() { Success = false, Errors = new List<string> { error }, StatusCode = statusCode };
}

public class PaginatedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    public static PaginatedResponse<T> Create(List<T> data, int page, int pageSize, int totalCount)
    {
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new()
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }
}
