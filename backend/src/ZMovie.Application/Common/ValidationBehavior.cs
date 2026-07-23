using ErrorOr;
using FluentValidation;
using MediatR;

namespace ZMovie.Application.Common;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, ErrorOr<TResponse>>
    where TRequest : IRequest<ErrorOr<TResponse>>
{
    public async Task<ErrorOr<TResponse>> Handle(TRequest request, RequestHandlerDelegate<ErrorOr<TResponse>> next, CancellationToken cancellationToken)
    {
        var errors = validators.Select(x => x.Validate(new ValidationContext<TRequest>(request)))
            .SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .Select(x => Error.Validation(x.PropertyName, x.ErrorMessage))
            .ToList();
        return errors.Count == 0 ? await next(cancellationToken) : errors;
    }
}
