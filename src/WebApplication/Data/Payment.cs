using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Data
{
    public class Payment
    {
        public int Id { get; set; }

        public string ExternalId { get; set; }

        public string ExternalAadditionalInformation { get; set; }

        public decimal Amount { get; set; }

        public int CustomerId { get; set; }
    }
}
