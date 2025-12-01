using Silk.NET.OpenGL;
using Silk.NET.SDL;

public interface IImGuiPipeline : IService
{
    public ToolGui Tools { get; }
    unsafe void Initialize(GL glApi, Sdl sdlApi, Window* windowHandler, void* glContext);

    unsafe void BeginFrame();

    void EndFrame();
    unsafe void ProcessEvents(Event ev);

}