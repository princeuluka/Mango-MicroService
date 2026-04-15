using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mango.Services.RewardAPI.Models
{
    public class RewardActivity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RewardActivityId { get; set; }

        public string UserId { get; set; }

        public int OrderHeaderId { get; set; }

        public int Points { get; set; }

        public DateTime ActivityDate { get; set; }
    }
}
