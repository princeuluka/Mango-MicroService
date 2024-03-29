﻿using Mango.Services.ShoppingCartAPI.Models.Dto;
using System.Threading.Tasks;

namespace Mango.Services.ShoppingCartAPI.Service.IService
{
    public interface ICouponService
    {
        Task<CouponDto> GetCoupon(string couponCode);
    }
}
