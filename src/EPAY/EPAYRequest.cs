using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPAY
{
    public class EPAYRequest
    {
        [BindProperty(Name = "OP")]
        public string OP { get; set; }

        [BindProperty(Name = "USERNAME")]
        public string Username { get; set; }

        [BindProperty(Name = "PASSWORD")]
        public string Password { get; set; }

        [BindProperty(Name = "CUSTOMER_ID")]
        public string CustomerId { get; set; }

        [BindProperty(Name = "SERVICE_ID")]
        public string ServiceId { get; set; }

        [BindProperty(Name = "PAY_AMOUNT")]
        public int PayAmount { get; set; }

        [BindProperty(Name = "PAY_SRC")]
        public string PaySource { get; set; }

        [BindProperty(Name = "PAYMENT_ID")]
        public string PaymentId { get; set; }

        [BindProperty(Name = "EXTRA_INFO")]
        public string ExtraInfo { get; set; }

        [BindProperty(Name = "HASH_CODE")]
        public string HashCode { get; set; }
    }
}
