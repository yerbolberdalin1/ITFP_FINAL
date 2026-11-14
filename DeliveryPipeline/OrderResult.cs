namespace DeliveryPipeline;

public abstract class OrderResult
{
    private OrderResult() { }

    public sealed class Valid : OrderResult
    {
        public DeliveryOrder Order { get; }

        internal Valid(DeliveryOrder order)
        {
            ArgumentNullException.ThrowIfNull(order);
            Order = order;
        }
    }

    public sealed class Invalid : OrderResult
    {
        public string Reason { get; }

        internal Invalid(string reason)
        {
            Reason = reason;
        }
    }
}
