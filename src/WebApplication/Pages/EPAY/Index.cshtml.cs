using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using WebApplication.EPAY;

namespace WebApplication.Pages.EPAY
{
    [BindProperties(SupportsGet = true)]
    public class IndexModel : PageModel
    {
        private readonly EPAYConfiguration _config;

        public IndexModel(IOptions<EPAYConfiguration> options)
        {
            _config = options.Value;
        }

        public EPAYRequest EPAYRequest { get; set; }

        public IActionResult OnGet()
        {
            if(!HttpContext.Request.IsValidEPAYRequest(EPAYRequest, _config, out string responseContent))
            {
                return Content(responseContent);
            }

            return Content(string.Empty);
        }
    }
}