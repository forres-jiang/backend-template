using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using My.XXX.APIs.Common;
using My.XXX.APIs.Models;
using My.XXX.Contracts.DTOs;
using My.XXX.Infrastructure.Security;
using My.XXX.Services.Examples.Interfaces;
using My.XXX.Services.Menus.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace My.XXX.APIs.Controllers
{
    [NonController] // 仅作示例；这些操作并非 HTTP 端点。
    [ApiController]
    [Route("api/[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly IStringLocalizer<DemoController> _localizer;
        private readonly IMenuService _menuService;
        private readonly IDemoService _demoService;
        private readonly IConnectionMultiplexer _redis;

        public DemoController(
            IStringLocalizer<DemoController> localizer,
            IDemoService demoService,
            IMenuService menuService,
            IConnectionMultiplexer redis)
        {
            _redis = redis;
            _demoService = demoService;
            _menuService = menuService;
            _localizer = localizer;
        }

        [HttpGet("Index")]
        public MyResult Index()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            //http://localhost:5000/swagger/index.html​
            string localIp = NetworkInterface.GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .FirstOrDefault(p => p.Address.AddressFamily == AddressFamily.InterNetwork
                            && !IPAddress.IsLoopback(p.Address))?.Address.ToString();
            var s = JsonConvert.SerializeObject(new { name = "test" });

            return MyResult.Success(new
            {
                Environment = environment,
                LocalIp = localIp,
                HostName = Dns.GetHostName(),
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Localization = _localizer["test"].Value,
                cache = DateTime.Now.Month
            });
        }

        [AllowAnonymous]
        [HttpGet("Exception")]
        public string Exception(string id)
        {
            throw new NotImplementedException("Exception.Exception.Exception...");
        }

        [AllowAnonymous]
        [HttpGet("Test/{id}")]
        public string Test(string id)
        {
            return AESHelper.Encrypt(id);
        }

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("Encrypt")]
        public string Encrypt([FromForm] string str)
        {
            return AESHelper.Encrypt(str);
        }

        [AllowAnonymous]
        [HttpPost("Decrypt")]
        public string Decrypt([FromForm] string str)
        {
            return AESHelper.Decrypt(str);
        }

        [AllowAnonymous]
        [HttpPost, IgnoreMetrics]
        [Route("Save")]
        public MyResult Save(DemoModel model)
        {
            if (!ModelState.IsValid)
            {
                return MyResult.Fail("errors");
            }
            return _demoService.Save(model).ToApiResult();
        }

        [HttpPost("Update")]
        public MyResult Update(DemoModel model)
        {
            return _demoService.Update(model).ToApiResult();
        }

        [HttpPost("RoleMenuAction")]
        public async Task<MyResult> RoleMenuAction(RoleMenuActionModel model)
        {
            return (await _menuService.RoleMenuAction(model, HttpContext.RequestAborted)).ToApiResult();
        }

        [AllowAnonymous]
        [HttpGet("ResultTest")]
        public BaseResult ResultTest()
        {
            return MyResult.Success();
        }

        [AllowAnonymous]
        [HttpGet("test")]
        public bool Test()
        {
            return true;
        }

        [AllowAnonymous]
        [HttpGet("redis")]
        public async Task<string> Redis()
        {
            var key = DateTime.Now.ToString("yyyyMMddHHmmss");
            await _redis.GetDatabase().StringSetAsync(key, "dddddddd-" + key, TimeSpan.FromHours(10));
            string cache = await _redis.GetDatabase().StringGetAsync(key);
            return cache;
        }

        [AllowAnonymous]
        [HttpPost("upload")]
        public IActionResult Upload()
        {
            var files = Request.Form.Files;
            long size = files.Sum(f => f.Length);
            var list = new List<string>();
            return Ok(new
            {
                count = files.Count,
                size,
                path = list
            });
        }
    }
}