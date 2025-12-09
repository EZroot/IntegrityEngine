using Integrity.Assets;
using Integrity.Interface;
using Silk.NET.OpenAL;
using System.Numerics;

namespace Integrity.Sound;

public class AudioManager : IAudioManager
{
    private AL? m_AlApi;
    private ALContext? m_AlcApi;
    private nint m_Device;
    private nint m_Context;

    private readonly List<uint> m_ActiveSources = new List<uint>();

    public AL AlApi => m_AlApi ?? throw new Exception("ALAPI isn't enabled!");

    /// <summary>
    /// Initialize OpenAL and Audio Device
    /// </summary>
    public unsafe void Initialize()
    {
        m_AlApi = AL.GetApi();
        m_AlcApi = ALContext.GetApi();

        if (m_AlApi == null || m_AlcApi == null)
        {
            Logger.Log("Failed to get OpenAL or ALContext API instance.", Logger.LogSeverity.Error);
            return;
        }

        m_Device = (nint)m_AlcApi.OpenDevice(null);

        if (m_Device == nint.Zero)
        {
            Logger.Log("Failed to open OpenAL device.", Logger.LogSeverity.Error);
            return;
        }

        m_Context = m_AlcApi.CreateContextHandle((Device*)m_Device, null);
        if (m_Context == nint.Zero)
        {
            Logger.Log("Failed to create OpenAL context.", Logger.LogSeverity.Error);
            m_AlcApi.CloseDevice((Device*)m_Device);
            return;
        }

        if (!m_AlcApi.MakeContextCurrent(m_Context))
        {
            Logger.Log("Failed to make OpenAL context current.", Logger.LogSeverity.Error);

            m_AlcApi.DestroyContext((Context*)m_Context);

            m_AlcApi.CloseDevice((Device*)m_Device);
            return;
        }

        m_AlApi.SetListenerProperty(ListenerFloat.Gain, 1.0f);
        m_AlApi.SetListenerProperty(ListenerVector3.Position, new Vector3(0.0f, 0.0f, 0.0f));
        m_AlApi.SetListenerProperty(ListenerVector3.Velocity, new Vector3(0.0f, 0.0f, 0.0f));

        Logger.Log("OpenAL initialized successfully.", Logger.LogSeverity.Info);
    }

    /// <summary>
    /// Keeps track of active audio sources and clean up
    /// </summary>
    public void Update()
    {
        if (m_AlApi == null) return;

        for (int i = m_ActiveSources.Count - 1; i >= 0; i--)
        {
            uint source = m_ActiveSources[i];

            m_AlApi.GetSourceProperty(source, GetSourceInteger.SourceState, out int stateInt);
            SourceState state = (SourceState)stateInt;

            if (state == SourceState.Stopped)
            {
                m_AlApi.SetSourceProperty(source, SourceInteger.Buffer, 0);

                m_AlApi.DeleteSource(source);

                m_ActiveSources.RemoveAt(i);

                Logger.Log($"<color=green>Cleaned up and deleted OpenAL source ID</color>: {source}", Logger.LogSeverity.Info);
            }
        }
    }

    /// <summary>
    /// Play a sound by a loaded audio clip
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySound(AudioClip clip)
    {
        if (m_AlApi == null)
        {
            Logger.Log("Cannot play sound: OpenAL API is not initialized.", Logger.LogSeverity.Error);
            return;
        }

        uint source;
        unsafe
        {
            m_AlApi.GenSources(1, &source);
        }

        if (m_AlApi.GetError() != AudioError.NoError)
        {
            Logger.Log("Failed to generate OpenAL source.", Logger.LogSeverity.Error);
            return;
        }

        m_AlApi.SetSourceProperty(source, SourceInteger.Buffer, (int)clip.BufferId);

        m_AlApi.SetSourceProperty(source, SourceFloat.Gain, 1.0f);
        m_AlApi.SetSourceProperty(source, SourceBoolean.Looping, false);
        m_AlApi.SetSourceProperty(source, SourceVector3.Position, new Vector3(0.0f, 0.0f, 0.0f));

        m_AlApi.SourcePlay(source);

        m_ActiveSources.Add(source);

        Logger.Log($"Playing sound using OpenAL source ID: {source}", Logger.LogSeverity.Info);
    }

    public unsafe void Shutdown()
    {
        if (m_AlcApi == null || m_Context == nint.Zero || m_Device == nint.Zero) return;

        if (m_AlApi != null)
        {
            foreach (uint source in m_ActiveSources)
            {
                m_AlApi.DeleteSource(source);
            }
            m_ActiveSources.Clear();
            Logger.Log($"Cleaned up {m_ActiveSources.Count} remaining OpenAL sources.", Logger.LogSeverity.Info);
        }

        m_AlcApi.MakeContextCurrent(nint.Zero);

        m_AlcApi.DestroyContext((Context*)m_Context);
        m_AlcApi.CloseDevice((Device*)m_Device);

        m_Context = nint.Zero;
        m_Device = nint.Zero;
        m_AlApi = null;
        m_AlcApi = null;
    }
}