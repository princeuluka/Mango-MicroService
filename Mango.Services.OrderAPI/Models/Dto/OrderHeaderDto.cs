namespace Mango.Services.OrderAPI.Models.Dto
{
    public class OrderHeaderDto
    {
        public int OrderHeaderId { get; set; }
        public string UserId { get; set; }
        public string? CouponCode { get; set; }
        public double Discount { get; set; }
        public double OrderTotal { get; set; }
        public string OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PickupEmail { get; set; }
        public string? PickupName { get; set; }
        public string? PickupPhone { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public DateTime OrderTime { get; set; }
        public IEnumerable<OrderDetailsDto>? OrderDetails { get; set; }
    }
}
