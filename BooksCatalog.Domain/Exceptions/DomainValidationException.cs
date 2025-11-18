using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooksCatalog.Domain.Exceptions
{
    public sealed class DomainValidationException : DomainException
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public DomainValidationException(
            string message,
            IDictionary<string, string[]> errors)
            : base(message)
        {
            Errors = new ReadOnlyDictionary<string, string[]>(errors);
        }
    }
}
