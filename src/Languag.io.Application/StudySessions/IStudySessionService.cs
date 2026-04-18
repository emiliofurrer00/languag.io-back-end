namespace Languag.io.Application.StudySessions;

public interface IStudySessionService
{
    Task<SubmitStudySessionResult> SubmitAsync(
        SubmitStudySessionCommand command,
        Guid userId,
        CancellationToken ct = default);
}
