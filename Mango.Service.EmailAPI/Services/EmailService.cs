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
            await LogAndEmail(message.ToString(), cartDto.CartHeader.Email);
        }

        public async Task EmailCheckoutAndLog(CheckoutHeaderDto checkoutHeader)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine("<br/>Checkout Email Requested");
            message.AppendLine("<br/>Order Total: " + checkoutHeader.OrderTotal);
            message.AppendLine("<br/>Discount: " + checkoutHeader.DiscountTotal);
            message.AppendLine("<br/>Customer: " + checkoutHeader.FirstName + " " + checkoutHeader.LastName);
            message.AppendLine("<br/>Email: " + checkoutHeader.Email);
            message.AppendLine("<br/>Phone: " + checkoutHeader.Phone);
            message.AppendLine("<br/>Address: " + checkoutHeader.AddressLine1 + ", " + checkoutHeader.AddressLine2);
            message.AppendLine("<br/>City: " + checkoutHeader.City + ", " + checkoutHeader.State + " " + checkoutHeader.PostalCode);
            message.AppendLine("<br/>Country: " + checkoutHeader.Country);
            message.AppendLine("<br/>");
            message.AppendLine("<ul>");
            foreach (var item in checkoutHeader.CartDetails)
            {
                message.AppendLine("<li>");
                message.AppendLine(item.Product.Name + " x " + item.Count + " @ $" + item.Product.Price);
                message.AppendLine("</li>");
            }
            message.AppendLine("</ul>");
            await LogAndEmail(message.ToString(), checkoutHeader.Email);
        }

        public async Task EmailOrderCreatedAndLog(OrderHeaderDto orderHeader)
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine("<br/>Order Created Email");
            message.AppendLine("<br/>Order ID: " + orderHeader.OrderHeaderId);
            message.AppendLine("<br/>Order Total: " + orderHeader.OrderTotal);
            message.AppendLine("<br/>Discount: " + orderHeader.Discount);
            message.AppendLine("<br/>Customer: " + orderHeader.Name);
            message.AppendLine("<br/>Email: " + orderHeader.Email);
            message.AppendLine("<br/>Phone: " + orderHeader.Phone);
            message.AppendLine("<br/>Status: " + orderHeader.Status);
            message.AppendLine("<br/>Order Time: " + orderHeader.OrderTime);
            message.AppendLine("<br/>");
            message.AppendLine("<ul>");
            foreach (var item in orderHeader.OrderDetails)
            {
                message.AppendLine("<li>");
                message.AppendLine("Product ID: " + item.ProductId + " x " + item.Count + " @ $" + item.Price);
                message.AppendLine("</li>");
            }
            message.AppendLine("</ul>");
            await LogAndEmail(message.ToString(), orderHeader.Email);
        }


        private async Task<bool> LogAndEmail(string message, string email)
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
