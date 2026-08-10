using System;
using System.Collections.Generic;
using System.Text;

namespace DAL.Models.Common
{
    public interface ISoftDeletable
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
