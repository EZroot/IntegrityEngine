using Silk.NET.OpenGL;
using Silk.NET.SDL;

public interface IRenderPipeline : IService
{
    GL? GlApi { get; }
    unsafe void InitializeRenderer(Sdl sdlApi, Window* window);
    void DrawTexture(GLTexture texture);
    void DrawTextureAt(GLTexture texture, float x, float y, float width, float height);
    void RenderFrameStart();
    void RenderFrameEnd();
}