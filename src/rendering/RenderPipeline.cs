using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Integrity.Core;
using Integrity.Interface;
using Integrity.Settings;
using Silk.NET.OpenGL;
using Silk.NET.SDL;

namespace Integrity.Rendering;
public class RenderPipeline : IRenderPipeline
{
    private static readonly float[] s_QuadVertices = {
        // Position (X, Y) | Texture Coord (U, V)
        0.0f, 1.0f, 0.0f, 1.0f, // Top-Left
        0.0f, 0.0f, 0.0f, 0.0f, // Bottom-Left
        1.0f, 0.0f, 1.0f, 0.0f, // Bottom-Right

        0.0f, 1.0f, 0.0f, 1.0f, // Top-Left
        1.0f, 0.0f, 1.0f, 0.0f, // Bottom-Right
        1.0f, 1.0f, 1.0f, 1.0f  // Top-Right
    };

    private Sdl? m_SdlApi;
    private GL? m_GlApi;

    private uint m_ShaderProgramId;
    private uint m_VaoId;
    private uint m_VboId;

    private uint m_ColorVboId;
    private nuint m_ColorBufferCapacityBytes;
    
    private uint m_InstanceVboId;
    private nuint m_InstanceBufferCapacityBytes = 0;

    private uint m_UvRectVboId;
    private nuint m_UvBufferCapacityBytes = 0;

    private uint m_TileShaderProgramId;
    private uint m_TileVaoId;
    private int m_TileProjectionUniformLocation;
    private int m_TileModelUniformLocation;

    private int m_ProjectionUniformLocation;

    private unsafe Window* m_WindowHandler;
    private readonly ClearBufferMask m_ClearBufferMask = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;
    private System.Drawing.Color m_ClearColor = System.Drawing.Color.CornflowerBlue;

    public GL? GlApi => m_GlApi;

    /// <summary>
    /// Initializes the renderer with the given SDL API and window.
    /// </summary>
    /// <param name="sdlApi"></param>
    /// <param name="window"></param>
    public unsafe void InitializeRenderer(Sdl sdlApi, Window* window)
    {
        m_SdlApi = sdlApi;
        m_WindowHandler = window;
        m_GlApi = GL.GetApi(GetProcAddress);
        if (m_GlApi == null)
        {
            throw new Exception("Failed to initialize OpenGL API in RenderPipeline.");
        }

        var settings = Service.Get<IEngineSettings>();
        Debug.Assert(settings != null, "Engine Settings service not found in RenderPipeline.");
        UpdateViewportSize(settings.Data.WindowWidth, settings.Data.WindowHeight);

        m_ShaderProgramId = CreateShaderProgram(
            Path.Combine(EngineSettings.SHADER_DIR, "default.vert"),
            Path.Combine(EngineSettings.SHADER_DIR, "default.frag")
        );
        m_ProjectionUniformLocation = m_GlApi.GetUniformLocation(m_ShaderProgramId, "projection");

        m_TileShaderProgramId = CreateShaderProgram(
            Path.Combine(EngineSettings.SHADER_DIR, "tile.vert"),
            Path.Combine(EngineSettings.SHADER_DIR, "tile.frag") 
        );

        m_TileProjectionUniformLocation = m_GlApi.GetUniformLocation(m_TileShaderProgramId, "projection");
        m_TileModelUniformLocation = m_GlApi.GetUniformLocation(m_TileShaderProgramId, "model");

        SetupTileMeshVao();
        SetupQuadMesh();
    }

    /// <summary>
    /// Draws multiple sprites using a single draw call by utilizing instanced rendering.
    /// </summary>
    /// <param name="texture">The Texture containing the shared texture.</param>
    /// <param name="modelMatrices">A list of Model matrices for all instances.</param>
    /// <param name="uvRects">A list of UV coordinates for atlas.</param>
    /// <param name="colors">A list colors for sprite component.</param>
    /// <param name="instanceCount">The number of sprites to draw.</param>
    public unsafe void DrawSpritesInstanced(Assets.Texture texture, in List<Matrix4x4> modelMatrices, in List<Vector4> uvRects, in List<Vector4> colors, int instanceCount)
    {
        if (instanceCount == 0 || m_GlApi == null) return;

        m_GlApi.UseProgram(m_ShaderProgramId);
        texture.Use(TextureUnit.Texture0);

        int modelDataSizeInBytes = instanceCount * sizeof(Matrix4x4);
        EnsureBufferCapacity((nuint)modelDataSizeInBytes, m_InstanceVboId, ref m_InstanceBufferCapacityBytes);
        int uvDataSizeInBytes = instanceCount * sizeof(Vector4);
        EnsureBufferCapacity((nuint)uvDataSizeInBytes, m_UvRectVboId, ref m_UvBufferCapacityBytes);
        int colorDataSizeInBytes = instanceCount * sizeof(Vector4);
        EnsureBufferCapacity((nuint)colorDataSizeInBytes, m_ColorVboId, ref m_ColorBufferCapacityBytes); 

        // Color
        var colorSpan = CollectionsMarshal.AsSpan(colors).Slice(0, instanceCount);
        fixed (Vector4* colorDataPtr = &MemoryMarshal.GetReference(colorSpan))
        {
            m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_ColorVboId);
            m_GlApi.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)colorDataSizeInBytes, colorDataPtr);
        }

        // Position
        var modelSpan = CollectionsMarshal.AsSpan(modelMatrices).Slice(0, instanceCount);
        fixed (Matrix4x4* dataPtr = &MemoryMarshal.GetReference(modelSpan))
        {
            m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_InstanceVboId);
            m_GlApi.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)modelDataSizeInBytes, dataPtr);
        }

        // UV
        var uvSpan = CollectionsMarshal.AsSpan(uvRects).Slice(0, instanceCount);
        fixed (Vector4* uvDataPtr = &MemoryMarshal.GetReference(uvSpan))
        {
            m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_UvRectVboId);
            m_GlApi.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)uvDataSizeInBytes, uvDataPtr);
        }

        int location = m_GlApi.GetUniformLocation(m_ShaderProgramId, "textureSampler");
        m_GlApi.Uniform1(location, 0);
        
        m_GlApi.BindVertexArray(m_VaoId);
        m_GlApi.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, (uint)instanceCount);

        m_GlApi.BindVertexArray(0);
        m_GlApi.BindTexture(TextureTarget.Texture2D, 0);
        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
        
    }

    public unsafe void DrawStaticMesh(Assets.Texture texture, uint vboId, int vertexCount, in Matrix4x4 modelMatrix)
    {
        if (vertexCount == 0 || m_GlApi == null)
        {
            Logger.Log("DrawStaticMesh: Vertex count {vertexCount}  is 0 or GLAPI couldnt be found!", Logger.LogSeverity.Error);
            return;
        }

        m_GlApi.UseProgram(m_TileShaderProgramId);
        texture.Use(TextureUnit.Texture0);

        if (m_TileModelUniformLocation != -1)
        {
            fixed (float* ptr = &modelMatrix.M11)
            {
                m_GlApi.UniformMatrix4(m_TileModelUniformLocation, 1, false, ptr);
            }
        }

        int location = m_GlApi.GetUniformLocation(m_TileShaderProgramId, "textureSampler");
        if (location != -1)
        {
            m_GlApi.Uniform1(location, 0);
        }

        m_GlApi.BindVertexArray(m_TileVaoId);
        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, vboId);

        int stride = 4 * sizeof(float);

        // Position 
        m_GlApi.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
        m_GlApi.EnableVertexAttribArray(0);

        // UV 
        m_GlApi.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(2 * sizeof(float)));
        m_GlApi.EnableVertexAttribArray(1);

        m_GlApi.DrawArrays(PrimitiveType.Triangles, 0, (uint)vertexCount);

        m_GlApi.DisableVertexAttribArray(1);
        m_GlApi.DisableVertexAttribArray(0);
        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
        m_GlApi.BindVertexArray(0);
        m_GlApi.BindTexture(TextureTarget.Texture2D, 0);
        m_GlApi.UseProgram(0);
    }

    /// <summary>
    /// Creates or updates the VBO for a specific tile chunk
    /// </summary>
    public unsafe void UpdateTileChunkVbo(TileChunk chunk)
    {
        if (chunk.VboId == 0)
        {
            chunk.VboId = m_GlApi!.GenBuffer();
        }

        m_GlApi!.BindBuffer(GLEnum.ArrayBuffer, chunk.VboId);

        fixed (float* vPtr = CollectionsMarshal.AsSpan(chunk.Vertices))
        {
            // Static draw for tiles
            m_GlApi.BufferData(GLEnum.ArrayBuffer, (nuint)(chunk.Vertices.Count * sizeof(float)), vPtr, GLEnum.StaticDraw);
        }

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
    }

    public void RenderFrameStart()
    {
        Debug.Assert(m_GlApi != null, "SDL API is not initialized in RenderPipeline.");
        m_GlApi.Enable(EnableCap.Blend);
        m_GlApi.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        m_GlApi.DepthMask(true);
        m_GlApi.ClearColor(m_ClearColor);
        m_GlApi.Clear(m_ClearBufferMask);
    }

    public unsafe void RenderFrameEnd()
    {
        Debug.Assert(m_SdlApi != null, "SDL API is not initialized in RenderPipeline.");
        m_SdlApi.GLSwapWindow(m_WindowHandler);
    }

    /// <summary>
    /// Updates the OpenGL Viewport. (Projection is handled externally by the Camera Manager).
    /// </summary>
    public unsafe void UpdateViewportSize(int width, int height)
    {
        Debug.Assert(m_GlApi != null, "GL API is null in RenderPipeline UpdateSize.");
        m_GlApi.Viewport(0, 0, (uint)width, (uint)height);
    }

    /// <summary>
    /// Uploads the View-Projection Matrix from the active camera to the shader.
    /// </summary>
    /// <param name="matrix">The combined Matrix4x4 data (View * Projection).</param>
    public unsafe void SetProjectionMatrix(in Matrix4x4 matrix)
    {
        Debug.Assert(m_GlApi != null, "GL API is null.");

        // Dynamic shader
        m_GlApi.UseProgram(m_ShaderProgramId);
        fixed (float* ptr = &matrix.M11)
        {
            m_GlApi.UniformMatrix4(m_ProjectionUniformLocation, 1, false, ptr);
        }

        // Tile shader
        m_GlApi.UseProgram(m_TileShaderProgramId);
        fixed (float* ptr = &matrix.M11)
        {
            m_GlApi.UniformMatrix4(m_TileProjectionUniformLocation, 1, false, ptr);
        }

        m_GlApi.UseProgram(0);
    }

    public void SetClearColor(System.Drawing.Color color)
    {
        m_ClearColor = color;
    }

    private uint CreateShaderProgram(string vertexPath, string fragmentPath)
    {
        Debug.Assert(m_GlApi != null, "OpenGL API is null when creating shader program.");

        var vertexSource = File.ReadAllText(vertexPath);
        var fragmentSource = File.ReadAllText(fragmentPath);
        uint vertexShader = m_GlApi.CreateShader(ShaderType.VertexShader);
        m_GlApi.ShaderSource(vertexShader, vertexSource);
        m_GlApi.CompileShader(vertexShader);

        uint fragmentShader = m_GlApi.CreateShader(ShaderType.FragmentShader);
        m_GlApi.ShaderSource(fragmentShader, fragmentSource);
        m_GlApi.CompileShader(fragmentShader);

        uint shaderProgram = m_GlApi.CreateProgram();
        m_GlApi.AttachShader(shaderProgram, vertexShader);
        m_GlApi.AttachShader(shaderProgram, fragmentShader);
        m_GlApi.LinkProgram(shaderProgram);

        m_GlApi.DetachShader(shaderProgram, vertexShader);
        m_GlApi.DetachShader(shaderProgram, fragmentShader);
        m_GlApi.DeleteShader(vertexShader);
        m_GlApi.DeleteShader(fragmentShader);
        return shaderProgram;
    }

    private unsafe void SetupQuadMesh()
    {
        Debug.Assert(m_GlApi != null, "OpenGL API is null when setting up quad mesh.");

        m_VaoId = m_GlApi.GenVertexArray();
        m_VboId = m_GlApi.GenBuffer(); 
        m_InstanceVboId = m_GlApi.GenBuffer(); 
        m_UvRectVboId = m_GlApi.GenBuffer(); 
        m_ColorVboId = m_GlApi.GenBuffer(); 

        m_GlApi.BindVertexArray(m_VaoId);

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_VboId);
        int vertexSizeInBytes = s_QuadVertices.Length * sizeof(float);
        fixed (float* v = s_QuadVertices)
        {
            m_GlApi.BufferData(GLEnum.ArrayBuffer, (nuint)vertexSizeInBytes, v, GLEnum.StaticDraw);
        }

        int stride = 4 * sizeof(float);

        m_GlApi.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
        m_GlApi.EnableVertexAttribArray(0);

        m_GlApi.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(2 * sizeof(float)));
        m_GlApi.EnableVertexAttribArray(1);

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_InstanceVboId);

        const int initialInstances = 1024;
        m_InstanceBufferCapacityBytes = (nuint)(initialInstances * sizeof(Matrix4x4));
        m_GlApi.BufferData(GLEnum.ArrayBuffer, m_InstanceBufferCapacityBytes, null, GLEnum.DynamicDraw);

        int matrixSize = sizeof(Matrix4x4);

        for (uint i = 0; i < 4; i++)
        {
            uint attribLocation = 2u + i; 
            m_GlApi.EnableVertexAttribArray(attribLocation);
            m_GlApi.VertexAttribPointer(
                attribLocation,
                4,
                VertexAttribPointerType.Float,
                false,
                (uint)matrixSize,             
                (void*)(i * sizeof(Vector4)) 
            );
            m_GlApi.VertexAttribDivisor(attribLocation, 1); 
        }

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_UvRectVboId);

        m_UvBufferCapacityBytes = (nuint)(initialInstances * sizeof(Vector4));
        m_GlApi.BufferData(GLEnum.ArrayBuffer, m_UvBufferCapacityBytes, null, GLEnum.DynamicDraw);

        uint uvRectAttribLocation = 6u;
        int uvRectSize = sizeof(Vector4);

        m_GlApi.EnableVertexAttribArray(uvRectAttribLocation);
        m_GlApi.VertexAttribPointer(
            uvRectAttribLocation,
            4,
            VertexAttribPointerType.Float,
            false,
            (uint)uvRectSize,
            (void*)0
        );
        m_GlApi.VertexAttribDivisor(uvRectAttribLocation, 1);

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_ColorVboId); 

        m_ColorBufferCapacityBytes = (nuint)(initialInstances * sizeof(Vector4));
        m_GlApi.BufferData(GLEnum.ArrayBuffer, m_ColorBufferCapacityBytes, null, GLEnum.DynamicDraw);

        uint colorAttribLocation = 7u;
        int colorSize = sizeof(Vector4);

        m_GlApi.EnableVertexAttribArray(colorAttribLocation);
        m_GlApi.VertexAttribPointer(
            colorAttribLocation,
            4, 
            VertexAttribPointerType.Float,
            false,
            (uint)colorSize,
            (void*)0
        );
        m_GlApi.VertexAttribDivisor(colorAttribLocation, 1); 

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
        m_GlApi.BindVertexArray(0);
    }

    private unsafe void SetupTileMeshVao()
    {
        m_TileVaoId = m_GlApi!.GenVertexArray();
        m_GlApi.BindVertexArray(m_TileVaoId);
        m_GlApi.BindVertexArray(0);
    }

    private unsafe IntPtr GetProcAddress(string procName)
    {
        Debug.Assert(m_SdlApi != null, "SDL Api is null when getting proc address!");
        return (IntPtr)m_SdlApi.GLGetProcAddress(procName);
    }

    private unsafe void EnsureBufferCapacity(nuint requiredBytes, uint vboId, ref nuint currentCapacity)
    {
        if (requiredBytes <= currentCapacity)
            return;

        nuint newCapacity = Math.Max(requiredBytes, currentCapacity == 0 ? requiredBytes : currentCapacity * 2);

        m_GlApi!.BindBuffer(GLEnum.ArrayBuffer, vboId);
        m_GlApi!.BufferData(GLEnum.ArrayBuffer, newCapacity, null, GLEnum.DynamicDraw);
        m_GlApi!.BindBuffer(GLEnum.ArrayBuffer, 0);

        currentCapacity = newCapacity;
    }
}