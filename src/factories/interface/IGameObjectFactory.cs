public interface IGameObjectFactory : IService
{
    GameObject CreateGameObject(string name, IComponent[]? componentsToAdd = null);
    SpriteObject CreateSpriteObject(string name, string assetPath, IComponent[]? componentsToAdd = null);
}