using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Features.Promotions.Commands;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.API.Controllers;

/// <summary>Uploads media files (image or video) for promotional sections.</summary>
[ApiController]
[Route("api/v1/promotions/admin/{sectionId:guid}/media")]
public sealed class PromotionalMediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromotionalMediaController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Uploads an image or video file for a promotional section background.
    /// Supported image types: jpg, jpeg, png, webp.
    /// Supported video types: mp4, webm.
    /// Max file size: 50MB.
    /// </summary>
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadMedia(
        [FromRoute] Guid sectionId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { Code = "VALIDATION", Message = "No file uploaded." });
        }

        string contentType = file.ContentType ?? "application/octet-stream";

        using Stream stream = file.OpenReadStream();

        Result<string> result = await _mediator.Send(
            new UploadPromoMediaCommand(sectionId, stream, file.FileName, file.Length, contentType),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new { url = result.Value })
            : MapError(result.Error);
    }

    private IActionResult MapError(Error error) => error.Code switch
    {
        "NOT_FOUND"  => NotFound(new { error.Code, error.Message }),
        "VALIDATION" => BadRequest(new { error.Code, error.Message }),
        _            => StatusCode(StatusCodes.Status500InternalServerError, new { error.Code, error.Message }),
    };
}
