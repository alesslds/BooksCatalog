using BooksCatalog.Application.Common.Models;
using BooksCatalog.Application.DTOs;
using BooksCatalog.Application.Repositories;
using BooksCatalog.Application.Validation;
using BooksCatalog.Domain.Entities;
using BooksCatalog.Domain.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BooksCatalog.Application.Services;

public sealed class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<PagedResult<BookDto>> GetPagedAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Normalizamos la paginación.
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var result = await _bookRepository.GetPagedAsync(
            search,
            category,
            pageNumber,
            pageSize,
            cancellationToken);

        var dtoItems = result.Items.Select(MapToDto).ToArray();

        return new PagedResult<BookDto>
        {
            Items = dtoItems,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems
        };
    }

    public async Task<BookDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);

        if (book is null)
            throw new NotFoundException($"Book with id '{id}' was not found.");

        return MapToDto(book);
    }

    public async Task<BookDto> CreateAsync(
        CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        BookValidator.ValidateCreate(request);

        var title = request.Title.Trim();
        var author = request.Author.Trim();
        var publisher = string.IsNullOrWhiteSpace(request.Publisher) ? null : request.Publisher.Trim();
        var category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
        var language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();
        var publicationYear = request.PublicationYear;
        var pageCount = request.PageCount;

        var id = await _bookRepository.CreateAsync(
            title,
            author,
            publicationYear,
            publisher,
            pageCount,
            category,
            isbn,
            language,
            cancellationToken);

        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book is null)
        {
            throw new NotFoundException($"Book with id '{id}' was not found.");
        }


        return MapToDto(book);
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        // Validamos campos y versión
        BookValidator.ValidateUpdate(request);

        // Revisamos que exista (y no esté soft-deleted)
        var existing = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Book with id '{id}' was not found.");
        }

        // Normalizamos strings (trim).
        var title = request.Title.Trim();
        var author = request.Author.Trim();
        var publisher = string.IsNullOrWhiteSpace(request.Publisher) ? null : request.Publisher.Trim();
        var category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
        var language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();
        var publicationYear = request.PublicationYear;
        var pageCount = request.PageCount;

        var updated = await _bookRepository.UpdateAsync(
            id,
            title,
            author,
            publicationYear,
            publisher,
            pageCount,
            category,
            isbn,
            language,
            request.Version,
            cancellationToken);

        if (!updated)
        {
            // Si no se actualizó ninguna fila, asumimos conflicto de concurrencia.
            throw new ConcurrencyException(
                $"Book with id '{id}' was modified by another process. Please reload and try again.");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _bookRepository.SoftDeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException($"Book with id '{id}' was not found.");
        }
    }

    private static readonly TimeSpan LimaOffset = TimeSpan.FromHours(-5);

    private static BookDto MapToDto(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        PublicationYear = book.PublicationYear,
        Publisher = book.Publisher,
        PageCount = book.PageCount,
        Category = book.Category,
        Isbn = book.Isbn,
        Language = book.Language,
        // Convertimos de UTC (o lo que venga) a hora de Lima
        CreatedAt = book.CreatedAt.ToOffset(LimaOffset),
        UpdatedAt = book.UpdatedAt.ToOffset(LimaOffset),
        Version = book.Version
    };
}
