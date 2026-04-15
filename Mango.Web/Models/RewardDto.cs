using System;

namespace Mango.Web.Models
{
    public class RewardDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int OrderId { get; set; }
        public DateTime RewardsDate { get; set; }
        public int RewardsPoints { get; set; }
    }
}
