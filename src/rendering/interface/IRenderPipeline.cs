using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.SDL;

public interface IRenderPipeline : IService
{
    GL? GlApi { get; }
    unsafe void InitializeRenderer(Sdl sdlApi, Window* window);
    void DrawSprite(SpriteComponent sprite, TransformComponent transform);
    void RenderFrameStart();
    void RenderFrameEnd();
    unsafe void UpdateViewportSize(int width, int height);
    unsafe void SetProjectionMatrix(in Matrix4x4 matrix);
}