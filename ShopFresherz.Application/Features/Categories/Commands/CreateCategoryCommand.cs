using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Categories.Commands;

/// <summary>Admin command for creating a new category.</summary>
public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<Result<int>>;

/// <summary>Handler for <see cref="CreateCategoryCommand"/>.</summary>
public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<int>>
{
    private readonly IUnitOfWork _uow;

    public CreateCategoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        CreateCategoryRequest req = command.Request;

        string slug = string.IsNullOrWhiteSpace(req.Slug)
            ? GenerateSlug(req.Name)
            : req.Slug.Trim().ToLowerInvariant();

        bool slugTaken = await _uow.Categories.SlugExistsAsync(slug, null, cancellationToken);
        if (slugTaken)
        {
            return Error.Conflict($"A category with slug '{slug}' already exists.");
        }

        // Validate parent if supplied.
        if (req.ParentId.HasValue)
        {
            Category? parent = await _uow.Categories.GetByIdAsync(req.ParentId.Value, cancellationToken);
            if (parent is null)
            {
                return Error.NotFound("Parent category");
            }
        }

        Category category = new()
        {
            Name            = req.Name.Trim(),
            Slug            = slug,
            ParentId        = req.ParentId,
            ImageUrl        = req.ImageUrl?.Trim(),
            SortOrder       = req.SortOrder,
            IsActive        = req.IsActive,
            MetaTitle       = req.MetaTitle?.Trim(),
            MetaDescription = req.MetaDescription?.Trim(),
        };

        await _uow.Categories.AddAsync(category, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(category.Id);
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("'", string.Empty)
            .Replace("&", "and");
    }
}

/// <summary>Validator for <see cref="CreateCategoryCommand"/>.</summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(150);
        When(x => x.Request.Slug is not null,
            () => RuleFor(x => x.Request.Slug).NotEmpty().MaximumLength(200));
        When(x => x.Request.MetaTitle is not null,
            () => RuleFor(x => x.Request.MetaTitle).MaximumLength(70));
        When(x => x.Request.MetaDescription is not null,
            () => RuleFor(x => x.Request.MetaDescription).MaximumLength(160));
    }
}
