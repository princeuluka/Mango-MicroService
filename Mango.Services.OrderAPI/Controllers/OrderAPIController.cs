using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        protected ResponseDto _response;

        public OrderAPIController(
            AppDbContext db,
            IMapper mapper,
            IMessageBus messageBus,
            IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _messageBus = messageBus;
            _configuration = configuration;
            _response = new ResponseDto();
        }

        [Authorize]
        [HttpGet("GetOrders/{userId}")]
        public async Task<ResponseDto> GetOrders(string userId)
        {
            try
            {
                var orderHeaders = await _db.OrderHeaders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderTime)
                    .ToListAsync();
                _response.Result = _mapper.Map<IEnumerable<OrderHeaderDto>>(orderHeaders);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpGet("GetOrder/{id}")]
        public async Task<ResponseDto> GetOrder(int id)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderHeaderId == id);
                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<ResponseDto> CreateOrder([FromBody] OrderHeaderDto orderHeaderDto)
        {
            try
            {
                var orderHeader = _mapper.Map<OrderHeader>(orderHeaderDto);
                orderHeader.OrderTime = DateTime.UtcNow;
                orderHeader.OrderStatus = "Pending";
                orderHeader.PaymentStatus = "Pending";

                _db.OrderHeaders.Add(orderHeader);
                await _db.SaveChangesAsync();

                // Publish order confirmed event
                var emailOrderQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailOrderQueue") ?? "emailorderqueue";
                var orderConfirmedQueue = _configuration.GetValue<string>("TopicAndQueueNames:OrderConfirmedQueue") ?? "orderconfirmedqueue";
                
                var createdOrderDto = _mapper.Map<OrderHeaderDto>(orderHeader);
                await _messageBus.PublishMessage(createdOrderDto, emailOrderQueue);
                await _messageBus.PublishMessage(createdOrderDto, orderConfirmedQueue);

                _response.Result = createdOrderDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPut("UpdateOrderStatus/{id}")]
        public async Task<ResponseDto> UpdateOrderStatus(int id, [FromBody] string status)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == id);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                orderHeader.OrderStatus = status;
                _db.OrderHeaders.Update(orderHeader);
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPost("CancelOrder/{id}")]
        public async Task<ResponseDto> CancelOrder(int id)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == id);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                if (orderHeader.OrderStatus == "Shipped" || orderHeader.OrderStatus == "Delivered")
                {
                    _response.IsSuccess = false;
                    _response.Message = "Cannot cancel order that is already shipped or delivered";
                    return _response;
                }

                orderHeader.OrderStatus = "Cancelled";
                _db.OrderHeaders.Update(orderHeader);
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPut("UpdatePaymentStatus/{id}")]
        public async Task<ResponseDto> UpdatePaymentStatus(int id, [FromBody] string paymentStatus)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == id);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                orderHeader.PaymentStatus = paymentStatus;
                _db.OrderHeaders.Update(orderHeader);
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [Authorize]
        [HttpPut("ValidateStripeSession")]
        public async Task<ResponseDto> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderHeaderId);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                // Stripe validation would go here
                // For now, just mark as approved
                orderHeader.PaymentStatus = "Approved";
                orderHeader.OrderStatus = "Confirmed";
                _db.OrderHeaders.Update(orderHeader);
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }
    }
}
