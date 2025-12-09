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

    public AudioManager()
    {
    }

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

    public void PlaySound(string soundPath)
    {
        // TODO: 
        // Should use sound name, or id
        // Load through assetmanager or grab from cache (not here)
        // Play the sound at position x/y
        // 
    }
    
    public unsafe void Shutdown()
    {
        if (m_AlcApi == null || m_Context == nint.Zero || m_Device == nint.Zero) return;

        m_AlcApi.MakeContextCurrent(nint.Zero);
        
        m_AlcApi.DestroyContext((Context*)m_Context);
        m_AlcApi.CloseDevice((Device*)m_Device);

        m_Context = nint.Zero;
        m_Device = nint.Zero;
        m_AlApi = null;
        m_AlcApi = null;
    }
}