namespace Rehably.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
        {
            throw new ArgumentException("Success result cannot have an error message.");
        }
        if (!isSuccess && string.IsNullOrEmpty(error))
        {
            throw new ArgumentException("Failure result must have an error message.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    public static Result Failure(string error)
    {
        return new Result(false, error);
    }
}

public class Result<T> : Result
{
    public T Value { get; }
    public T Data => Value; // Alias for convenience

    protected Result(bool isSuccess, T value, string error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty);
    }

    public static new Result<T> Failure(string error)
    {
        return new Result<T>(false, default(T)!, error);
    }
}
