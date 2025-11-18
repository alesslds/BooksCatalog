using BooksCatalog.Application.Common.Models;
using BooksCatalog.Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BooksCatalog.Application.Services;
public interface IBookService
{
    Task<PagedResult<BookDto>> GetPagedAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BookDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BookDto> CreateAsync(CreateBookRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, UpdateBookRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
