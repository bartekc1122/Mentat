namespace Mentat.Infrastructure.Search;

/// <summary>Konwersja embeddingów float[] ↔ byte[] (do zapisu w SQLite) oraz podobieństwo kosinusowe.</summary>
public static class VectorMath
{
    public static byte[] ToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
        return vector;
    }

    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0d;

        double dot = 0d, normA = 0d, normB = 0d;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0d || normB == 0d)
            return 0d;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
