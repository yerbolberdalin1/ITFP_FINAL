namespace DeliveryPipeline;

public abstract class Result<T> where T : notnull
{
    private Result() { }

    public sealed class Success : Result<T>
    {
        public T Value { get; }

        internal Success(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed class Failure : Result<T>
    {
        public Error Error { get; }

        internal Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }
    }

    public static Result<T> Ok(T value) => new Success(value);

    public static Result<T> Fail(Error error) => new Failure(error);

    public Result<U> Map<U>(Func<T, U> transform) where U : notnull
    {
        ArgumentNullException.ThrowIfNull(transform);

        return this switch
        {
            Success success => Result<U>.Ok(transform(success.Value)),
            Failure failure => Result<U>.Fail(failure.Error),
            _ => throw new InvalidOperationException("Unknown result case.")
        };
    }

    public Result<U> Bind<U>(Func<T, Result<U>> next) where U : notnull
    {
        ArgumentNullException.ThrowIfNull(next);

        return this switch
        {
            Success success => next(success.Value)
                ?? throw new InvalidOperationException("A Result operation returned null."),
            Failure failure => Result<U>.Fail(failure.Error),
            _ => throw new InvalidOperationException("Unknown result case.")
        };
    }
}
