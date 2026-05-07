namespace Languag.io.Application.AiSagaGeneration;

public sealed class AiSagaGenerationWeeklyLimitExceededException : Exception
{
    public AiSagaGenerationWeeklyLimitExceededException(Exception innerException)
        : base("AI saga generation weekly limit was exceeded.", innerException)
    {
    }
}
