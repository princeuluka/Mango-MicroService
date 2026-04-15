using System;

namespace Mango.Web.Models
{
    public class RewardActivityDto
    {
        public int RewardActivityId { get; set; }
        public string UserId { get; set; }
        public int OrderHeaderId { get; set; }
        public int Points { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}
