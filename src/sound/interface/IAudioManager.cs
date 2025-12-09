using Integrity.Assets;
using Silk.NET.OpenAL;

namespace Integrity.Interface;
public interface IAudioManager : IService
{
    public AL AlApi { get; }
    void Initialize();
    void Update();
    void PlaySound(AudioClip clip);
    void Shutdown();
}