using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingCenter.Database;
using ShoppingCenter.Models;
using System.Security.Claims;

namespace ShoppingCenter.Controllers
{
    [Route("api/Order")]
    [Authorize]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ShopContext _context;
        public OrderController(ShopContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var user = HttpContext.User.Claims.Where(c => c.Type == "Id").FirstOrDefault();
            if (user == null) return Unauthorized();

            var orders = await _context.Orders
                .Where(w => w.UserId == Convert.ToInt32(user.Value))
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
                .ToListAsync();
            return Ok(orders);
        }


        [HttpPost]
        public async Task<IActionResult> PostOrders([FromBody] OrderPostDto orderPostDto)
        {
            var userinfo = HttpContext.User.Claims.Where(c => c.Type == "Id").FirstOrDefault();
            if (userinfo == null) return Unauthorized();

            var ordernew = new Order()
            {
                CreatedDate = DateTime.Now,
                Number = orderPostDto.Number,
                ClientId = orderPostDto.ClientId,
                UserId = Convert.ToInt32(userinfo.Value)
            };
            _context.Orders.Add(ordernew);
            _context.SaveChanges();

            var goodsIds = orderPostDto.Positions.Select(t => t.GoodsId).ToList();
            var goods = await _context.Goods.Where(x => goodsIds.Contains(x.Id)).ToListAsync();
            foreach (var item in orderPostDto.Positions)
            {
                var findgoods = goods.Where(w => w.Id == item.GoodsId).FirstOrDefault();
                if (findgoods != null)
                {
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

        [HttpPut]
        public async Task<IActionResult> PutOrders([FromBody] OrderPostDto orderPostDto, [FromQuery] int orderId)
        {
            var userinfo = HttpContext.User.Claims.Where(c => c.Type == "Id").FirstOrDefault();
            if (userinfo == null)
                return Unauthorized();
            Order order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
            order.Number = orderPostDto.Number;
            order.ClientId = orderPostDto.ClientId;

            foreach (var item in orderPostDto.Positions)
            {
                OrderComposition poser = _context.Compositions.Where(w => w.Order.User.Id == Convert.ToInt32(userinfo.Value)).FirstOrDefault();
                poser.GoodsId = item.GoodsId;
                poser.Count = item.Count;
            }

            foreach (var item in orderPostDto.Positions)
            {




            }




            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete]        
        public async Task<IActionResult> DeleteOrders([FromHeader] int id)
        {
            var userinfo = HttpContext.User.Claims.Where(c => c.Type == "Id").FirstOrDefault();
            if (userinfo == null)
                return Unauthorized();
            var order = await _context.Orders.SingleOrDefaultAsync(w => w.Id == id);

            _context.Orders.Remove(order);
            _context.SaveChanges();
            return Ok();
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder([FromRoute] int id)
        {
            var userinfo = HttpContext.User.Claims.Where(c => c.Type == "Id").FirstOrDefault();
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
