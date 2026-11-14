using System.Globalization;

namespace DeliveryPipeline;

public static class Validation
{
    public static OrderResult Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new OrderResult.Invalid("Line is empty.");

        string[] parts = line.Split(';');
        if (parts.Length != 5)
            return new OrderResult.Invalid("Expected 5 semicolon-separated fields.");

        const NumberStyles numberStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        if (!decimal.TryParse(parts[2].Trim(), numberStyles, CultureInfo.InvariantCulture, out decimal distance))
            return new OrderResult.Invalid("Distance must be a number using a decimal point.");

        if (!decimal.TryParse(parts[3].Trim(), numberStyles, CultureInfo.InvariantCulture, out decimal weight))
            return new OrderResult.Invalid("Weight must be a number using a decimal point.");

        string modeText = parts[4].Trim();
        DeliveryMode mode;
        if (modeText.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            mode = DeliveryMode.Standard;
        else if (modeText.Equals("Express", StringComparison.OrdinalIgnoreCase))
            mode = DeliveryMode.Express;
        else if (modeText.Equals("Priority", StringComparison.OrdinalIgnoreCase))
            mode = DeliveryMode.Priority;
        else
            return new OrderResult.Invalid("Unknown delivery mode.");

        return DeliveryOrder.Create(parts[0], parts[1], distance, weight, mode);
    }

    public static bool TryParse(string? line, out DeliveryOrder? order, out string? error)
    {
        switch (Parse(line))
        {
            case OrderResult.Valid valid:
                order = valid.Order;
                error = null;
                return true;
            case OrderResult.Invalid invalid:
                order = null;
                error = invalid.Reason;
                return false;
            default:
                throw new InvalidOperationException("Unknown order result.");
        }
    }
}
