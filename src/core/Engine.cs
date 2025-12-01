using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.SDL;

public class Engine
{
    private Sdl? m_SdlApi;
    private readonly Stopwatch m_Stopwatch;

    private readonly IGame m_Game;
    private readonly IEngineSettings m_Settings;
    private readonly IAssetManager m_AssetManager;
    private readonly IInputManager m_InputManager;
    private readonly IWindowPipeline m_WindowPipe;
    private readonly IRenderPipeline m_RenderPipe;
    private readonly IImGuiPipeline m_ImGuiPipe;
    private readonly ISceneManager m_SceneManager;
    private readonly IAudioManager m_AudioManager;
    private readonly ICameraManager m_CameraManager;
    private readonly IGameObjectFactory m_GameObjectFactory;

    // DEBUG
    const float cameraSpeed = 300.0f; 
    // END DEBUG
    private bool m_IsRunning;

    // DEBUG TESTING
    private SpriteObject? m_testObject;
    // END

    /// <summary>
    /// Initializes a new instance of the Engine class with the specified game and all the required services.
    /// </summary>
    /// <param name="game"></param>
    /// <exception cref="Exception"></exception>
    public Engine(IGame game)
    {
        m_Stopwatch = new Stopwatch();

        m_Settings = Service.Get<IEngineSettings>() ?? throw new Exception("Engine Settings service not found.");
        m_AssetManager = Service.Get<IAssetManager>() ?? throw new Exception("Asset Manager service not found.");
        m_InputManager = Service.Get<IInputManager>() ?? throw new Exception("Input Manager service not found.");
        m_WindowPipe = Service.Get<IWindowPipeline>() ?? throw new Exception("Window Pipeline service not found.");
        m_RenderPipe = Service.Get<IRenderPipeline>() ?? throw new Exception("Render Pipeline service not found.");
        m_ImGuiPipe = Service.Get<IImGuiPipeline>() ?? throw new Exception("ImGui Pipeline service not found.");
        m_SceneManager = Service.Get<ISceneManager>() ?? throw new Exception("Scene Manager service not found.");
        m_AudioManager = Service.Get<IAudioManager>() ?? throw new Exception("Audio Manager service not found.");
        m_CameraManager = Service.Get<ICameraManager>() ?? throw new Exception("Camera Manager service not found.");
        m_GameObjectFactory = Service.Get<IGameObjectFactory>() ?? throw new Exception("GameObjectFactory service not found.");

        m_Game = game;
    }

    /// <summary>
    /// Runs the main engine loop.
    /// </summary>
    public void Run()
    {
        Initialize();
        m_Stopwatch.Start();
        m_IsRunning = true;
        while(m_IsRunning)
        {
            long elapsedTicks = m_Stopwatch.ElapsedTicks;
            m_Stopwatch.Restart(); 
            float deltaTime = (float)elapsedTicks / Stopwatch.Frequency;
            HandleInput();
            Update(deltaTime);
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
        Scene defaultScene = new Scene("DefaultScene");
        m_testObject = m_GameObjectFactory.CreateSpriteObject("TestGameObject", "/home/ezroot/Repos/Integrity/DefaultEngineAssets/logo.png");
        m_testObject.Transform.ScaleX = 0.25f;
        m_testObject.Transform.ScaleY = 0.25f;

        if(m_testObject != null)
        {
            defaultScene.RegisterGameObject(m_testObject);
        }
        m_SceneManager.AddScene(defaultScene);
        m_SceneManager.LoadScene(defaultScene);
        // END DEBUG

        Camera2D mainCamera = new Camera2D("MainCamera", m_Settings.Data.WindowWidth, m_Settings.Data.WindowHeight);
        m_CameraManager.RegisterCamera(mainCamera);
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

        if (m_InputManager.IsKeyDown(Scancode.ScancodeW))
            m_CameraManager.MainCamera!.Position += new Vector2(0, -cameraSpeed * deltaTime);
        if (m_InputManager.IsKeyDown(Scancode.ScancodeS))
            m_CameraManager.MainCamera!.Position += new Vector2(0, cameraSpeed * deltaTime);
        if (m_InputManager.IsKeyDown(Scancode.ScancodeA))
            m_CameraManager.MainCamera!.Position += new Vector2(-cameraSpeed * deltaTime, 0);
        if (m_InputManager.IsKeyDown(Scancode.ScancodeD))
            m_CameraManager.MainCamera!.Position += new Vector2(cameraSpeed * deltaTime, 0);   

        m_Game.Update(deltaTime);
    }

    private void Render()
    {
        m_RenderPipe.RenderFrameStart();
        
        Matrix4x4 cameraMatrix = m_CameraManager.MainCamera!.GetViewProjectionMatrix(); 
        m_RenderPipe.SetProjectionMatrix(in cameraMatrix);

        // DEBUG TESTING
        if(m_SceneManager.CurrentScene != null)
        {
            var sceneGameObjects = m_SceneManager.CurrentScene.GetAllSpriteObjects();
            for(var i = 0; i < sceneGameObjects.Count; i++)
            {
                var sceneObj = sceneGameObjects[i];
                m_RenderPipe.DrawSprite(sceneObj.Sprite, sceneObj.Transform);
            }
        }
        // END DEBUG

        m_Game.Render();

        m_ImGuiPipe.BeginFrame();
        m_ImGuiPipe.Tools.DrawMenuBar();
        m_ImGuiPipe.Tools.DrawTools();
        m_ImGuiPipe.EndFrame();

        m_RenderPipe.RenderFrameEnd();
    }

    private void Cleanup()
    {
        m_Game.Cleanup();
    }
}