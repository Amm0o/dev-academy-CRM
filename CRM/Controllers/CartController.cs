using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly BasicCrud _basicCurd;
        private readonly ILogger<CartController> _logger;


        // TO DO: Add checks to ensure both of the parameters are never null
        public CartController(BasicCrud basicCrud, ILogger<CartController> logger)
        {
            _basicCurd = basicCrud ?? throw new ArgumentNullException(nameof(basicCrud));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        // GET: api/cart/{userId}
        [HttpGet("AddCart")]
        public IActionResult GetCart(int userId)
        {
            
        }


    }
}