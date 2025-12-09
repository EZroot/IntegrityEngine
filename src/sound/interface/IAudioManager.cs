namespace Integrity.Interface;
public interface IAudioManager : IService
{
    void Initialize();
    void PlaySound(string soundPath);
    unsafe void Shutdown();
}