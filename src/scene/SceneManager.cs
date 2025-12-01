public class SceneManager : ISceneManager
{
    public Dictionary<Guid, Scene> m_SceneMap;
    private Scene? m_CurrentScene;
    public Scene? CurrentScene => m_CurrentScene;

    public SceneManager()
    {
        m_SceneMap = new Dictionary<Guid, Scene>(capacity: 32);
    }

    /// <summary>
    /// Loads the scene, initializes all objects, and sets it as the active scene.
    /// </summary>
    public void LoadScene(Scene scene)
    {
        if (m_CurrentScene != null)
        {
            UnloadScene(m_CurrentScene);
        }

        m_CurrentScene = scene;
        Logger.Log($"Activating scene: {scene.Name} ({scene.Id})");

        scene.Initialize();
    }

    /// <summary>
    /// Cleans up all resources associated with a scene.
    /// </summary>
    public void UnloadScene(Scene scene)
    {
        if (scene == m_CurrentScene)
        {
            m_CurrentScene = null;
        }

        scene.Cleanup();
        Logger.Log($"Unloaded and removed scene: {scene.Name} ({scene.Id})");
    }

    public void AddScene(Scene scene)
    {
        m_SceneMap.Add(scene.Id, scene);
        Logger.Log($"Added scene: {scene.Name} -> {scene.Id}");
    }

    public Scene? GetScene(Guid id)
    {
        if (m_SceneMap.TryGetValue(id, out var val))
        {
            return val;
        }
        return null;
    }

    public Scene? GetScene(string sceneName)
    {
        foreach (var scene in m_SceneMap.Values)
        {
            if (scene.Name == sceneName)
                return scene;
        }
        return null;
    }
}