public class GameObjectFactory : IGameObjectFactory
{
    private readonly IAssetManager m_AssetManager;
    public GameObjectFactory()
    {
        m_AssetManager = Service.Get<IAssetManager>() ?? throw new ArgumentNullException(nameof(IAssetManager));
    }

    /// <summary>
    /// Creates a standard GameObject pre-configured with a TransformComponent cached.
    /// </summary>
    /// <param name="name">The Game Object's name.</param>
    /// <param name="componentsToAdd">Additional components to add to the game object.</param>
    /// <returns>A new GameObject instance.</returns>
    public GameObject CreateGameObject(string name, IComponent[]? componentsToAdd = null)
    {
        GameObject obj = new GameObject(name);
        if (componentsToAdd != null)
        {
            foreach (var component in componentsToAdd)
            {
                obj.AddComponent(component);
            }
        }
        return obj;
    }

    /// <summary>
    /// Creates a standard GameObject pre-configured with a TransformComponent and SpriteComponent cached.
    /// </summary>
    /// <param name="assetPath">The file path used to retrieve the shared GLTexture resource.</param>
    /// <param name="name">The Game Object's name.</param>
    /// <param name="componentsToAdd">Additional components to add to the game object.</param>
    /// <returns>A new GameObject instance.</returns>
    public SpriteObject CreateSpriteObject(string name, string assetPath, IComponent[]? componentsToAdd = null)
    {
        var texture = m_AssetManager.GetTexture(assetPath);
        SpriteObject obj = new SpriteObject(name, texture);
        if (componentsToAdd != null)
        {
            foreach (var component in componentsToAdd)
            {
                obj.AddComponent(component);
            }
        }
        return obj;
    }
}