using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        protected ResponseDto _response;

        public OrderAPIController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
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
                    .Where(u => u.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
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
        [HttpGet("GetOrder/{orderId}")]
        public async Task<ResponseDto> GetOrder(int orderId)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);
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
        public async Task<ResponseDto> CreateOrder([FromBody] OrderDto orderDto)
        {
            try
            {
                var orderHeader = _mapper.Map<OrderHeader>(orderDto.OrderHeader);
                orderHeader.CreatedAt = DateTime.Now;
                orderHeader.OrderStatus = OrderStatus.Pending;
                orderHeader.PaymentStatus = "Pending";

                _db.OrderHeaders.Add(orderHeader);
                await _db.SaveChangesAsync();

                foreach (var detail in orderDto.OrderDetails)
                {
                    var orderDetails = _mapper.Map<OrderDetails>(detail);
                    orderDetails.OrderHeaderId = orderHeader.OrderHeaderId;
                    _db.OrderDetails.Add(orderDetails);
                }
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
        [HttpPost("UpdateStatus/{orderId}")]
        public async Task<ResponseDto> UpdateStatus(int orderId, [FromBody] string status)
        {
            try
            {
                if (!OrderStatus.IsValid(status))
                {
                    _response.IsSuccess = false;
                    _response.Message = $"Invalid order status: {status}";
                    return _response;
                }

                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                if (!OrderStatus.CanTransition(orderHeader.OrderStatus, status))
                {
                    _response.IsSuccess = false;
                    _response.Message = $"Cannot transition order from {orderHeader.OrderStatus} to {status}";
                    return _response;
                }

                orderHeader.OrderStatus = status;
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
        [HttpPost("CancelOrder/{orderId}")]
        public async Task<ResponseDto> CancelOrder(int orderId)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                if (!OrderStatus.CanCancel(orderHeader.OrderStatus))
                {
                    _response.IsSuccess = false;
                    _response.Message = $"Cannot cancel order with status {orderHeader.OrderStatus}";
                    return _response;
                }

                orderHeader.OrderStatus = OrderStatus.Cancelled;
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
        [HttpPost("UpdatePaymentStatus/{orderId}")]
        public async Task<ResponseDto> UpdatePaymentStatus(int orderId, [FromBody] string paymentStatus)
        {
            try
            {
                var orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);
                if (orderHeader == null)
                {
                    _response.IsSuccess = false;
                    _response.Message = "Order not found";
                    return _response;
                }

                orderHeader.PaymentStatus = paymentStatus;
                if (paymentStatus == "Approved" && orderHeader.OrderStatus == OrderStatus.Pending)
                {
                    orderHeader.OrderStatus = OrderStatus.Confirmed;
                }
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
