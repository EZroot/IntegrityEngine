using Silk.NET.OpenAL;

namespace Integrity.Assets;

public class AudioClip : IDisposable
{
    public uint BufferId { get; private set; }
    public int SampleRate { get; }
    public int Channels { get; }

    private readonly AL m_AlApi;

    public AudioClip(AL alApi, AudioData audioData)
    {
        m_AlApi = alApi;
        SampleRate = audioData.SampleRate;
        Channels = audioData.Channels;

        BufferId = CreateBuffer(audioData);
    }

    private unsafe uint CreateBuffer(AudioData audioData)
    {
        uint bufferId = m_AlApi.GenBuffer();

        BufferFormat format = audioData.GetFormat();
        int sizeInBytes = audioData.SampleData.Length;

        fixed (byte* ptr = audioData.SampleData)
        {
            m_AlApi.BufferData(
                bufferId,
                format,
                ptr,
                sizeInBytes,
                audioData.SampleRate
            );
        }

        AudioError error = m_AlApi.GetError();
        if (error != AudioError.NoError)
        {
            Logger.Log($"AudioClip GetBuffer: {error}", Logger.LogSeverity.Error);
        }

        return bufferId;
    }

    /// <summary>
    /// Deletes the OpenAL buffer when the audio clip is no longer needed.
    /// </summary>
    public void Dispose()
    {
        m_AlApi.DeleteBuffer(BufferId);
    }
}