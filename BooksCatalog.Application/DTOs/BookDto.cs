using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooksCatalog.Application.DTOs
{
    public sealed class BookDto
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = default!;
        public string Author { get; init; } = default!;
        public int PublicationYear { get; init; }
        public string? Publisher { get; init; }
        public int PageCount { get; init; }
        public string? Category { get; init; }
        public string? Isbn { get; init; }
        public string? Language { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public int Version { get; init; }
    }
}
