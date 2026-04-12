using System.Collections.Generic;

namespace Mango.Services.OrderAPI.Models.Dto
{
    public class OrderDto
    {
        public OrderHeaderDto OrderHeader { get; set; }
        public IEnumerable<OrderDetailsDto> OrderDetails { get; set; }
    }
}
