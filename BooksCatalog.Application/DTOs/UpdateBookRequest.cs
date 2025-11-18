using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooksCatalog.Application.DTOs
{
    public sealed class UpdateBookRequest
    {
        public string Title { get; set; } = default!;
        public string Author { get; set; } = default!;
        public int PublicationYear { get; set; }
        public string? Publisher { get; set; }
        public int PageCount { get; set; }
        public string? Category { get; set; }
        public string? Isbn { get; set; }
        public string? Language { get; set; }

        // Concurrency token (version esperado)
        public int Version { get; set; }
    }
}
