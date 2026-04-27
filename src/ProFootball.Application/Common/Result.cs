namespace ProFootball.Application.Common;

public sealed record ResultError(string Code, string Message);

public sealed class Result
{
    private Result(bool isSuccess, ResultError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string code, string message) => new(false, new ResultError(code, message));
}
