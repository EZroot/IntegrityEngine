using Silk.NET.OpenAL;

namespace Integrity.Assets;

/// <summary>
/// Stores raw audio sample data and its defining format characteristics.
/// </summary>
public struct AudioData
{
    public byte[] SampleData;
    public int SampleRate;
    public int Channels;
    public int BitsPerSample;

    /// <summary>
    /// Gets the appropriate OpenAL BufferFormat enum value.
    /// </summary>
    /// <returns>The OpenAL format.</returns>
    public BufferFormat GetFormat()
    {
        if (Channels == 1) // Mono
        {
            if (BitsPerSample == 8)
                return BufferFormat.Mono8;
            if (BitsPerSample == 16)
                return BufferFormat.Mono16;
        }
        else if (Channels == 2) // Stereo
        {
            if (BitsPerSample == 8)
                return BufferFormat.Stereo8;
            if (BitsPerSample == 16)
                return BufferFormat.Stereo16;
        }

        throw new NotSupportedException($"Unsupported audio format: {Channels} channels, {BitsPerSample} bits.");
    }

    public AudioData(byte[] sampleData, int sampleRate, int channels, int bitsPerSample)
    {
        SampleData = sampleData;
        SampleRate = sampleRate;
        Channels = channels;
        BitsPerSample = bitsPerSample;
    }
}