using System.Collections.Generic;

namespace Mango.Services.OrderAPI.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Processing = "Processing";
        public const string ReadyForPickup = "ReadyForPickup";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        private static readonly HashSet<string> _validStatuses = new HashSet<string>
        {
            Pending, Confirmed, Processing, ReadyForPickup, Completed, Cancelled
        };

        private static readonly Dictionary<string, HashSet<string>> _allowedTransitions = new Dictionary<string, HashSet<string>>
        {
            { Pending, new HashSet<string> { Confirmed, Cancelled } },
            { Confirmed, new HashSet<string> { Processing, Cancelled } },
            { Processing, new HashSet<string> { ReadyForPickup, Cancelled } },
            { ReadyForPickup, new HashSet<string> { Completed } },
            { Completed, new HashSet<string>() },
            { Cancelled, new HashSet<string>() }
        };

        public static bool IsValid(string status)
        {
            return !string.IsNullOrEmpty(status) && _validStatuses.Contains(status);
        }

        public static bool CanTransition(string currentStatus, string newStatus)
        {
            if (!IsValid(currentStatus) || !IsValid(newStatus))
                return false;

            if (!_allowedTransitions.TryGetValue(currentStatus, out var allowedNext))
                return false;

            return allowedNext.Contains(newStatus);
        }

        public static bool CanCancel(string currentStatus)
        {
            return currentStatus == Pending ||
                   currentStatus == Confirmed ||
                   currentStatus == Processing;
        }
    }
}
