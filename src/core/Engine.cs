using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private readonly ISceneManager m_SceneManager;
    private readonly IAudioManager m_AudioManager;

    private bool m_IsRunning;

    // DEBUG TESTING
    private GLTexture? m_TestTexture;
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
        m_Game.Initialize();

        // DEBUG TESTING
        var imageData = m_AssetManager.LoadAsset("/home/ezroot/Repos/Integrity/DefaultEngineAssets/logo.png");
        Debug.Assert(m_RenderPipe.GlApi != null, "GL API is null in Engine.");
        m_TestTexture = new GLTexture(m_RenderPipe.GlApi, imageData);
        // END DEBUG
    }

    private void HandleInput()
    {
        m_InputManager?.ProcessInput();
    }

    private void Update()
    {
        m_Game.Update();
    }

    private void Render()
    {
        m_RenderPipe.RenderFrameStart();

        // DEBUG TESTING
        Debug.Assert(m_TestTexture != null, "Test texture is null in Engine Render.");
        m_RenderPipe.DrawTextureAt(m_TestTexture, 100, 50, 256, 256);
        // END DEBUG

        m_Game.Render();
        m_RenderPipe.RenderFrameEnd();
    }

    private void Cleanup()
    {
        m_Game.Cleanup();
    }
}