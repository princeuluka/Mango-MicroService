using Mango.Service.EmailAPI.Models;
using Mango.Services.EmailAPI.Data;
using Mango.Services.EmailAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Mango.Service.EmailAPI.Services
{
    public class EmailService : IEmailService
    {
        private DbContextOptions<AppDbContext> _dboptions;
        public EmailService(DbContextOptions<AppDbContext> dboptions)
        {
            _dboptions = dboptions;
        }

        

        public async Task EmailCartAndLog(CartDto cartDto)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine("<br/>Cart Email Requested");
            message.AppendLine("<br/>Total " + cartDto.CartHeader.CartTotal);
            message.AppendLine("<br/>");
            message.AppendLine("<ul>");
            foreach (var item in cartDto.CartDetails)
            {
                message.AppendLine("<li>");
                message.AppendLine(item.Product.Name + " x " + item.Count);
                message.AppendLine("</li>");
            }
            message.AppendLine("</ul>");
            await LogAndEmail(message.ToString(),cartDto.CartHeader.Email);
        }

        public async Task EmailOrderConfirmationAndLog(OrderHeaderDto orderHeaderDto)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine("<br/>Order Confirmation");
            message.AppendLine("<br/>Order ID: " + orderHeaderDto.OrderHeaderId);
            message.AppendLine("<br/>Order Status: " + orderHeaderDto.OrderStatus);
            message.AppendLine("<br/>Order Total: " + orderHeaderDto.OrderTotal);
            if (orderHeaderDto.Discount > 0)
            {
                message.AppendLine("<br/>Discount Applied: " + orderHeaderDto.Discount);
            }
            if (!string.IsNullOrEmpty(orderHeaderDto.CouponCode))
            {
                message.AppendLine("<br/>Coupon Code: " + orderHeaderDto.CouponCode);
            }
            message.AppendLine("<br/>");
            message.AppendLine("<ul>");
            if (orderHeaderDto.OrderDetails != null)
            {
                foreach (var item in orderHeaderDto.OrderDetails)
                {
                    message.AppendLine("<li>");
                    message.AppendLine(item.ProductName + " x " + item.Count + " @ " + item.Price);
                    message.AppendLine("</li>");
                }
            }
            message.AppendLine("</ul>");
            await LogAndEmail(message.ToString(), orderHeaderDto.PickupEmail);
        }


        private async Task<bool> LogAndEmail(string message , string email)
        {
            try
            {
                EmailLogger emailLog = new EmailLogger()
                {
                    Email = email,
                    EmailSent = DateTime.Now,
                    Message = message
                };

                await using var _db = new AppDbContext(_dboptions);

                await _db.EmailLoggers.AddAsync(emailLog);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
