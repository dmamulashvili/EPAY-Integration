using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPAY
{
    public enum ResponseStatusCode
    {
        OK = 0,
        AccessDenied = 1,
        InvalidUsernamePassword = 2,
        InvalidHashCode = 3,
        QueryParameterMissing = 4,
        QueryParameterValueInvalid = 5,
        CustomerNotFound = 6,
        InvalidAmount = 7,
        InvalidPaymentIdNonUnique = 8,
        PaymentNotAvailable = 9,
        InvalidServiceId = 10,
        ValidPaymentIdNonUnique = 18,
        Error = 99
    }
}
