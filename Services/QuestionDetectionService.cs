using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace meeting_copilot.Services;

/// <summary>
/// Service for detecting questions in transcribed text and calculating answer priority.
/// </summary>
public class QuestionDetectionService
{
    private readonly ILogger<QuestionDetectionService> _logger;

    // Question patterns ordered by confidence
    private static readonly (string Pattern, double Confidence)[] QuestionPatterns = new[]
    {
        // Direct question words at start
        (@"^(?:what|who|where|when|why|how|which|whose|whom)\b", 0.95),
        // "Can/Could/Would/Should you..." questions
        (@"^(?:can|could|would|should|will|do|does|did|is|are|was|were|have|has|had)\b.*\?", 0.9),
        // Questions ending with question mark
        (@"\?$", 0.85),
        // "Do you know..." style indirect questions
        (@"(?:do you know|can you tell|could you explain|would you mind)", 0.8),
        // Tag questions "..., right?" "..., isn't it?"
        (@",\s*(?:right|correct|isn't it|aren't you|don't you|won't you)\s*\??$", 0.75),
        // "I was wondering" style
        (@"(?:i was wondering|i'm wondering|wondering if)", 0.7),
    };

    // Role-based priority weights (lower = higher priority)
    private static readonly Dictionary<string, int> RolePriorityWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        // Executive roles - highest priority
        { "CEO", 1 },
        { "CTO", 2 },
        { "CFO", 2 },
        { "COO", 2 },
        { "Executive", 3 },
        { "VP", 4 },
        { "Vice President", 4 },
        // Management roles
        { "Director", 10 },
        { "Manager", 15 },
        { "Lead", 20 },
        { "Principal", 20 },
        // External stakeholders - very high priority
        { "Customer", 5 },
        { "Client", 5 },
        { "Partner", 8 },
        { "Investor", 3 },
        // Technical roles
        { "Architect", 25 },
        { "Senior", 30 },
        // Default
        { "Engineer", 50 },
        { "Developer", 50 },
        { "Analyst", 50 },
    };

    public QuestionDetectionService(ILogger<QuestionDetectionService> logger)
    {
        _logger = logger;
    }

    public record QuestionDetectionResult(
        bool IsQuestion,
        double Confidence,
        string? QuestionType,
        string Text
    );

    /// <summary>
    /// Detects if the given text contains a question.
    /// </summary>
    public QuestionDetectionResult DetectQuestion(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new QuestionDetectionResult(false, 0, null, text);
        }

        var normalizedText = text.Trim().ToLowerInvariant();
        
        foreach (var (pattern, confidence) in QuestionPatterns)
        {
            if (Regex.IsMatch(normalizedText, pattern, RegexOptions.IgnoreCase))
            {
                var questionType = DetermineQuestionType(normalizedText);
                _logger.LogDebug(
                    "Detected {Type} question with confidence {Confidence}: {Text}",
                    questionType,
                    confidence,
                    text.Substring(0, Math.Min(50, text.Length)));

                return new QuestionDetectionResult(true, confidence, questionType, text);
            }
        }

        return new QuestionDetectionResult(false, 0, null, text);
    }

    /// <summary>
    /// Determines the type of question (factual, clarification, procedural, etc.)
    /// </summary>
    private string DetermineQuestionType(string text)
    {
        if (Regex.IsMatch(text, @"^what\b"))
            return "Factual";
        if (Regex.IsMatch(text, @"^who\b"))
            return "Person";
        if (Regex.IsMatch(text, @"^where\b"))
            return "Location";
        if (Regex.IsMatch(text, @"^when\b"))
            return "Time";
        if (Regex.IsMatch(text, @"^why\b"))
            return "Reason";
        if (Regex.IsMatch(text, @"^how\b"))
            return "Procedural";
        if (Regex.IsMatch(text, @"^which\b"))
            return "Choice";
        if (Regex.IsMatch(text, @"(?:can|could|would|should)\s+(?:you|we|i)"))
            return "Request";
        if (Regex.IsMatch(text, @"(?:is|are|was|were|do|does|did)\b"))
            return "YesNo";
        
        return "General";
    }

    /// <summary>
    /// Extracts the core question from text (removing filler words).
    /// </summary>
    public string ExtractCoreQuestion(string text)
    {
        // Remove common filler phrases
        var cleaned = Regex.Replace(text, 
            @"(?:um|uh|like|you know|basically|actually|so|well)\s*,?\s*", 
            "", 
            RegexOptions.IgnoreCase);
        
        // Trim and clean up spacing
        return Regex.Replace(cleaned.Trim(), @"\s+", " ");
    }

    /// <summary>
    /// Determines if a question should be prioritized for immediate answer.
    /// </summary>
    public bool ShouldPrioritize(QuestionDetectionResult detection, string? speakerRole)
    {
        // High confidence questions are always prioritized
        if (detection.Confidence >= 0.9)
            return true;

        // Questions from key speakers (e.g., executives) are prioritized
        var priorityRoles = new[] { "CEO", "CTO", "VP", "Director", "Manager", "Lead", "Customer", "Client" };
        if (!string.IsNullOrEmpty(speakerRole) && 
            priorityRoles.Any(r => speakerRole.Contains(r, StringComparison.OrdinalIgnoreCase)))
        {
            return detection.Confidence >= 0.7;
        }

        return detection.Confidence >= 0.85;
    }

    /// <summary>
    /// Calculates the answer priority for a question based on speaker and question characteristics.
    /// Lower values = higher priority (should be answered first).
    /// </summary>
    /// <param name="detection">The question detection result.</param>
    /// <param name="speakerPriorityRank">The speaker's priority rank (1 = highest, 99 = lowest).</param>
    /// <param name="speakerRole">The speaker's role if known.</param>
    /// <param name="isSpeakerSelf">Whether the speaker is the user themselves.</param>
    /// <returns>Priority score where lower is more urgent (1-100 scale).</returns>
    public int CalculateAnswerPriority(
        QuestionDetectionResult detection, 
        int speakerPriorityRank = 99,
        string? speakerRole = null,
        bool isSpeakerSelf = false)
    {
        // Base priority from speaker rank (1-99, lower = more important)
        var priority = speakerPriorityRank;

        // Self-user questions get highest priority
        if (isSpeakerSelf)
        {
            priority = 1;
        }
        // Role-based adjustment if speaker role is known
        else if (!string.IsNullOrEmpty(speakerRole))
        {
            priority = GetRolePriority(speakerRole);
        }

        // Confidence adjustment: higher confidence questions get slight priority boost
        // Reduce priority by up to 10 points for high-confidence questions
        var confidenceBoost = (int)((detection.Confidence - 0.5) * 20);
        priority = Math.Max(1, priority - confidenceBoost);

        // Question type adjustment
        priority = AdjustForQuestionType(priority, detection.QuestionType);

        // Ensure priority is within bounds
        return Math.Clamp(priority, 1, 100);
    }

    /// <summary>
    /// Gets the priority weight for a given role.
    /// </summary>
    private int GetRolePriority(string role)
    {
        // Check for exact match first
        if (RolePriorityWeights.TryGetValue(role, out var exactPriority))
        {
            return exactPriority;
        }

        // Check for partial matches (e.g., "Senior Engineer" contains "Senior")
        foreach (var (knownRole, weight) in RolePriorityWeights)
        {
            if (role.Contains(knownRole, StringComparison.OrdinalIgnoreCase))
            {
                return weight;
            }
        }

        // Default priority for unknown roles
        return 75;
    }

    /// <summary>
    /// Adjusts priority based on question type.
    /// </summary>
    private int AdjustForQuestionType(int basePriority, string? questionType)
    {
        return questionType switch
        {
            // Clarification questions are urgent - speaker needs info to continue
            "YesNo" => Math.Max(1, basePriority - 5),
            "Request" => Math.Max(1, basePriority - 3),
            // Time-sensitive questions
            "Time" => Math.Max(1, basePriority - 2),
            // Factual questions are standard
            "Factual" => basePriority,
            "Procedural" => basePriority,
            // Other types
            _ => basePriority
        };
    }

    /// <summary>
    /// Updates a speaker's priority rank based on their role.
    /// Returns the suggested priority rank for storage.
    /// </summary>
    public int SuggestPriorityRank(string? role, bool isSelf)
    {
        if (isSelf)
            return 1;
        
        if (string.IsNullOrEmpty(role))
            return 99;

        return GetRolePriority(role);
    }
}
