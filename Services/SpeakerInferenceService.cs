using Microsoft.Extensions.Logging;

namespace meeting_copilot.Services;

/// <summary>
/// Service for inferring speaker names from transcript context using NLP patterns.
/// </summary>
public class SpeakerInferenceService
{
    private readonly ILogger<SpeakerInferenceService> _logger;
    private static readonly string[] IntroductionPatterns = new[]
    {
        @"(?:my name is|I'm|I am|this is)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)",
        @"([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\s+(?:here|speaking|joining)",
        @"(?:hello|hi|hey)[,\s]+(?:I'm|I am|this is)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)",
    };

    private static readonly string[] RolePatterns = new[]
    {
        @"([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\s+(?:from|with|at)\s+(\w+(?:\s+\w+)?)",
        @"(?:I'm|I am)\s+(?:the|a)\s+(\w+(?:\s+\w+)?)\s+(?:at|for|from)",
    };

    public SpeakerInferenceService(ILogger<SpeakerInferenceService> logger)
    {
        _logger = logger;
    }

    public record SpeakerInference(
        string? Name,
        string? Role,
        string? Organization,
        double Confidence
    );

    /// <summary>
    /// Attempts to infer speaker identity from transcript text.
    /// </summary>
    public SpeakerInference InferFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SpeakerInference(null, null, null, 0);
        }

        string? inferredName = null;
        string? inferredRole = null;
        string? inferredOrg = null;
        double confidence = 0;

        // Try introduction patterns
        foreach (var pattern in IntroductionPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                inferredName = match.Groups[1].Value.Trim();
                confidence = 0.8;
                _logger.LogDebug("Inferred name '{Name}' from introduction pattern", inferredName);
                break;
            }
        }

        // Try role patterns
        foreach (var pattern in RolePatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (match.Groups.Count > 2)
                {
                    inferredRole = match.Groups[1].Value.Trim();
                    inferredOrg = match.Groups[2].Value.Trim();
                }
                else
                {
                    inferredRole = match.Groups[1].Value.Trim();
                }
                confidence = Math.Max(confidence, 0.6);
                _logger.LogDebug("Inferred role '{Role}' at '{Org}'", inferredRole, inferredOrg);
                break;
            }
        }

        return new SpeakerInference(inferredName, inferredRole, inferredOrg, confidence);
    }

    /// <summary>
    /// Checks if text contains a self-introduction.
    /// </summary>
    public bool ContainsIntroduction(string text)
    {
        return IntroductionPatterns.Any(pattern =>
            System.Text.RegularExpressions.Regex.IsMatch(
                text,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
