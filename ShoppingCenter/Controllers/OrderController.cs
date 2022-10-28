﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingCenter.Database;
using ShoppingCenter.Models;

namespace ShoppingCenter.Controllers
{
    [Route("api/Order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ShopContext _context;
        public OrderController(ShopContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromHeader] string? token)
        {
            var userinfo = _context.Users.Where(w => w.Fio + w.Id == token).SingleOrDefault();
            if (userinfo == null)
                return Unauthorized();

            var orders = await _context.Orders
                .Where(w => w.UserId == userinfo.Id)
                .Select(t => new OrderDto()
                {
                    Id = t.Id,
                    CreatedDate = t.CreatedDate,
                    Number = t.Number,
                    ClientId = t.ClientId,
                    UserId = t.UserId
                }).ToListAsync();
            return Ok(orders);
        }


        [HttpPost]
        public async Task<IActionResult> PostOrders([FromBody] OrderPostDto orderPostDto, [FromHeader] string token)
        {
            var userinfo = _context.Users.Where(w => w.Fio + w.Id == token).SingleOrDefault();
            if (userinfo == null)  
                 return Unauthorized();

            var ordernew = new Order()
            {
                CreatedDate = DateTime.Now,
                Number = orderPostDto.Number,
                ClientId = orderPostDto.ClientId,
                UserId = userinfo.Id                
            };
            _context.Orders.Add(ordernew);
            _context.SaveChanges();

            var goodsIds = orderPostDto.Positions.Select(t => t.GoodsId).ToList();

            var goods = await _context.Goods.Where(x => goodsIds.Contains(x.Id)).ToListAsync();

            foreach(var item in orderPostDto.Positions)
            {
                var findgoods = goods.Where(w => w.Id == item.GoodsId).FirstOrDefault();

                if (findgoods != null) {
                    var position = new OrderComposition()
                    {
                        GoodsId = item.GoodsId,
                        Count = item.Count,
                        OrderId = ordernew.Id,
                        Price = findgoods.Price,
                    };

                    _context.Compositions.Add(position);
                }

            }
            await _context.SaveChangesAsync();


            return Ok();
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder([FromRoute] int id, [FromHeader] string? token)
        {
            var userinfo = _context.Users.Where(w => w.Fio + w.Id == token).SingleOrDefault();
            if (userinfo == null)
                return Unauthorized();

            var order = await _context.Orders
                .Where(w => w.Id == id)
                .Select(t => new OrderDto()
                {
                    Id = t.Id,
                    CreatedDate = t.CreatedDate,
                    UserId = t.UserId,
                    Number = t.Number,
                    ClientId = t.ClientId,
                    ClientName = t.Client.Fio,
                    UserName = t.User.Fio,
                    Positions = t.Compositions.Select(p => new OrderCompositionsDto()
                    {
                        GoodsId = p.GoodsId,
                        Count = p.Count,
                        Price = p.Price,
                        GoodsName = p.Goods.Name
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}