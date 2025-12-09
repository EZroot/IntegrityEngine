using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.SDL;

namespace Integrity.Interface;
public interface IRenderPipeline : IService
{
    GL? GlApi { get; }
    unsafe void InitializeRenderer(Sdl sdlApi, Window* window);
    unsafe void DrawSpritesInstanced(Assets.Texture texture, in List<Matrix4x4> modelMatrices, in List<Vector4> uvRects, int instanceCount);

    void RenderFrameStart();
    void RenderFrameEnd();
    unsafe void UpdateViewportSize(int width, int height);
    unsafe void SetProjectionMatrix(in Matrix4x4 matrix);
    void SetClearColor(System.Drawing.Color color);
}