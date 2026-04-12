namespace Mango.Services.OrderAPI.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        private static readonly Dictionary<string, HashSet<string>> _validTransitions = new()
        {
            { Pending, new HashSet<string> { Confirmed, Cancelled } },
            { Confirmed, new HashSet<string> { Shipped, Cancelled } },
            { Shipped, new HashSet<string> { Delivered } },
            { Delivered, new HashSet<string>() },
            { Cancelled, new HashSet<string>() }
        };

        public static bool IsValidTransition(string currentStatus, string newStatus)
        {
            if (string.IsNullOrEmpty(currentStatus) || string.IsNullOrEmpty(newStatus))
                return false;

            if (!_validTransitions.ContainsKey(currentStatus))
                return false;

            return _validTransitions[currentStatus].Contains(newStatus);
        }

        public static bool IsValidStatus(string status)
        {
            return status == Pending ||
                   status == Confirmed ||
                   status == Shipped ||
                   status == Delivered ||
                   status == Cancelled;
        }
    }
}
