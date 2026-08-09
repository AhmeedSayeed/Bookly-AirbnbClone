using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Enums
{
    public enum PaymentStatus
    {
        Pending = 1,
        Success,
        Failed,
        Refunded,
        Voided
    }
}
