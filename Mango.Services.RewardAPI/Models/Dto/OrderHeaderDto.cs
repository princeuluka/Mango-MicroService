using System;

namespace Mango.Services.RewardAPI.Models.Dto
{
    public class OrderHeaderDto
    {
        public int OrderHeaderId { get; set; }
        public string? UserId { get; set; }
        public double OrderTotal { get; set; }
    }
}
