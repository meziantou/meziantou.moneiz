using System.Net;

namespace Meziantou.Moneiz.Services;

/// <summary>
/// Thrown when a GitHub operation fails because GitHub itself is unreachable or replies with a server error,
/// as opposed to a failure caused by the token, the repository name, or the content of the database.
/// </summary>
public sealed class GitHubUnavailableException : Exception
{
    /// <summary>
    /// The GitHub status page. It is displayed to the user so they can check for an ongoing incident.
    /// </summary>
    public const string StatusPageUrl = "https://www.githubstatus.com";

    public GitHubUnavailableException()
    {
    }

    public GitHubUnavailableException(string message) : base(message)
    {
    }

    public GitHubUnavailableException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public GitHubUnavailableException(string message, HttpStatusCode? statusCode, Exception? innerException) : base(message, innerException) => StatusCode = statusCode;

    /// <summary>
    /// The status code returned by GitHub, or <see langword="null"/> when GitHub could not be reached at all.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// The operation that failed, such as "Cannot download the database from GitHub". <see langword="null"/> when the exception was not created by <see cref="Create"/> or <see cref="CreateInvalidResponse"/>.
    /// </summary>
    public string? Operation { get; private init; }

    /// <summary>
    /// The explanation of the failure, without the <see cref="Operation"/>. <see langword="null"/> when the exception was not created by <see cref="Create"/> or <see cref="CreateInvalidResponse"/>.
    /// </summary>
    public string? Details { get; private init; }

    public static GitHubUnavailableException Create(string operation, HttpStatusCode? statusCode, Exception? innerException)
    {
        var reason = statusCode switch
        {
            null => "GitHub could not be reached. GitHub may be down, or this device may be offline.",
            HttpStatusCode.ServiceUnavailable => "GitHub is temporarily unavailable (HTTP 503 Service Unavailable). This usually means GitHub is down.",
            HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout => $"GitHub did not answer in time (HTTP {(int)statusCode.Value} {statusCode}). This usually means GitHub is down.",
            _ => $"GitHub replied with an unexpected error (HTTP {(int)statusCode.Value} {statusCode}).",
        };

        return CreateCore(operation, reason, statusCode, innerException);
    }

    public static GitHubUnavailableException CreateInvalidResponse(string operation, Exception? innerException)
        => CreateCore(operation, "GitHub replied with an unexpected response instead of the expected data. This usually means GitHub is down or that a network device altered the response.", statusCode: null, innerException);

    private static GitHubUnavailableException CreateCore(string operation, string reason, HttpStatusCode? statusCode, Exception? innerException)
    {
        var details = reason + " Nothing was changed on this device, so you can safely retry once GitHub is back.";
        return new GitHubUnavailableException($"{operation}: {details}", statusCode, innerException)
        {
            Operation = operation,
            Details = details,
        };
    }
}
