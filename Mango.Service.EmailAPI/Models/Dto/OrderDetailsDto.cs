namespace Mango.Services.EmailAPI.Models.Dto
{
    public class OrderDetailsDto
    {
        public int OrderDetailsId { get; set; }
        public int OrderHeaderId { get; set; }
        public int ProductId { get; set; }
        public double Price { get; set; }
        public int Count { get; set; }
        public ProductDto Product { get; set; }
    }
}
