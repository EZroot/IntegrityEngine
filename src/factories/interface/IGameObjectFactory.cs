public interface IGameObjectFactory : IService
{
    GameObject CreateGameObject(IComponent[]? componentsToAdd = null);
    SpriteObject CreateSpriteObject(string assetPath, IComponent[]? componentsToAdd = null);
}