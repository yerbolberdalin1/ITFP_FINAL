namespace DeliveryPipeline;

public abstract class Maybe<T> where T : notnull
{
    private Maybe() { }

    public sealed class Present : Maybe<T>
    {
        public T Value { get; }

        internal Present(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
    }

    public sealed class Missing : Maybe<T>
    {
        internal Missing() { }
    }

    public static Maybe<T> Some(T value) => new Present(value);

    public static Maybe<T> None { get; } = new Missing();

    public Result<T> ToResult(Error missingError)
    {
        ArgumentNullException.ThrowIfNull(missingError);

        return this switch
        {
            Present present => Result<T>.Ok(present.Value),
            Missing => Result<T>.Fail(missingError),
            _ => throw new InvalidOperationException("Unknown lookup case.")
        };
    }
}
