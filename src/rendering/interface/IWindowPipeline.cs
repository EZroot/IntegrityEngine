using Silk.NET.SDL;

namespace Integrity.Interface;
public interface IWindowPipeline : IService
{
    unsafe Window* WindowHandler { get; }
    unsafe void* GlContext { get; }
    void InitializeWindow(Sdl sdlApi, string title, int width, int height, int useVsync);
    void SetVSync(int useVsync);
    bool ShouldClose();
}