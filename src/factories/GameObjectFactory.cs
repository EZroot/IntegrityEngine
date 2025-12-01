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
    /// <param name="assetPath">The file path used to retrieve the shared GLTexture resource.</param>
    /// <returns>A new GameObject instance.</returns>
    public GameObject CreateGameObject(IComponent[]? componentsToAdd = null)
    {
        GameObject obj = new GameObject();
        if(componentsToAdd != null)
        {
            foreach(var component in componentsToAdd)
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
    /// <returns>A new GameObject instance.</returns>
    public SpriteObject CreateSpriteObject(string assetPath, IComponent[]? componentsToAdd = null)
    {
        var texture = m_AssetManager.GetTexture(assetPath);
        SpriteObject obj = new SpriteObject(texture);
        if(componentsToAdd != null)
        {
            foreach(var component in componentsToAdd)
            {
                obj.AddComponent(component);
            }
        }
        return obj;
    }
}