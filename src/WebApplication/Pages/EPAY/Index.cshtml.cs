using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApplication.Data;
using EPAY;

namespace WebApplication.Pages.EPAY
{
    [BindProperties(SupportsGet = true)]
    public class IndexModel : PageModel
    {
        private readonly EPAYConfiguration _epayConfig;
        private readonly ApplicationDbContext _context;

        public IndexModel(IOptions<EPAYConfiguration> options
            //, ApplicationDbContext context
            )
        {
            _epayConfig = options.Value;
            //_context = context;
        }

        public EPAYRequest EPAYRequest { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // NOTE: Some validations are missing, you can implement them on your own. (e.g ResponseStatusCode.QueryParameterMissing, ResponseStatusCode.QueryParameterValueInvalid, etc)

            if (!EPAYHelper.IsValidEPAYRequest(HttpContext.Request, EPAYRequest, _epayConfig, out string responseContent))
            {
                return Content(responseContent);
            }

            if (!Enum.TryParse(EPAYRequest.OP, true, out OperationType operationType))
            {
                throw new NotSupportedException(EPAYRequest.OP.ToString());
            }

            if (operationType == OperationType.PING)
            {
                return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK));
            }

            // EPAYRequest.CustomerId is Customer Mobile or Email
            var customer = await _context.Customers.SingleOrDefaultAsync(s => s.Mobile == EPAYRequest.CustomerId || s.Email.Equals(EPAYRequest.CustomerId, StringComparison.OrdinalIgnoreCase));

            if (customer == null)
            {
                return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.CustomerNotFound));
            }

            if (operationType == OperationType.VERIFY)
            {
                return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK));
            }

            if (operationType == OperationType.DEBT)
            {
                return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK, EPAYHelper.ConvertGELToEPAYAmount(customer.Balance), customer.FirstName, customer.LastName));
            }

            if (operationType == OperationType.PAY)
            {
                var duplicatePayment = await _context.Payments.SingleOrDefaultAsync(s => s.ExternalId == EPAYRequest.PaymentId);
                if(duplicatePayment != null)
                {
                    return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.InvalidPaymentIdNonUnique));
                }

                var payment = new Payment
                {
                    ExternalId = EPAYRequest.PaymentId,
                    ExternalAadditionalInformation = EPAYRequest.ExtraInfo,
                    Amount = EPAYHelper.ConvertEPAYAmountToGEL(EPAYRequest.PayAmount),
                    CustomerId = customer.Id
                };
                await _context.Payments.AddAsync(payment);

                customer.Balance += payment.Amount;
                _context.Customers.Update(customer);

                await _context.SaveChangesAsync();

                return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK, receiptId: payment.Id));
            }

            return BadRequest();
        }
    }
}