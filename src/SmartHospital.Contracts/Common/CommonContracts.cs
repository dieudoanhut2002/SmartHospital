namespace SmartHospital.Contracts.Common;

public class PagedRequest
{
    private const int MaxPageSize = 100;
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public record ApiErrorResponse(
    string Code,
    string Message,
    string? CorrelationId = null,
    IDictionary<string, string[]>? ValidationErrors = null);

public record OperationResult(bool Succeeded, string Message, Guid? EntityId = null)
{
    public static OperationResult Ok(string message = "Thao tác thành công.", Guid? entityId = null)
        => new(true, message, entityId);

    public static OperationResult Fail(string message)
        => new(false, message);
}

public record OperationResult<T>(bool Succeeded, string Message, T? Data = default)
{
    public static OperationResult<T> Ok(T data, string message = "Thao tác thành công.")
        => new(true, message, data);

    public static OperationResult<T> Fail(string message)
        => new(false, message);
}
