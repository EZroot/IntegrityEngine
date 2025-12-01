using Silk.NET.SDL;

public interface IWindowPipeline : IService
{
    unsafe Window* WindowHandler { get; }
    unsafe void* GlContext { get; }
    void InitializeWindow(Sdl sdlApi, string title, int width, int height,  int useVsync);
    bool ShouldClose();
}