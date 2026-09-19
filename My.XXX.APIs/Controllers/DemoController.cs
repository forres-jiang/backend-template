using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using My.XXX.APIs.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Shared;
using My.XXX.Shared.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace My.XXX.APIs.Controllers
{
    [NonController] // Examples only; these operations are not HTTP endpoints.
    [ApiController]
    [Route("api/[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly IStringLocalizer<DemoController> _localizer;
        private readonly IAppCenterService _appCenterService;
        private readonly IMailService _mailService;
        private readonly IMenuService _menuService;
        private readonly IDemoService _demoService;

        public DemoController(
            IStringLocalizer<DemoController> localizer,
            IAppCenterService appCenterService,
            IDemoService demoService,
            IMenuService menuService,
            IMailService mailService)
        {
            _appCenterService = appCenterService;
            _demoService = demoService;
            _menuService = menuService;
            _mailService = mailService;
            _localizer = localizer;
        }

        [HttpGet("Index")]
        public MyResult Index()
        {
            //var r = new DataValidator<UserRole>();
            //r.Valid(null);
            //_demoService.ExecProc();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            //http://localhost:5000/swagger/index.html​
            string localIp = NetworkInterface.GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses)
                .FirstOrDefault(p => p.Address.AddressFamily == AddressFamily.InterNetwork
                            && !IPAddress.IsLoopback(p.Address))?.Address.ToString();
            var s = JsonConvert.SerializeObject(new { name = "test" });
            //_logger.LogError("{env}", environment);

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

        [HttpGet("SendEmail")]
        public MyResult SendEmail()
        {
            var sc = "Test" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var model = new Mail
            {
                MFROM = "CNHK GTS SDC Support",
                MTO = "Forres Jiang/CN/GTS",
                SUBJECT = sc,
                CONTENT = sc,
                SENDDATE = DateTime.Now
            };
            return _mailService.SendEmail(model).ToApiResult();
        }

        [AllowAnonymous]
        [HttpPost("SendEmailWithAttachment")]
        public bool SendEmailWithAttachment(List<IFormFile> files)
        {
            if (files.Count == 0)
                return false;

            var file = files.First();
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                fileBytes = ms.ToArray();
            }

            var sc = "Test" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var model = new Mail
            {
                MFROM = "CNHK GTS SDC Support",
                MTO = "Forres Jiang/CN/GTS",
                SUBJECT = sc,
                CONTENT = sc,
                SENDDATE = DateTime.Now
            };

            return _mailService.SendEmailWithFile(model, fileBytes, file.FileName, "application/zip").IsSuccess;
        }

        [HttpGet("File/{id}")]
        public IActionResult GetFile(int id)
        {
            var result = _mailService.GetAttachment(id);
            Stream stream = new MemoryStream(result.AttachmentContent);
            var fileResult = new FileStreamResult(stream, result.AttachmentMimeType)
            {
                FileDownloadName = result.AttachmentFileName
            };
            return fileResult;
        }

        [AllowAnonymous]
        [HttpGet("BulkCopy")]
        public MyResult BulkCopy()
        {
            string key = DateTime.Now.ToString("HHssmmfff");
            RedisHelper.Set("XXX" + key, key, TimeSpan.FromHours(1));
            return _mailService.BatchInsertEmail().ToApiResult();
        }

        [UnifyResult]
        [HttpGet("retry")]
        public async Task<IActionResult> Export()
        {
            var token = await _appCenterService.GetToken();
            return new ContentResult() { Content = token };
        }

        [AllowAnonymous]
        [HttpGet("GetRoles")]
        public async Task<List<Role>> GetRoles()
        {
            var result = await _appCenterService.GetRole();
            //await _appCenterService.GetUserByTicket("test");
            return result;
        }

        [HttpPost("RoleMenuAction")]
        public MyResult RoleMenuAction(RoleMenuActionModel model)
        {
            return _menuService.RoleMenuAction(model).ToApiResult();
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
        public string Redis()
        {
            var key = DateTime.Now.ToString("yyyyMMddHHmmss");
            RedisHelper.Set(key, "dddddddd-" + key, TimeSpan.FromHours(10));
            string cache = RedisHelper.Get(key);
            return cache;
        }

        [HttpGet("flurl")]
        public async Task<string> Flurl()
        {
            var s = await _appCenterService.GetUserByTicket("aaa");
            return s.UserName;
        }

        [AllowAnonymous]
        [HttpPost("upload")]
        public IActionResult Upload()
        {
            var files = Request.Form.Files;
            long size = files.Sum(f => f.Length);
            var list = new List<string>();
            //foreach (var formFile in files)
            //{
            //    if (formFile.Length > 0)
            //    {
            //        var allPath = wwwroot + "/" + formFile.FileName;
            //        list.Add(allPath);
            //        using var stream = System.IO.File.Create(allPath);
            //        await formFile.CopyToAsync(stream);
            //    }
            //}
            return Ok(new
            {
                count = files.Count,
                size,
                path = list
            });
        }
    }
}