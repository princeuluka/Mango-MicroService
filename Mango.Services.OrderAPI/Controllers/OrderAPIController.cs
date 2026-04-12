using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        protected ResponseDto _response;

        public OrderAPIController(AppDbContext db, IMapper mapper, IMessageBus messageBus, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _messageBus = messageBus;
            _configuration = configuration;
            _response = new ResponseDto();
        }

        [HttpGet("GetOrders/{userId}")]
        [Authorize]
        public async Task<ResponseDto> GetOrders(string userId)
        {
            try
            {
                IEnumerable<OrderHeader> objList;
                if (User.IsInRole("ADMIN"))
                {
                    objList = await _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(u => u.OrderHeaderId).ToListAsync();
                }
                else
                {
                    objList = await _db.OrderHeaders.Include(u => u.OrderDetails).Where(u => u.UserId == userId).OrderByDescending(u => u.OrderHeaderId).ToListAsync();
                }
                _response.Result = _mapper.Map<IEnumerable<OrderHeaderDto>>(objList);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpGet("GetOrder/{id}")]
        [Authorize]
        public async Task<ResponseDto> GetOrder(int id)
        {
            try
            {
                OrderHeader orderHeader = await _db.OrderHeaders.Include(u => u.OrderDetails).FirstAsync(u => u.OrderHeaderId == id);
                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("CreateOrder")]
        [Authorize]
        public async Task<ResponseDto> CreateOrder([FromBody] CartDto cartDto)
        {
            try
            {
                OrderHeader orderHeader = _mapper.Map<OrderHeader>(cartDto.CartHeader);
                orderHeader.OrderTime = DateTime.Now;
                orderHeader.Status = "Pending";
                await _db.OrderHeaders.AddAsync(orderHeader);
                await _db.SaveChangesAsync();

                foreach (var detail in cartDto.CartDetails)
                {
                    OrderDetails orderDetail = _mapper.Map<OrderDetails>(detail);
                    orderDetail.OrderHeaderId = orderHeader.OrderHeaderId;
                    orderDetail.ProductName = detail.Product?.Name ?? "";
                    orderDetail.Price = detail.Product?.Price ?? 0;
                    await _db.OrderDetails.AddAsync(orderDetail);
                }
                await _db.SaveChangesAsync();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);

                // publish for email
                await _messageBus.PublishMessage(cartDto, _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue"));
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        private readonly string[] _validStatuses = { "Pending", "Approved", "ReadyForPickup", "Completed", "Cancelled" };

        [HttpPost("UpdateOrderStatus/{orderId}")]
        [Authorize]
        public async Task<ResponseDto> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            try
            {
                if (!_validStatuses.Contains(newStatus))
                {
                    _response.IsSuccess = false;
                    _response.Message = "Invalid status value";
                    return _response;
                }

                OrderHeader orderHeader = await _db.OrderHeaders.FirstAsync(u => u.OrderHeaderId == orderId);
                if (!IsValidStatusTransition(orderHeader.Status, newStatus))
                {
                    _response.IsSuccess = false;
                    _response.Message = $"Invalid status transition from {orderHeader.Status} to {newStatus}";
                    return _response;
                }

                orderHeader.Status = newStatus;
                _db.OrderHeaders.Update(orderHeader);
                await _db.SaveChangesAsync();
                _response.Result = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            if (currentStatus == "Completed" || currentStatus == "Cancelled") return false;
            if (currentStatus == "Pending" && (newStatus == "Approved" || newStatus == "Cancelled")) return true;
            if (currentStatus == "Approved" && (newStatus == "ReadyForPickup" || newStatus == "Cancelled")) return true;
            if (currentStatus == "ReadyForPickup" && (newStatus == "Completed" || newStatus == "Cancelled")) return true;
            return false;
        }
    }
}
