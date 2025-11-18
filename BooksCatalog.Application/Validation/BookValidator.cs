using System;
using System.Collections.Generic;
using System.Linq;
using BooksCatalog.Application.DTOs;
using BooksCatalog.Domain.Exceptions;

namespace BooksCatalog.Application.Validation;
public static class BookValidator
{
    public static void ValidateCreate(CreateBookRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var errors = new Dictionary<string, List<string>>();

        ValidateCommon(
            request.Title,
            request.Author,
            request.PublicationYear,
            request.PageCount,
            request.Isbn,
            errors);

        ThrowIfAny(errors, "Validation failed when creating a book.");
    }

    public static void ValidateUpdate(UpdateBookRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var errors = new Dictionary<string, List<string>>();

        ValidateCommon(
            request.Title,
            request.Author,
            request.PublicationYear,
            request.PageCount,
            request.Isbn,
            errors);

        if (request.Version <= 0)
        {
            AddError(errors, nameof(request.Version), "Version must be greater than 0.");
        }

        ThrowIfAny(errors, "Validation failed when updating a book.");
    }

    private static void ValidateCommon(
        string title,
        string author,
        int publicationYear,
        int pageCount,
        string? isbn,
        Dictionary<string, List<string>> errors)
    {
        // Title
        if (string.IsNullOrWhiteSpace(title))
            AddError(errors, nameof(title), "Title is required.");

        if (!string.IsNullOrWhiteSpace(title) && title.Trim().Length > 200)
            AddError(errors, nameof(title), "Title must be at most 200 characters.");

        // Author
        if (string.IsNullOrWhiteSpace(author))
            AddError(errors, nameof(author), "Author is required.");

        if (!string.IsNullOrWhiteSpace(author) && author.Trim().Length > 200)
            AddError(errors, nameof(author), "Author must be at most 200 characters.");

        // PublicationYear
        var currentYear = DateTime.UtcNow.Year;
        if (publicationYear < 1450 || publicationYear > currentYear + 1)
        {
            AddError(errors, nameof(publicationYear),
                $"PublicationYear must be between 1450 and {currentYear + 1}.");
        }

        // PageCount
        if (pageCount <= 0 || pageCount > 10000)
        {
            AddError(errors, nameof(pageCount),
                "PageCount must be greater than 0 and at most 10000.");
        }

        // ISBN (formato simple: 10 o 13 dígitos sin guiones)
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            var cleaned = new string(isbn.Where(char.IsDigit).ToArray());
            if (cleaned.Length is not (10 or 13))
            {
                AddError(errors, nameof(isbn),
                    "ISBN must contain 10 or 13 digits when removing non-digit characters.");
            }
        }
    }

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            errors[field] = list;
        }
        list.Add(message);
    }

    private static void ThrowIfAny(Dictionary<string, List<string>> errors, string message)
    {
        if (errors.Count == 0)
            return;

        var final = errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        throw new DomainValidationException(message, final);
    }
}
