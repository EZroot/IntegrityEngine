public class Scene
{
    public Guid Id { get; }
    public string Name { get; }

    private readonly Dictionary<Guid, GameObject> m_GameObjectsMap;
    private readonly List<GameObject> m_GameObjectsList;
    private SpriteRenderSystem m_SpriteRenderSystem;

    public SpriteRenderSystem SpriteRenderSystem => m_SpriteRenderSystem;

    public Scene(string name)
    {
        Id = Guid.NewGuid();
        Name = name;

        m_GameObjectsMap = new Dictionary<Guid, GameObject>(capacity: 1024);
        m_GameObjectsList = new List<GameObject>(capacity: 1024);
        m_SpriteRenderSystem = new SpriteRenderSystem();
    }

    /// <summary>
    /// Called when the scene is first loaded/activated. Initializes all GameObjects.
    /// </summary>
    public void Initialize()
    {
        Logger.Log($"Initializing scene: {Name}");

        foreach (var obj in m_GameObjectsList)
        {
            // obj.Initialize(); 
        }
    }

    public void RegisterGameObject(GameObject obj)
    {
        if (m_GameObjectsMap.TryAdd(obj.Id, obj))
        {
            m_GameObjectsList.Add(obj);
            m_SpriteRenderSystem.RegisterObject(obj);
        }
    }

    public GameObject? GetGameObjectById(Guid id)
    {
        m_GameObjectsMap.TryGetValue(id, out var obj);
        return obj;
    }

    // Property to allow systems to quickly iterate over all objects without modifying the storage
    public IReadOnlyList<GameObject> GetAllGameObjects() => m_GameObjectsList;
    public IReadOnlyList<SpriteObject> GetAllSpriteObjects() => m_SpriteRenderSystem.SpriteObjectList;

    /// <summary>
    /// Called when the scene is being unloaded. Cleans up all GameObjects.
    /// </summary>
    public void Cleanup()
    {
        Logger.Log($"Cleaning up scene: {Name}");

        // Iterate backwards to future proof for list removal if needed
        for (int i = m_GameObjectsList.Count - 1; i >= 0; i--)
        {
            var obj = m_GameObjectsList[i];
            // obj.Cleanup();
        }

        m_GameObjectsMap.Clear();
        m_GameObjectsList.Clear();
    }
}