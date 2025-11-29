public class GameObjectFactory : IGameObjectFactory
{
    private readonly IAssetManager m_AssetManager;
    public GameObjectFactory()
    {
        m_AssetManager = Service.Get<IAssetManager>() ?? throw new ArgumentNullException(nameof(IAssetManager));
    }

    /// <summary>
    /// Creates a standard GameObject pre-configured with a TransformComponent and a SpriteComponent.
    /// </summary>
    /// <param name="assetPath">The file path used to retrieve the shared GLTexture resource.</param>
    /// <returns>A new GameObject instance.</returns>
    public GameObject CreateGameObject(string assetPath, IComponent[]? componentsToAdd = null)
    {
        var texture = m_AssetManager.GetTexture(assetPath); 
        if (texture == null)
        {
            throw new InvalidOperationException($"Failed to create sprite object: Texture for path '{assetPath}' not found."); 
        }

        GameObject obj = new GameObject(texture);
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