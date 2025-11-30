using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.SDL;

public class Engine
{
    private Sdl? m_SdlApi;
    private readonly IGame m_Game;
    private readonly IEngineSettings m_Settings;
    private readonly IAssetManager m_AssetManager;
    private readonly IInputManager m_InputManager;
    private readonly IWindowPipeline m_WindowPipe;
    private readonly IRenderPipeline m_RenderPipe;
    private readonly IImGuiPipeline m_ImGuiPipe;
    private readonly ISceneManager m_SceneManager;
    private readonly IAudioManager m_AudioManager;
    private Camera2D? m_MainCamera;
    // DEBUG
    const float cameraSpeed = 300.0f; 
    // END DEBUG
    private bool m_IsRunning;

    // DEBUG TESTING
    private GameObject? m_testObject;
    // END

    /// <summary>
    /// Initializes a new instance of the Engine class with the specified game and all the required services.
    /// </summary>
    /// <param name="game"></param>
    /// <exception cref="Exception"></exception>
    public Engine(IGame game)
    {
        m_Settings = Service.Get<IEngineSettings>() ?? throw new Exception("Engine Settings service not found.");
        m_AssetManager = Service.Get<IAssetManager>() ?? throw new Exception("Asset Manager service not found.");
        m_InputManager = Service.Get<IInputManager>() ?? throw new Exception("Input Manager service not found.");
        m_WindowPipe = Service.Get<IWindowPipeline>() ?? throw new Exception("Window Pipeline service not found.");
        m_RenderPipe = Service.Get<IRenderPipeline>() ?? throw new Exception("Render Pipeline service not found.");
        m_ImGuiPipe = Service.Get<IImGuiPipeline>() ?? throw new Exception("ImGui Pipeline service not found.");
        m_SceneManager = Service.Get<ISceneManager>() ?? throw new Exception("Scene Manager service not found.");
        m_AudioManager = Service.Get<IAudioManager>() ?? throw new Exception("Audio Manager service not found.");

        m_Game = game;
    }

    /// <summary>
    /// Runs the main engine loop.
    /// </summary>
    public void Run()
    {
        Initialize();
        
        m_IsRunning = true;
        while(m_IsRunning)
        {
            HandleInput();
            Update();
            Render();
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
        Logger.Log($"Engine '{m_Settings.Data.EngineName}' version {m_Settings.Data.EngineVersion} initialized.",
        Logger.LogSeverity.Info);
    }

    private unsafe void Initialize()
    {
        m_SdlApi = Sdl.GetApi();
        if (m_SdlApi.Init(Sdl.InitVideo) < 0)
            throw new Exception("Failed to initialize SDL Video subsystem.");
            
        Logger.Log("SDL Video subsystem initialized.", Logger.LogSeverity.Info);

        m_WindowPipe.InitializeWindow(m_SdlApi, m_Settings.Data.WindowTitle, 
            m_Settings.Data.WindowWidth, 
            m_Settings.Data.WindowHeight
        );
        m_RenderPipe.InitializeRenderer(m_SdlApi, m_WindowPipe.WindowHandler);
        m_ImGuiPipe.Initialize(m_RenderPipe.GlApi!, m_SdlApi, m_WindowPipe.WindowHandler, m_WindowPipe.GlContext);

        m_Game.Initialize();

        // DEBUG TESTING
        m_testObject = Service.Get<IGameObjectFactory>()?.CreateGameObject("/home/ezroot/Repos/Integrity/DefaultEngineAssets/logo.png");
        // END DEBUG

        m_MainCamera = new Camera2D(m_Settings.Data.WindowWidth, m_Settings.Data.WindowHeight);
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
                m_MainCamera!.UpdateViewportSize(newW, newH);
                m_RenderPipe.UpdateViewportSize(newW, newH); 
            }
        }
    }


    private void Update()
    {
        float deltaTime = 1.0f / 60.0f; 
        if (m_InputManager.IsKeyDown(Scancode.ScancodeW))
            m_MainCamera!.Position += new Vector2(0, -cameraSpeed * deltaTime);
        if (m_InputManager.IsKeyDown(Scancode.ScancodeS))
            m_MainCamera!.Position += new Vector2(0, cameraSpeed * deltaTime);
        if (m_InputManager.IsKeyDown(Scancode.ScancodeA))
            m_MainCamera!.Position += new Vector2(-cameraSpeed * deltaTime, 0);
        if (m_InputManager.IsKeyDown(Scancode.ScancodeD))
            m_MainCamera!.Position += new Vector2(cameraSpeed * deltaTime, 0);   

        m_Game.Update();
    }

    private void Render()
    {
        m_RenderPipe.RenderFrameStart();
        
        Matrix4x4 cameraMatrix = m_MainCamera!.GetViewProjectionMatrix(); 
        m_RenderPipe.SetProjectionMatrix(in cameraMatrix);

        // DEBUG TESTING
        Debug.Assert(m_testObject != null, "Test texture is null in Engine Render.");
        //TODO: Replace with proper sprite rendering system
        // Such as m_SceneManager.GetActiveScene().GetAllObjects() or something similar
        m_RenderPipe.DrawSprite(m_testObject.Sprite, m_testObject.Transform);
        // END DEBUG

        m_Game.Render();

        m_ImGuiPipe.BeginFrame();
        ImGui.ShowDemoWindow();
        m_ImGuiPipe.EndFrame();

        m_RenderPipe.RenderFrameEnd();
    }

    private void Cleanup()
    {
        m_Game.Cleanup();
    }
}