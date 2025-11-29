public interface IGameObjectFactory : IService
{
    GameObject CreateGameObject(string assetPath, IComponent[]? componentsToAdd = null);
}