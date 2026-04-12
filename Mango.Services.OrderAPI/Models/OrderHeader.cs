using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mango.Services.OrderAPI.Models
{
    public class OrderHeader
    {
        [Key]
        public int OrderHeaderId { get; set; }
        public string UserId { get; set; }
        public string CouponCode { get; set; }
        public double Discount { get; set; }
        public double OrderTotal { get; set; }
        public string OrderStatus { get; set; }
        public string StatusMessage { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentIntentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public IEnumerable<OrderDetails> OrderDetails { get; set; }
    }
}
