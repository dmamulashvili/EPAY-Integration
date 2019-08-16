# EPAY-Integration
Simple EPAY Integration with ASP.NET Core WebAPI, Razor Pages

![alt text](https://www.lucidchart.com/publicSegments/view/892fe3ec-1c40-46c7-a367-acfe11d2da3b/image.png)

## Configuration
1. Configuration model `EPAY.EPAYConfiguration.cs`:
```
public class EPAYConfiguration
{
    public string Username { get; set; }

    public string Password { get; set; }

    public string ServiceId { get; set; }

    public string SecretKey { get; set; }
}
```
2. Configuration sub-section in `appsettings.json`:
```
"EPAYConfiguration": {
  "Username": "",
  "Password": "",
  "ServiceId": "",
  "SecretKey": ""
}
```
3. Registered configuration in `Startup.cs`'s `ConfigureServices` method:
```
services.Configure<EPAYConfiguration>(Configuration.GetSection(nameof(EPAYConfiguration)));
```

## Integration
1. HTTP GET Request model `EPAY.EPAYRequest.cs`:
```
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
```
2. WebAPI Implementation
```
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

            await _context.SaveChangesAsync();

            return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK, receiptId: payment.Id));
        }

        return BadRequest();
    }
}
```
3. Razor Page Implementation `/Pages/EPAY/Index.cshtml.cs`:
```
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
        // NOTE: Some validations are missing, you can implement them on your own. (e.g. ResponseStatusCode.QueryParameterMissing, ResponseStatusCode.QueryParameterValueInvalid, etc.)

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
        var customer = await _context.Customers.SingleOrDefaultAsync(s => s.Mobile == EPAYRequest.CustomerId || s.Email.ToUpper() == EPAYRequest.CustomerId.ToUpper());

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

            await _context.SaveChangesAsync();

            return Content(EPAYHelper.BuildResponseContent(ResponseStatusCode.OK, receiptId: payment.Id));
        }

        return BadRequest();
    }
}
```
