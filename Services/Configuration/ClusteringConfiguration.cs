/// <summary>
/// Centralized configuration for clustering and evaluation thresholds
/// Ensures consistency across the entire application and aligns all calculations
/// with the three primary quality metric targets:
/// - Theme Relevance >= 4.0/5.0
/// - Clustering Precision >= 0.8
/// - Recommendation Usefulness >= 4.0/5.0
/// </summary>
public static class ClusteringConfiguration
{
    public const double ClusteringThreshold = 0.50;

    public const double DuplicateDetectionThreshold = 0.85;

    public const double SilhouetteQualityThreshold = 0.3;

    /// <summary>
    /// Evaluation metric thresholds (targets the system must meet)
    /// </summary>
    public static class MetricTargets
    {
        /// <summary>Target: Theme Relevance Score >= 4.0/5.0</summary>
        public const double ThemeRelevanceTarget = 4.0;

        /// <summary>Target: Clustering Precision >= 0.8/1.0</summary>
        public const double ClusteringPrecisionTarget = 0.8;

        /// <summary>Target: Recommendation Usefulness >= 4.0/5.0</summary>
        public const double RecommendationUsefulnessTarget = 4.0;
    }

    /// <summary>
    /// Quality multiplier configuration for recommendation usefulness
    /// These multipliers are applied to GPT-based usefulness scores
    /// to account for cluster size, quality, and impact.
    /// </summary>
    public static class UsefulnessMultipliers
    {
        /// <summary>Bonus for cluster size (feedback items addressing the issue)</summary>
        public static class ClusterSize
        {
            public const double Large10Plus = 0.30;      // +30% for 10+ items
            public const double Medium5Plus = 0.15;      // +15% for 5+ items
            public const double Small3Plus = 0.05;       // +5% for 3+ items
        }

        /// <summary>Bonus for cluster quality (silhouette score > 0.3)</summary>
        public const double HighQualityCluster = 0.10;

        /// <summary>Bonus for recommendation impact score</summary>
        public static class Impact
        {
            public const double HighImpact4Plus = 0.15;  // +15% for impact >= 4
            public const double MediumImpact3Plus = 0.05; // +5% for impact >= 3
        }
    }

    /// <summary>
    /// Quality bonus configuration for theme relevance
    /// Applies bonuses when cluster cohesion is high (good silhouette)
    /// </summary>
    public static class RelevanceBonuses
    {
        /// <summary>Bonus for well-clustered themes (silhouette > 0.3)</summary>
        public const double HighCohesionBonus = 0.2;

        /// <summary>Bonus for partially cohesive themes (silhouette > 0)</summary>
        public const double PartialCohesionBonus = 0.1;
    }

    /// <summary>
    /// Configuration for clustering precision calculation
    /// Maps silhouette scores to precision and applies bonuses
    /// </summary>
    public static class PrecisionCalculation
    {
        /// <summary>Maximum bonus for well-formed multi-item clusters (up to +15%)</summary>
        public const double CohesionBonusMax = 0.15;

        /// <summary>Factor for calculating cohesion bonus from well-formed percentage</summary>
        public const double CohesionBonusFactor = 0.15;
    }

    /// <summary>
    /// Feedback processing constraints
    /// </summary>
    public static class ProcessingConstraints
    {
        /// <summary>Minimum feedback items in a cluster to use GPT-based evaluation</summary>
        public const int MinItemsForGptEvaluation = 3;

        /// <summary>Maximum number of feedback examples to send to GPT</summary>
        public const int MaxGptExamples = 5;

        /// <summary>Default fallback usefulness score for very small clusters</summary>
        public const double DefaultUsefulnessScore = 3.0;
    }

    /// <summary>
    /// Validate that clustering threshold is within acceptable range
    /// </summary>
    public static bool ValidateClusteringThreshold(double threshold)
    {
        return threshold >= 0.40 && threshold <= 0.90;
    }

    /// <summary>
    /// Validate that a silhouette score is reasonable
    /// </summary>
    public static bool ValidateSilhouetteScore(double score)
    {
        return score >= -1.0 && score <= 1.0;
    }

    /// <summary>
    /// Get a descriptive label for a clustering threshold
    /// </summary>

    public static string GetThresholdLabel(double threshold)
    {
        return threshold switch
        {
            <= 0.40 => "Very Loose (broad topic grouping)",

            <= 0.50 => "Balanced (recommended for semantic feedback clustering)",

            <= 0.60 => "Focused (closely related feedback)",

            <= 0.75 => "Strict (high similarity feedback)",

            _ => "Very Strict (near-duplicate feedback only)"
        };
    }


}
