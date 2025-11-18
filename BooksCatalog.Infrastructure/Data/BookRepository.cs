using BooksCatalog.Application.Common.Models;
using BooksCatalog.Application.DTOs;
using BooksCatalog.Application.Repositories;
using BooksCatalog.Domain.Entities;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace BooksCatalog.Infrastructure;
public sealed class BookRepository : IBookRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public BookRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    // ---------------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------------
    public async Task<Guid> CreateAsync(
        string title,
        string author,
        int publicationYear,
        string? publisher,
        int pageCount,
        string? category,
        string? isbn,
        string? language,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT usp_books_create(
                @p_title,
                @p_author,
                @p_publication_year,
                @p_publisher,
                @p_page_count,
                @p_category,
                @p_isbn,
                @p_language
            );
        ";

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("p_title", title);
        cmd.Parameters.AddWithValue("p_author", author);
        cmd.Parameters.AddWithValue("p_publication_year", publicationYear);
        cmd.Parameters.AddWithValue("p_publisher", (object?)publisher ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_page_count", pageCount);
        cmd.Parameters.AddWithValue("p_category", (object?)category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_isbn", (object?)isbn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_language", (object?)language ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync(cancellationToken)
                     ?? throw new InvalidOperationException("usp_books_create returned null.");

        return (Guid)result;
    }

    // ---------------------------------------------------------------------
    // READ BY ID
    // ---------------------------------------------------------------------
    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT *
            FROM usp_books_get_by_id(@p_id);
        ";

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("p_id", id);

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapBook(reader);
    }

    // ---------------------------------------------------------------------
    // READ PAGED
    // ---------------------------------------------------------------------
    public async Task<PagedResult<Book>> GetPagedAsync(
        string? search,
        string? category,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT *
            FROM usp_books_list_paged(
                @p_search,
                @p_category,
                @p_page_number,
                @p_page_size
            );
        ";

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("p_search", (object?)search ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_category", (object?)category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_page_number", pageNumber);
        cmd.Parameters.AddWithValue("p_page_size", pageSize);

        var items = new List<Book>();
        long totalCount = 0;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var book = MapBook(reader);
            totalCount = reader.GetInt64(reader.GetOrdinal("total_count"));
            items.Add(book);
        }

        return new PagedResult<Book>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    // ---------------------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------------------
    public async Task<bool> UpdateAsync(
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
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT usp_books_update(
                @p_id,
                @p_title,
                @p_author,
                @p_publication_year,
                @p_publisher,
                @p_page_count,
                @p_category,
                @p_isbn,
                @p_language,
                @p_expected_version
            );
        ";

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("p_id", id);
        cmd.Parameters.AddWithValue("p_title", title);
        cmd.Parameters.AddWithValue("p_author", author);
        cmd.Parameters.AddWithValue("p_publication_year", publicationYear);
        cmd.Parameters.AddWithValue("p_publisher", (object?)publisher ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_page_count", pageCount);
        cmd.Parameters.AddWithValue("p_category", (object?)category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_isbn", (object?)isbn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_language", (object?)language ?? DBNull.Value);
        cmd.Parameters.AddWithValue("p_expected_version", expectedVersion);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            return false;
        }

        var rows = Convert.ToInt32(result);
        return rows > 0;
    }

    // ---------------------------------------------------------------------
    // SOFT DELETE
    // ---------------------------------------------------------------------
    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT usp_books_soft_delete(@p_id);";

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("p_id", id);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            return false;
        }

        var rows = Convert.ToInt32(result);
        return rows > 0;
    }

    // ---------------------------------------------------------------------
    // Helper de mapeo Domain Entity
    // ---------------------------------------------------------------------
    private static Book MapBook(IDataRecord record)
    {
        var idxId = record.GetOrdinal("id");
        var idxTitle = record.GetOrdinal("title");
        var idxAuthor = record.GetOrdinal("author");
        var idxPubYear = record.GetOrdinal("publication_year");
        var idxPublisher = record.GetOrdinal("publisher");
        var idxPageCount = record.GetOrdinal("page_count");
        var idxCategory = record.GetOrdinal("category");
        var idxIsbn = record.GetOrdinal("isbn");
        var idxLanguage = record.GetOrdinal("language");
        var idxCreatedAt = record.GetOrdinal("created_at");
        var idxUpdatedAt = record.GetOrdinal("updated_at");
        var idxVersion = record.GetOrdinal("version");

        var createdAt = record.GetDateTime(idxCreatedAt);
        var updatedAt = record.GetDateTime(idxUpdatedAt);

        return new Book
        {
            Id = record.GetGuid(idxId),
            Title = record.GetString(idxTitle),
            Author = record.GetString(idxAuthor),
            PublicationYear = record.GetInt32(idxPubYear),
            Publisher = record.IsDBNull(idxPublisher) ? null : record.GetString(idxPublisher),
            PageCount = record.GetInt32(idxPageCount),
            Category = record.IsDBNull(idxCategory) ? null : record.GetString(idxCategory),
            Isbn = record.IsDBNull(idxIsbn) ? null : record.GetString(idxIsbn),
            Language = record.IsDBNull(idxLanguage) ? null : record.GetString(idxLanguage),
            CreatedAt = new DateTimeOffset(createdAt),
            UpdatedAt = new DateTimeOffset(updatedAt),
            Version = record.GetInt32(idxVersion)
        };
    }
}
