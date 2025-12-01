public interface IAssetManager : IService
{
    IReadOnlyDictionary<string, AssetInfo> GetLoadedAssets();
    GLTexture GetTexture(string assetPath);
}