using System.Diagnostics;
using System.Numerics;
using Integrity.Interface;
using Integrity.Utils;
using Silk.NET.SDL;

namespace Integrity.Core;
public class Engine
{
    private const float FPS_UPDATE_INTERVAL = 1.0f;

    private Sdl? m_SdlApi;
    private readonly Stopwatch m_Stopwatch;

    private readonly IGame m_Game;
    private readonly IEngineSettings m_Settings;
    private readonly IInputManager m_InputManager;
    private readonly IWindowPipeline m_WindowPipe;
    private readonly IRenderPipeline m_RenderPipe;
    private readonly IImGuiPipeline m_ImGuiPipe;
    private readonly ISceneManager m_SceneManager;
    private readonly ICameraManager m_CameraManager;
    private readonly IProfiler m_Profiler;

    private readonly Dictionary<Assets.Texture, List<Matrix4x4>> m_RenderingBatchMap;
    private readonly Dictionary<Assets.Texture, List<Vector4>> m_UvBatchMap = new();

    private int m_FrameCount;
    private float m_FpsTimeAccumulator;
    private float m_CurrentFps;
    private bool m_IsRunning;

    /// <summary>
    /// Initializes a new instance of the Engine class with the specified game and all the required services.
    /// </summary>
    /// <param name="game"></param>
    /// <exception cref="Exception"></exception>
    public Engine(IGame game)
    {
        m_RenderingBatchMap = new();
        m_Stopwatch = new Stopwatch();

        m_Settings = Service.Get<IEngineSettings>() ?? throw new Exception("Engine Settings service not found.");
        m_InputManager = Service.Get<IInputManager>() ?? throw new Exception("Input Manager service not found.");
        m_WindowPipe = Service.Get<IWindowPipeline>() ?? throw new Exception("Window Pipeline service not found.");
        m_RenderPipe = Service.Get<IRenderPipeline>() ?? throw new Exception("Render Pipeline service not found.");
        m_ImGuiPipe = Service.Get<IImGuiPipeline>() ?? throw new Exception("ImGui Pipeline service not found.");
        m_SceneManager = Service.Get<ISceneManager>() ?? throw new Exception("Scene Manager service not found.");
        m_CameraManager = Service.Get<ICameraManager>() ?? throw new Exception("Camera Manager service not found.");
        m_Profiler = Service.Get<IProfiler>() ?? throw new Exception("Profiler service not found.");

        m_Game = game;
    }

    /// <summary>
    /// Runs the main engine loop.
    /// </summary>
    public void Run()
    {
        Initialize();
        m_Profiler.InitializeRenderProfiler("Full_Render");
        m_Profiler.InitializeRenderProfiler("Draw_Sprite_Instanced");
        m_Profiler.InitializeRenderProfiler("Draw_ImGui");
        m_Stopwatch.Start();
        m_IsRunning = true;
        while (m_IsRunning)
        {
            m_Profiler.StartCpuProfile("Full_Update");
            m_Stopwatch.Stop();
            float deltaTime = (float)m_Stopwatch.Elapsed.TotalSeconds;
            m_Stopwatch.Restart();
            m_FrameCount++;
            m_FpsTimeAccumulator += deltaTime;
            if (m_FpsTimeAccumulator >= FPS_UPDATE_INTERVAL)
            {
                m_CurrentFps = m_FrameCount / m_FpsTimeAccumulator;
                m_FrameCount = 0;
                m_FpsTimeAccumulator = 0.0f;
            }

            m_Profiler.StartCpuProfile("Cpu_Input");
            HandleInput();
            m_Profiler.StopCpuProfile("Cpu_Input");
            m_Profiler.StartCpuProfile("Cpu_Update");
            Update(deltaTime);
            m_Profiler.StopCpuProfile("Cpu_Update");
            m_Profiler.StopCpuProfile("Full_Update");
            m_Profiler.StartRenderProfile("Full_Render");
            Render();
            m_Profiler.StopRenderProfile("Full_Render");
        }

        Cleanup();
    }

    /// <summary>
    /// Initializes the asynchronous components of the engine.
    /// </summary>
    /// <returns></returns>
    public async Task InitializeAsync()
    {
        await m_Settings.LoadSettingsAsync();
        Logger.Log($"Engine '{m_Settings.Data.EngineName}' version {m_Settings.Data.EngineVersion} initialized.", Logger.LogSeverity.Info);
        Logger.Log($"Graphics Vsync='{m_Settings.Data.UseVsync}'", Logger.LogSeverity.Info);
    }

    private unsafe void Initialize()
    {
        m_SdlApi = Sdl.GetApi();
        if (m_SdlApi.Init(Sdl.InitVideo) < 0)
            throw new Exception("Failed to initialize SDL Video subsystem.");

        Logger.Log("SDL Video subsystem initialized.", Logger.LogSeverity.Info);

        m_WindowPipe.InitializeWindow(m_SdlApi, m_Settings.Data.WindowTitle,
            m_Settings.Data.WindowWidth,
            m_Settings.Data.WindowHeight,
            m_Settings.Data.UseVsync
        );

        m_RenderPipe.InitializeRenderer(m_SdlApi, m_WindowPipe.WindowHandler);
        m_ImGuiPipe.Initialize(m_RenderPipe.GlApi!, m_SdlApi, m_WindowPipe.WindowHandler, m_WindowPipe.GlContext);

        m_Game.Initialize();

        if(m_CameraManager.MainCamera == null)
        {
            Logger.Log("No camera present. Aborting!", Logger.LogSeverity.Error);
            Environment.Exit(0);
        }
    }

    private unsafe void HandleInput()
    {
        Event ev;
        while (m_SdlApi!.PollEvent(&ev) != 0)
        {
            m_ImGuiPipe.ProcessEvents(ev);
            m_InputManager.ProcessInput(ev);

            if ((EventType)ev.Type == EventType.Windowevent && ev.Window.Event == (byte)WindowEventID.SizeChanged)
            {
                int newW, newH;
                m_SdlApi.GetWindowSize(m_WindowPipe.WindowHandler, &newW, &newH);
                m_CameraManager.MainCamera!.UpdateViewportSize(newW, newH);
                m_RenderPipe.UpdateViewportSize(newW, newH);
            }
        }
    }

    private void Update(float deltaTime)
    {
        m_ImGuiPipe.Tools.DrawToolsUpdate(deltaTime);
        m_Game.Update(deltaTime);

        if(m_SceneManager.CurrentScene != null)
        {
            m_SceneManager.CurrentScene.AnimationRenderSystem.Update(deltaTime);
        }
    }

    private void Render()
    {
        m_RenderPipe.RenderFrameStart();
        Matrix4x4 cameraMatrix = m_CameraManager.MainCamera!.GetViewProjectionMatrix();
        m_RenderPipe.SetProjectionMatrix(in cameraMatrix);

        if (m_SceneManager.CurrentScene != null)
        {
            // Batch object positions by texture
            m_Profiler.StartCpuProfile("Sprite_Sorting");
            var sceneGameObjects = m_SceneManager.CurrentScene.GetAllSpriteObjects();
            m_RenderingBatchMap.Clear();
            m_UvBatchMap.Clear();

            foreach (var obj in sceneGameObjects)
            {
                if (obj.Sprite == null) continue;

                var sprite = obj.Sprite;
                var texture = sprite.Texture;

                // Sort positions by texture
                if (!m_RenderingBatchMap.TryGetValue(texture, out var instancedSpritePositionList))
                {
                    instancedSpritePositionList = new List<Matrix4x4>();
                    m_RenderingBatchMap[texture] = instancedSpritePositionList;
                }

                var model = MathHelper.Translation(
                    obj.Transform.X, obj.Transform.Y,
                    obj.Sprite.SourceRect.Width * obj.Transform.ScaleX,
                    obj.Sprite.SourceRect.Height * obj.Transform.ScaleY
                );

                instancedSpritePositionList.Add(model);

                // Sort UVs on atlas by texture
                if (!m_UvBatchMap.TryGetValue(texture, out var instancedUvRectList))
                {
                    instancedUvRectList = new List<Vector4>();
                    m_UvBatchMap[texture] = instancedUvRectList;
                }

                float texW = texture.Width;
                float texH = texture.Height;
                
                float rectX = sprite.SourceRect.X / texW;
                float rectY = sprite.SourceRect.Y / texH;
                float rectW = sprite.SourceRect.Width / texW;
                float rectH = sprite.SourceRect.Height / texH;
                
                instancedUvRectList.Add(new Vector4(rectX, rectY, rectW, rectH));
            }


            m_Profiler.StopCpuProfile("Sprite_Sorting");

            m_Profiler.StartRenderProfile("Draw_Sprite_Instanced");
            foreach (var kvp in m_RenderingBatchMap)
            {
                var texture = kvp.Key;
                var matrices = kvp.Value;
                if(m_UvBatchMap.TryGetValue(texture, out var uvrects))
                {
                    m_RenderPipe.DrawSpritesInstanced(texture, matrices, uvrects, matrices.Count);
                }
            }
            m_Profiler.StopRenderProfile("Draw_Sprite_Instanced");
        }

        m_Game.Render();

        m_Profiler.StartRenderProfile("Draw_ImGui");
        m_ImGuiPipe.BeginFrame();
        m_ImGuiPipe.Tools.DrawMenuBar(m_CurrentFps);
        m_ImGuiPipe.Tools.DrawTools(m_Profiler);
        m_ImGuiPipe.EndFrame();
        m_Profiler.StopRenderProfile("Draw_ImGui");

        m_RenderPipe.RenderFrameEnd();
    }

    private void Cleanup()
    {
        m_Game.Cleanup();
    }
}