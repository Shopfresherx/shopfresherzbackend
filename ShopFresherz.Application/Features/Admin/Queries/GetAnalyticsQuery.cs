using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Admin;

namespace ShopFresherz.Application.Features.Admin.Queries;

/// <summary>Query for admin analytics data.</summary>
public sealed record GetAnalyticsQuery() : IRequest<Result<AnalyticsDto>>;