using CRM.Infra;
using CRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
            _basicCurd = basicCrud;
            _logger = logger;
        }


        [HttpPost("register")]
        

    }
}