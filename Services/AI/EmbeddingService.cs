using OpenAI;
using OpenAI.Embeddings;
using System.Linq;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

/// <summary>
/// Service for generating and managing embeddings using OpenAI's embedding models
/// </summary>
public class EmbeddingService
{
    private readonly OpenAIClient _client;
    private const string EmbeddingModel = "text-embedding-3-small"; // Changed to 3-small for cost efficiency

    public EmbeddingService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found in environment variables.");

        _client = new OpenAIClient(apiKey);
    }

    /// <summary>
    /// Generate embeddings for multiple texts (batch processing)
    /// </summary>
    public async Task<Dictionary<string, float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
            throw new ArgumentException("Texts list cannot be empty", nameof(texts));     

        var result = new Dictionary<string, float[]>();
        var client = _client.GetEmbeddingClient(EmbeddingModel);

        // Process in batches to avoid rate limiting
        const int batchSize = 25;
        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var embeddings = await client.GenerateEmbeddingsAsync(batch);

            for (int j = 0; j < batch.Count && j < embeddings.Value.Count; j++)
            {
                var embeddingData = embeddings.Value[j];
                result[batch[j]] = embeddingData.ToFloats().ToArray();
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate cosine similarity between two embeddings
    /// Handles edge cases: zero norms, identical vectors, near-zero values
    /// </summary>
    public double CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
            throw new ArgumentException("Embeddings must have the same length");

        // Edge case: check for zero vectors or very small embeddings
        double norm1 = 0, norm2 = 0;
        for (int i = 0; i < embedding1.Length; i++)
        {
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }

        norm1 = Math.Sqrt(norm1);
        norm2 = Math.Sqrt(norm2);

        // If either vector is zero or very close to zero, return 0 similarity
        const double epsilon = 1e-10;
        if (norm1 < epsilon || norm2 < epsilon)
            return 0.0;

        double dotProduct = 0;
        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
        }

        // Clamp result to [-1, 1] to handle numerical precision issues
        double similarity = dotProduct / (norm1 * norm2);
        return Math.Clamp(similarity, -1.0, 1.0);
    }

    /// <summary>
    /// Serialize embedding vector to JSON for storage
    /// </summary>
    public string SerializeEmbedding(float[] embedding)
    {
        return JsonSerializer.Serialize(embedding);
    }

    /// <summary>
    /// Deserialize embedding vector from JSON
    /// </summary>
    public float[] DeserializeEmbedding(string json)
    {
        return JsonSerializer.Deserialize<float[]>(json);
    }

    /// <summary>
    /// Calculate centroid of multiple embeddings
    /// </summary>
    public float[] CalculateCentroid(List<float[]> embeddings)
    {
        if (embeddings.Count == 0)
            throw new ArgumentException("Embeddings list cannot be empty");

        var dimension = embeddings[0].Length;
        var centroid = new float[dimension];

        foreach (var embedding in embeddings)
        {
            for (int i = 0; i < dimension; i++)
            {
                centroid[i] += embedding[i];
            }
        }

        for (int i = 0; i < dimension; i++)
        {
            centroid[i] /= embeddings.Count;
        }

        return centroid;
    }
}
