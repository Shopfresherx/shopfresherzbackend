using FluentValidation;
using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Review;
using ShopFresherz.Domain.Entities;
using ShopFresherz.Domain.Interfaces;

namespace ShopFresherz.Application.Features.Reviews.Commands;

/// <summary>Command for submitting a product review.</summary>
/// <param name="UserId">The authenticated reviewer's ID.</param>
/// <param name="Request">The review payload.</param>
public sealed record CreateReviewCommand(Guid UserId, CreateReviewRequest Request)
    : IRequest<Result<Guid>>;

/// <summary>Handler for <see cref="CreateReviewCommand"/>.</summary>
public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public CreateReviewCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        bool alreadyReviewed = await _uow.Reviews.UserHasReviewedAsync(
            command.UserId, command.Request.ProductId, cancellationToken);

        if (alreadyReviewed)
        {
            return Error.Conflict("You have already reviewed this product.");
        }

        Product? product = await _uow.Products.GetByIdAsync(
            command.Request.ProductId, cancellationToken);

        if (product is null)
        {
            return Error.NotFound("Product");
        }

        Review review = new()
        {
            UserId    = command.UserId,
            ProductId = command.Request.ProductId,
            Rating    = command.Request.Rating,
            Title     = command.Request.Title,
            Body      = command.Request.Body,
            IsApproved = false,
        };

        await _uow.Reviews.AddAsync(review, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(review.Id);
    }
}

/// <summary>Validator for <see cref="CreateReviewCommand"/>.</summary>
public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.Request.ProductId).NotEmpty();
        RuleFor(x => x.Request.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Request.Title).MaximumLength(200).When(x => x.Request.Title is not null);
        RuleFor(x => x.Request.Body).MaximumLength(2000).When(x => x.Request.Body is not null);
    }
}
