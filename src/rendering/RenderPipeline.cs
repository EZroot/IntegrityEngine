using System.Diagnostics;
using System.Numerics;
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

    private unsafe Window* m_WindowHandler;
    private readonly System.Drawing.Color ClearColor = System.Drawing.Color.CornflowerBlue;
    private readonly ClearBufferMask m_ClearBufferMask = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;

    private int m_ProjectionUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;
    
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
        if(m_GlApi == null)
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

        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0, 
            settings.Data.WindowWidth,
            settings.Data.WindowHeight, 
            0,                          // Top (makes Y=0 the top of the window)
            -1.0f, 
            1.0f
        );

        m_ProjectionUniformLocation = m_GlApi.GetUniformLocation(m_ShaderProgramId, "projection");

        m_GlApi.UseProgram(m_ShaderProgramId);
        fixed(float* ptr = &m_ProjectionMatrix.M11)
        {
            m_GlApi.UniformMatrix4(m_ProjectionUniformLocation, 1, false, ptr);         
        }
        m_GlApi.UseProgram(0);
    }

    /// <summary>
    /// Draws a sprite using its associated SpriteComponent for texture and TransformComponent for position, scale, and rotation.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="transform"></param>
    public void DrawSprite(SpriteComponent sprite, TransformComponent transform)
    {
        Debug.Assert(m_GlApi != null, "GL API is null.");

        m_GlApi.UseProgram(m_ShaderProgramId);
        sprite.Texture.Use(TextureUnit.Texture0);

        var model = MathHelper.Translation(transform.X, transform.Y, sprite.Texture.Width * transform.ScaleX, sprite.Texture.Height * transform.ScaleY);
        int modelLoc = m_GlApi.GetUniformLocation(m_ShaderProgramId, "model");
        unsafe
        {
            m_GlApi.UniformMatrix4(modelLoc, 1, false, (float*)&model); 
        }

        int location = m_GlApi.GetUniformLocation(m_ShaderProgramId, "textureSampler");
        m_GlApi.Uniform1(location, 0); 

        m_GlApi.BindVertexArray(m_VaoId);
        m_GlApi.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // Clean up
        m_GlApi.BindVertexArray(0);
        m_GlApi.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void RenderFrameStart()
    {
        Debug.Assert(m_GlApi != null, "SDL API is not initialized in RenderPipeline.");
        m_GlApi.ClearColor(ClearColor);
        m_GlApi.Clear(m_ClearBufferMask);
    }

    public unsafe void RenderFrameEnd()
    {
        Debug.Assert(m_SdlApi != null, "SDL API is not initialized in RenderPipeline.");
        m_SdlApi.GLSwapWindow(m_WindowHandler);
    }

    /// <summary>
    /// Updates the OpenGL Viewport and Projection Matrix when the window size changes.
    /// </summary>
    public unsafe void UpdateViewportSize(int width, int height)
    {
        Debug.Assert(m_GlApi != null, "GL API is null in RenderPipeline UpdateSize.");

        // 1. Update Viewport
        m_GlApi.Viewport(0, 0, (uint)width, (uint)height);

        // 2. Update Projection Matrix (Orthographic Off Center)
        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0, 
            width,
            height, 
            0, // Top (makes Y=0 the top of the window)
            -1.0f, 
            1.0f
        );

        // 3. Upload new Projection Matrix to the Shader
        m_GlApi.UseProgram(m_ShaderProgramId);
        fixed(float* ptr = &m_ProjectionMatrix.M11)
        {
            m_GlApi.UniformMatrix4(m_ProjectionUniformLocation, 1, false, ptr);         
        }
        m_GlApi.UseProgram(0);
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

        m_GlApi.BindVertexArray(m_VaoId);
        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, m_VboId);
        
        int vertexSizeInBytes = s_QuadVertices.Length * sizeof(float);
        fixed (float* v = s_QuadVertices)
        {
            m_GlApi.BufferData(GLEnum.ArrayBuffer, (nuint)vertexSizeInBytes, v, GLEnum.StaticDraw);
        }

        int stride = 4 * sizeof(float);
        m_GlApi.VertexAttribPointer(
            0, // location in shader
            2, // size (2 components: X, Y)
            VertexAttribPointerType.Float,
            false, // normalized
            (uint)stride,
            (void*)0 // offset from start of vertex (0 bytes)
        );
        m_GlApi.EnableVertexAttribArray(0);

        // Texture coordinate attribute (Layout location 1 in vertex shader)
        m_GlApi.VertexAttribPointer(
            1, // location in shader
            2, // size (2 components: U, V)
            VertexAttribPointerType.Float,
            false, // normalized
            (uint)stride,
            (void*)(2 * sizeof(float)) // offset from start of vertex (2 floats * 4 bytes/float)
        );

        m_GlApi.EnableVertexAttribArray(1);
        m_GlApi.BindBuffer(GLEnum.ArrayBuffer, 0);
        m_GlApi.BindVertexArray(0);
    }

    private unsafe IntPtr GetProcAddress(string procName) 
    { 
        Debug.Assert(m_SdlApi != null, "SDL Api is null when getting proc address!");
        return (IntPtr)m_SdlApi.GLGetProcAddress(procName); 
    }
}