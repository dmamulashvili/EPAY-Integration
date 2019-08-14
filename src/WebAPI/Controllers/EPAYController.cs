using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPAY;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI.Data;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EPAYController : ControllerBase
    {
        private readonly EPAYConfiguration _epayConfig;
        private readonly ApplicationDbContext _context;

        public EPAYController(IOptions<EPAYConfiguration> options
            //, ApplicationDbContext context
            )
        {
            _epayConfig = options.Value;
            //_context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]EPAYRequest epayRequest)
        {
            // NOTE: Some validations are missing, you can implement them on your own. (e.g ResponseStatusCode.QueryParameterMissing, ResponseStatusCode.QueryParameterValueInvalid, etc)
            
            if (!EPAYHelper.IsValidEPAYRequest(HttpContext.Request, epayRequest, _epayConfig, out string responseContent))
            {
                return Content(responseContent);
            }

            if (!Enum.TryParse(epayRequest.OP, true, out OperationType operationType))
            {
                throw new NotSupportedException(epayRequest.OP.ToString());
            }

            if (operationType == OperationType.PING)
            {
                return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK));
            }

            // EPAYRequest.CUSTOMER_ID is Customer Mobile or Email
            var customer = await _context.Customers.SingleOrDefaultAsync(s => s.Mobile == epayRequest.CustomerId || s.Email.ToUpper() == epayRequest.CustomerId.ToUpper());

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
                var duplicatePayment = await _context.Payments.SingleOrDefaultAsync(s => s.ExternalId == epayRequest.PaymentId);
                if (duplicatePayment != null)
                {
                    return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.InvalidPaymentIdNonUnique));
                }

                var payment = new Payment
                {
                    ExternalId = epayRequest.PaymentId,
                    ExternalAadditionalInformation = epayRequest.ExtraInfo,
                    Amount = EPAYHelper.ConvertEPAYAmountToGEL(epayRequest.PayAmount),
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