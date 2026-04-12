using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        protected ResponseDto _response;
        private IMapper _mapper;
        private readonly IConfiguration _configuration;

        public OrderAPIController(AppDbContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _response = new ResponseDto();
            _mapper = mapper;
            _configuration = configuration;
        }

        [HttpGet("GetOrders")]
        [Authorize]
        public ResponseDto Get(string? userId = "")
        {
            try
            {
                IEnumerable<OrderHeader> objList;
                if (string.IsNullOrEmpty(userId))
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(u => u.OrderHeaderId).ToList();
                }
                else
                {
                    objList = _db.OrderHeaders.Include(u => u.OrderDetails).Where(u => u.UserId == userId)
                                .OrderByDescending(u => u.OrderHeaderId).ToList();
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

        [HttpGet("GetOrder/{id:int}")]
        [Authorize]
        public ResponseDto Get(int id)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.Include(u => u.OrderDetails).First(u => u.OrderHeaderId == id);
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
        public ResponseDto CreateOrder([FromBody] CartDto cartDto) // simplified, or use CheckoutHeader
        {
            try
            {
                OrderHeader orderHeader = _mapper.Map<OrderHeader>(cartDto.CartHeader);
                orderHeader.OrderTime = DateTime.Now;
                orderHeader.Status = SD.Status_Pending;
                orderHeader.OrderDetails = _mapper.Map<IEnumerable<OrderDetails>>(cartDto.CartDetails);
                _db.OrderHeaders.Add(orderHeader);
                _db.SaveChanges();

                _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        [Authorize]
        public ResponseDto UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderId);
                if (orderHeader != null)
                {
                    if (!IsValidStatusTransition(orderHeader.Status, newStatus))
                    {
                        _response.IsSuccess = false;
                        _response.Message = "Invalid status transition";
                        return _response;
                    }
                    orderHeader.Status = newStatus;
                    _db.SaveChanges();
                }
                _response.IsSuccess = true;
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
            string[] allowedStatuses = { SD.Status_Pending, SD.Status_Approved, SD.Status_ReadyForPickup, SD.Status_Completed, SD.Status_Cancelled };
            if (!allowedStatuses.Contains(newStatus))
            {
                return false;
            }

            if (currentStatus == SD.Status_Completed || currentStatus == SD.Status_Cancelled)
            {
                return false;
            }

            if (currentStatus == SD.Status_Pending)
            {
                return newStatus == SD.Status_Approved || newStatus == SD.Status_Cancelled;
            }
            if (currentStatus == SD.Status_Approved)
            {
                return newStatus == SD.Status_ReadyForPickup || newStatus == SD.Status_Cancelled;
            }
            if (currentStatus == SD.Status_ReadyForPickup)
            {
                return newStatus == SD.Status_Completed || newStatus == SD.Status_Cancelled;
            }

            return false;
        }

        [HttpPost("CreateStripeSession")]
        [Authorize]
        public ResponseDto CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    SuccessUrl = stripeRequestDto.ApprovedUrl,
                    CancelUrl = stripeRequestDto.CancelUrl,
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.ProductName
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);
                stripeRequestDto.StripeSessionId = session.Id;
                stripeRequestDto.StripeSessionUrl = session.Url;

                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                orderHeader.StripeSessionId = session.Id;
                _db.SaveChanges();
                _response.Result = stripeRequestDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return _response;
        }

        [HttpPost("ValidateStripeSession")]
        [Authorize]
        public ResponseDto ValidateStripeSession([FromBody] int orderHeaderId)
        {
            try
            {
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == orderHeaderId);

                var service = new SessionService();
                Session session = service.Get(orderHeader.StripeSessionId);

                var paymentIntentService = new PaymentIntentService();
                PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    orderHeader.PaymentIntentId = paymentIntent.Id;
                    orderHeader.Status = SD.Status_Approved;
                    _db.SaveChanges();
                    _response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                }
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
