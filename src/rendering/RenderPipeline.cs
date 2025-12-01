using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Silk.NET.SDL;

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
    private uint m_InstanceVboId;
    private nuint m_InstanceBufferCapacityBytes = 0;

    private unsafe Window* m_WindowHandler;
    private readonly ClearBufferMask m_ClearBufferMask = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;
    private System.Drawing.Color m_ClearColor = System.Drawing.Color.CornflowerBlue;

    private int m_ProjectionUniformLocation;

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

        SetupQuadMesh();
        m_ProjectionUniformLocation = m_GlApi.GetUniformLocation(m_ShaderProgramId, "projection");
    }

    /// <summary>
    /// Draws multiple sprites using a single draw call by utilizing instanced rendering.
    /// </summary>
    /// <param name="texture">The GLTexture containing the shared texture.</param>
    /// <param name="modelMatrices">A list of Model matrices for all instances.</param>
    /// <param name="instanceCount">The number of sprites to draw.</param>
    public unsafe void DrawSpritesInstanced(GLTexture texture, in List<Matrix4x4> modelMatrices, int instanceCount)
    {
        if (instanceCount == 0 || m_GlApi == null) return;

        m_GlApi.UseProgram(m_ShaderProgramId);
        texture.Use(TextureUnit.Texture0);

        int dataSizeInBytes = instanceCount * sizeof(Matrix4x4);
        EnsureInstanceBufferCapacity((nuint)dataSizeInBytes);

        var span = CollectionsMarshal.AsSpan(modelMatrices).Slice(0, instanceCount);
        fixed (Matrix4x4* dataPtr = &MemoryMarshal.GetReference(span))
        {
            m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_InstanceVboId);
            m_GlApi.BufferSubData(GLEnum.ArrayBuffer, 0, (nuint)dataSizeInBytes, dataPtr);
        }

        int location = m_GlApi.GetUniformLocation(m_ShaderProgramId, "textureSampler");
        m_GlApi.Uniform1(location, 0);

        m_GlApi.BindVertexArray(m_VaoId);
        m_GlApi.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, (uint)instanceCount);

        m_GlApi.BindVertexArray(0);
        m_GlApi.BindTexture(TextureTarget.Texture2D, 0);
        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
    }

    private unsafe void EnsureInstanceBufferCapacity(nuint requiredBytes)
    {
        if (requiredBytes <= m_InstanceBufferCapacityBytes)
            return;

        // Grow to at least requiredBytes, usually double the old size for amortized growth
        nuint newCapacity = Math.Max(requiredBytes, m_InstanceBufferCapacityBytes == 0 ? requiredBytes : m_InstanceBufferCapacityBytes * 2);
        m_GlApi!.BindBuffer(GLEnum.ArrayBuffer, m_InstanceVboId);
        m_GlApi!.BufferData(GLEnum.ArrayBuffer, newCapacity, null, GLEnum.DynamicDraw);
        m_GlApi!.BindBuffer(GLEnum.ArrayBuffer, 0);
        m_InstanceBufferCapacityBytes = newCapacity;
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

        m_GlApi.UseProgram(m_ShaderProgramId);

        fixed (float* ptr = &matrix.M11)
        {
            m_GlApi.UniformMatrix4(m_ProjectionUniformLocation, 1, false, ptr);
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

        m_GlApi.BindVertexArray(m_VaoId);

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_VboId);
        int vertexSizeInBytes = s_QuadVertices.Length * sizeof(float);
        fixed (float* v = s_QuadVertices)
        {
            m_GlApi.BufferData(GLEnum.ArrayBuffer, (nuint)vertexSizeInBytes, v, GLEnum.StaticDraw);
        }

        int stride = 4 * sizeof(float); // 2 pos + 2 uv
        m_GlApi.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
        m_GlApi.EnableVertexAttribArray(0);
        m_GlApi.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(2 * sizeof(float)));
        m_GlApi.EnableVertexAttribArray(1);

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_InstanceVboId);

        // Buffer is dynamic but we just set this as an initial starting state of the buffer
        const int initialInstances = 1024;
        m_InstanceBufferCapacityBytes = (nuint)(initialInstances * sizeof(Matrix4x4));
        m_GlApi.BufferData(GLEnum.ArrayBuffer, m_InstanceBufferCapacityBytes, null, GLEnum.DynamicDraw);

        int matrixSize = sizeof(Matrix4x4);

        // For System.Numerics.Matrix4x4 the memory layout is 4 Vector4s contiguous.
        for (uint i = 0; i < 4; i++)
        {
            uint attribLocation = 2u + i; // 2,3,4,5
            m_GlApi.EnableVertexAttribArray(attribLocation);
            m_GlApi.VertexAttribPointer(
                attribLocation,
                4,
                VertexAttribPointerType.Float,
                false,
                (uint)matrixSize,             // stride = size of Matrix4x4
                (void*)(i * sizeof(Vector4))  // offset = i-th column (or row depending layout)
            );
            // Per-instance (crucial for instancing)
            m_GlApi.VertexAttribDivisor(attribLocation, 1);
        }

        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
        m_GlApi.BindVertexArray(0);
    }

    private unsafe IntPtr GetProcAddress(string procName)
    {
        Debug.Assert(m_SdlApi != null, "SDL Api is null when getting proc address!");
        return (IntPtr)m_SdlApi.GLGetProcAddress(procName);
    }
}