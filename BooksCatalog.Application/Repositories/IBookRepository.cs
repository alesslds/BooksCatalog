using BooksCatalog.Application.Common.Models;
using BooksCatalog.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooksCatalog.Application.Repositories
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<PagedResult<Book>> GetPagedAsync(
            string? search,
            string? category,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        Task<Guid> CreateAsync(
            string title,
            string author,
            int publicationYear,
            string? publisher,
            int pageCount,
            string? category,
            string? isbn,
            string? language,
            CancellationToken cancellationToken);

        Task<bool> UpdateAsync(
            Guid id,
            string title,
            string author,
            int publicationYear,
            string? publisher,
            int pageCount,
            string? category,
            string? isbn,
            string? language,
            int expectedVersion,
            CancellationToken cancellationToken);
        Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
