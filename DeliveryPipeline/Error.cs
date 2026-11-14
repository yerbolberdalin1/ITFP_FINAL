namespace DeliveryPipeline;

public sealed class Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
    }
}
