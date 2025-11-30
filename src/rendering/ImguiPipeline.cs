using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System.Numerics;
using System.Diagnostics;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;
using PixelType = Silk.NET.OpenGL.PixelType;

public class ImGuiPipeline : IImGuiPipeline
{
    private IntPtr m_Context;
    private GL? m_GlApi;
    private unsafe void* m_GlContext;
    private Sdl? m_SdlApi;
    private unsafe Window* m_WindowHandler;
    
    private double m_Time = 0.0;
    private bool m_IsInitialized = false;
    private uint m_ProgramId;
    private uint m_VaoId;
    private uint m_VboId;
    private uint m_EboId;
    private int m_ProjUniformLocation;
    private int m_AttribLocationPos;
    private int m_AttribLocationUV;
    private int m_AttribLocationColor;


    public unsafe void Initialize(GL glApi, Sdl sdlApi, Window* windowHandler, void* glContext)
    {
        m_GlApi = glApi;
        m_SdlApi = sdlApi;
        m_WindowHandler = windowHandler;
        m_GlContext = glContext;

        m_Context = ImGui.CreateContext();
        ImGui.SetCurrentContext(m_Context);
        
        ImGuiIOPtr io = ImGui.GetIO();
        
        int w, h;
        m_SdlApi.GetWindowSize(windowHandler, &w, &h);
        io.DisplaySize = new Vector2(w, h);
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        
        uint fontTexture = m_GlApi.GenTexture(); 
        m_GlApi.BindTexture(TextureTarget.Texture2D, fontTexture);
        m_GlApi.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        m_GlApi.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        m_GlApi.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        m_GlApi.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        m_GlApi.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        m_GlApi.TexImage2D(GLEnum.Texture2D, 0, (int)PixelFormat.Rgba,
            (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)pixels);
        
        io.Fonts.SetTexID((IntPtr)fontTexture);
        io.Fonts.ClearTexData();
        
        ImGui.StyleColorsDark();

        SetupRenderResources();
        
        m_IsInitialized = true;
    }

    public void BeginFrame()
    {
        if (!m_IsInitialized) return;

        ImGuiIOPtr io = ImGui.GetIO();

        double currentTime = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        io.DeltaTime = m_Time > 0.0 ? (float)(currentTime - m_Time) : 1.0f / 60.0f;
        m_Time = currentTime;

        int w, h;
        unsafe {
            m_SdlApi!.GetWindowSize(m_WindowHandler, &w, &h);
            io.DisplaySize = new Vector2(w, h);
        }
        
        ImGui.NewFrame();
    }

    public unsafe void EndFrame()
    {
        if (!m_IsInitialized) return;

        ImGui.Render();
        ImDrawDataPtr drawData = ImGui.GetDrawData();
        
        RenderDrawDataOpenGL(drawData);

        ImGui.EndFrame();
        
        if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
            Debug.Assert(m_SdlApi != null, "SDL API is null in ImGuiPipeline EndFrame.");
            m_SdlApi.GLMakeCurrent(m_WindowHandler, m_GlContext);
        }
    }
    
    /// <summary>
    /// Maps SDL events to ImGui's internal Input/Output system.
    /// </summary>
    public unsafe void ProcessEvents(Event ev)
    {
        Debug.Assert(m_GlApi != null, "GL API is null in ImGuiPipeline ProcessEvents.");

        ImGuiIOPtr io = ImGui.GetIO();

        switch ((EventType)ev.Type)
        {
            case EventType.Keydown:
            case EventType.Keyup:
                {
                    bool down = (EventType)ev.Type == EventType.Keydown;
                    ImGuiKey key = MapSDLKey(ev.Key.Keysym.Scancode); 
                    
                    if (key != ImGuiKey.None)
                    {
                        io.KeysData[(int)key].Down = (byte)(down ? 1 : 0);
                    }
                    
                    io.KeyCtrl = (ev.Key.Keysym.Mod & (ushort)Keymod.Ctrl) != 0;
                    io.KeyShift = (ev.Key.Keysym.Mod & (ushort)Keymod.Shift) != 0;
                    io.KeyAlt = (ev.Key.Keysym.Mod & (ushort)Keymod.Alt) != 0;
                    io.KeySuper = (ev.Key.Keysym.Mod & (ushort)Keymod.Gui) != 0;
                }
                break;

            case EventType.Textinput:
                string text = System.Text.Encoding.UTF8.GetString(ev.Text.Text, 32); // SDL_TEXTINPUTEVENT_TEXT_SIZE is 32
                io.AddInputCharactersUTF8(text);
                break;
            
            case EventType.Mousebuttondown:
            case EventType.Mousebuttonup:
                {
                    int button = (int)ev.Button.Button;
                    bool down = (EventType)ev.Type == EventType.Mousebuttondown;
                    
                    if (button >= 1 && button <= 5)
                    {
                        io.MouseDown[button - 1] = down;
                    }
                }
                break;

            case EventType.Mousemotion:
                io.MousePos = new Vector2(ev.Motion.X, ev.Motion.Y);
                break;

            case EventType.Mousewheel:
                {
                    float wheelX = ev.Wheel.X;
                    float wheelY = ev.Wheel.Y;
                    
                    if ((ev.Wheel.Direction & (uint)MouseWheelDirection.Flipped) != 0)
                    {
                        wheelX *= -1.0f;
                        wheelY *= -1.0f;
                    }
                    
                    io.MouseWheelH += wheelX;
                    io.MouseWheel += wheelY;
                }
                break;
                
            case EventType.Windowevent:
                if (ev.Window.Event == (byte)WindowEventID.SizeChanged)
                {
                    int w, h;
                    m_SdlApi!.GetWindowSize(m_WindowHandler, &w, &h);
                    io.DisplaySize = new Vector2(w, h);
                }
                break;
        }
    }
    
    private ImGuiKey MapSDLKey(Scancode scancode)
    {
        return scancode switch
        {
            Scancode.ScancodeTab => ImGuiKey.Tab,
            Scancode.ScancodeLeft => ImGuiKey.LeftArrow,
            Scancode.ScancodeRight => ImGuiKey.RightArrow,
            Scancode.ScancodeUp => ImGuiKey.UpArrow,
            Scancode.ScancodeDown => ImGuiKey.DownArrow,
            Scancode.ScancodePageup => ImGuiKey.PageUp,
            Scancode.ScancodePagedown => ImGuiKey.PageDown,
            Scancode.ScancodeHome => ImGuiKey.Home,
            Scancode.ScancodeEnd => ImGuiKey.End,
            Scancode.ScancodeDelete => ImGuiKey.Delete,
            Scancode.ScancodeBackspace => ImGuiKey.Backspace,
            Scancode.ScancodeReturn => ImGuiKey.Enter,
            Scancode.ScancodeEscape => ImGuiKey.Escape,
            Scancode.ScancodeA => ImGuiKey.A,
            Scancode.ScancodeC => ImGuiKey.C,
            Scancode.ScancodeV => ImGuiKey.V,
            Scancode.ScancodeX => ImGuiKey.X,
            Scancode.ScancodeY => ImGuiKey.Y,
            Scancode.ScancodeZ => ImGuiKey.Z,
            Scancode.ScancodeLctrl => ImGuiKey.LeftCtrl,
            Scancode.ScancodeRctrl => ImGuiKey.RightCtrl,
            Scancode.ScancodeLshift => ImGuiKey.LeftShift,
            Scancode.ScancodeRshift => ImGuiKey.RightShift,
            _ => ImGuiKey.None,
        };
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

    private unsafe void SetupRenderResources()
    {
        Debug.Assert(m_GlApi != null, "OpenGL API is null when setting up ImGui resources.");

        m_ProgramId = CreateShaderProgram(
            Path.Combine(EngineSettings.SHADER_DIR, "imgui.vert"), 
            Path.Combine(EngineSettings.SHADER_DIR, "imgui.frag")
        );
        
        m_AttribLocationPos = m_GlApi.GetAttribLocation(m_ProgramId, "in_position");
        m_AttribLocationUV = m_GlApi.GetAttribLocation(m_ProgramId, "in_uv");
        m_AttribLocationColor = m_GlApi.GetAttribLocation(m_ProgramId, "in_color");
        m_ProjUniformLocation = m_GlApi.GetUniformLocation(m_ProgramId, "projection");
        
        m_VaoId = m_GlApi.GenVertexArray();
        m_VboId = m_GlApi.GenBuffer();
        m_EboId = m_GlApi.GenBuffer();
        
        m_GlApi.BindVertexArray(m_VaoId);
        
        m_GlApi.BindBuffer(BufferTargetARB.ArrayBuffer, m_VboId);
        m_GlApi.BufferData(BufferTargetARB.ArrayBuffer, (nuint)1, null, BufferUsageARB.DynamicDraw);
        
        m_GlApi.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_EboId);
        m_GlApi.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)1, null, BufferUsageARB.DynamicDraw);

        int stride = sizeof(ImDrawVert);
        
        m_GlApi.EnableVertexAttribArray((uint)m_AttribLocationPos);
        m_GlApi.VertexAttribPointer((uint)m_AttribLocationPos, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
        
        m_GlApi.EnableVertexAttribArray((uint)m_AttribLocationUV);
        m_GlApi.VertexAttribPointer((uint)m_AttribLocationUV, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(2 * sizeof(float)));
        
        m_GlApi.EnableVertexAttribArray((uint)m_AttribLocationColor);
        m_GlApi.VertexAttribPointer((uint)m_AttribLocationColor, 4, VertexAttribPointerType.UnsignedByte, true, (uint)stride, (void*)(4 * sizeof(float)));

        m_GlApi.BindVertexArray(0);
        m_GlApi.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        m_GlApi.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }
    
    /// <summary>
    /// Executes all OpenGL draw calls based on the generated ImGui draw data.
    /// </summary>
    public unsafe void RenderDrawDataOpenGL(ImDrawDataPtr drawData)
    {
        Debug.Assert(m_GlApi != null, "GL API is null in RenderDrawDataOpenGL.");
        if (drawData.CmdListsCount == 0 || m_GlApi == null) return;
        
        if (drawData.DisplaySize.X <= 0.0f || drawData.DisplaySize.Y <= 0.0f) return;

        m_GlApi.Enable(EnableCap.Blend);
        m_GlApi.BlendEquation(GLEnum.FuncAdd);
        m_GlApi.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        m_GlApi.Disable(EnableCap.CullFace);
        m_GlApi.Disable(EnableCap.DepthTest);
        m_GlApi.Enable(EnableCap.ScissorTest);
        m_GlApi.Disable(EnableCap.ProgramPointSize);
        m_GlApi.Enable(EnableCap.Texture2D);
        m_GlApi.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Fill);

        m_GlApi.Viewport(0, 0, (uint)drawData.DisplaySize.X, (uint)drawData.DisplaySize.Y);
        
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
        
        Matrix4x4 orthoProjection = new Matrix4x4(
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
        );
        
        m_GlApi.UseProgram(m_ProgramId);
        m_GlApi.Uniform1(m_GlApi.GetUniformLocation(m_ProgramId, "textureSampler"), 0);
        
        float[] projectionArray = new float[]
        {
            orthoProjection.M11, orthoProjection.M12, orthoProjection.M13, orthoProjection.M14,
            orthoProjection.M21, orthoProjection.M22, orthoProjection.M23, orthoProjection.M24,
            orthoProjection.M31, orthoProjection.M32, orthoProjection.M33, orthoProjection.M34,
            orthoProjection.M41, orthoProjection.M42, orthoProjection.M43, orthoProjection.M44
        };
        fixed (float* ptr = projectionArray)
        {
            m_GlApi.UniformMatrix4(m_ProjUniformLocation, 1, false, ptr);
        }
        
        m_GlApi.BindVertexArray(m_VaoId);
        
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            m_GlApi.BindBuffer(BufferTargetARB.ArrayBuffer, m_VboId);
            m_GlApi.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert)), (void*)cmdList.VtxBuffer.Data, BufferUsageARB.StreamDraw);

            m_GlApi.BindBuffer(BufferTargetARB.ElementArrayBuffer, m_EboId);
            m_GlApi.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(cmdList.IdxBuffer.Size * sizeof(ushort)), (void*)cmdList.IdxBuffer.Data, BufferUsageARB.StreamDraw);
            
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[i];
                
                if (cmd.UserCallback != IntPtr.Zero)
                {
                    continue;
                }
                
                float clipX = cmd.ClipRect.X - drawData.DisplayPos.X;
                float clipY = cmd.ClipRect.Y - drawData.DisplayPos.Y;
                float clipW = cmd.ClipRect.Z - clipX;
                float clipH = cmd.ClipRect.W - clipY;
                
                if (clipW <= 0.0f || clipH <= 0.0f) continue;
                
                m_GlApi.Scissor((int)clipX, (int)(drawData.DisplaySize.Y - clipY - clipH), (uint)clipW, (uint)clipH);
                m_GlApi.BindTexture(TextureTarget.Texture2D, (uint)cmd.TextureId.ToInt32());
                m_GlApi.DrawElements(PrimitiveType.Triangles, cmd.ElemCount, DrawElementsType.UnsignedShort, (void*)(cmd.IdxOffset * sizeof(ushort)));
            }
        }
        
        // Engine state restore
        m_GlApi.UseProgram(0);
        m_GlApi.BindVertexArray(0);
        m_GlApi.BindTexture(TextureTarget.Texture2D, 0);
        m_GlApi.Disable(EnableCap.Blend);
        m_GlApi.Disable(EnableCap.ScissorTest);
    }
}