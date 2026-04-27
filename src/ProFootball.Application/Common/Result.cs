using ProFootball.Application.Common.Errors;

namespace ProFootball.Application.Common;

public interface IResult
{
    Error Error { get; }

    bool IsSuccess { get; }

    bool IsFailure { get; }
}

public interface IResult<out TValue> : IResult
{
    TValue Value { get; }
}

public class Result : IResult
{
    public Error Error { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    private Result()
    {
        IsSuccess = true;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result Success() => new();

    public static Result Failure(Error error) => new(error);

    public static Result Failure(string code, string message) => new(new Error(code, message));
}

public class Result<TValue> : IResult<TValue>
{
    public TValue Value { get; }

    public Error Error { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    private Result(TValue value)
    {
        IsSuccess = true;
        Value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
        Value = default!;
    }

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error) => new(error);

    public static Result<TValue> Failure(string code, string message) => new(new Error(code, message));

    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}
