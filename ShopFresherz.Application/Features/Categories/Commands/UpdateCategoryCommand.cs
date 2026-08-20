using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Product;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Categories.Commands;

/// <summary>Admin command for updating an existing category.</summary>
public sealed record UpdateCategoryCommand(int Id, UpdateCategoryRequest Request) : IRequest<Result<bool>>;

/// <summary>Handler for <see cref="UpdateCategoryCommand"/>.</summary>
public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateCategoryCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        Category? category = await _uow.Categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null) return Error.NotFound("Category");

        UpdateCategoryRequest req = command.Request;

        if (req.Name is not null) category.Name = req.Name.Trim();

        if (req.Slug is not null)
        {
            string newSlug = req.Slug.Trim().ToLowerInvariant();
            bool slugTaken = await _uow.Categories.SlugExistsAsync(newSlug, command.Id, cancellationToken);
            if (slugTaken)
            {
                return Error.Conflict($"A category with slug '{newSlug}' already exists.");
            }
            category.Slug = newSlug;
        }

        if (req.ParentId.HasValue)
        {
            if (req.ParentId.Value == command.Id)
            {
                return Error.Validation("A category cannot be its own parent.");
            }
            Category? parent = await _uow.Categories.GetByIdAsync(req.ParentId.Value, cancellationToken);
            if (parent is null) return Error.NotFound("Parent category");
            category.ParentId = req.ParentId.Value;
        }

        if (req.ImageUrl is not null)       category.ImageUrl        = req.ImageUrl.Trim();
        if (req.SortOrder.HasValue)         category.SortOrder       = req.SortOrder.Value;
        if (req.IsActive.HasValue)          category.IsActive        = req.IsActive.Value;
        if (req.MetaTitle is not null)      category.MetaTitle       = req.MetaTitle.Trim();
        if (req.MetaDescription is not null) category.MetaDescription = req.MetaDescription.Trim();

        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

/// <summary>Validator for <see cref="UpdateCategoryCommand"/>.</summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        When(x => x.Request.Name is not null,
            () => RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(150));
        When(x => x.Request.Slug is not null,
            () => RuleFor(x => x.Request.Slug).NotEmpty().MaximumLength(200));
        When(x => x.Request.MetaTitle is not null,
            () => RuleFor(x => x.Request.MetaTitle).MaximumLength(70));
        When(x => x.Request.MetaDescription is not null,
            () => RuleFor(x => x.Request.MetaDescription).MaximumLength(160));
    }
}
