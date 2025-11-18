using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooksCatalog.Domain.Exceptions
{
    public sealed class ConcurrencyException : DomainException
    {
        public ConcurrencyException(string message)
            : base(message)
        {
        }
    }
}
