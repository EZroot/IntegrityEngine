using Integrity.Assets;

namespace Integrity.Interface;
public interface IAssetManager : IService
{
    IReadOnlyDictionary<string, AssetInfo> GetLoadedAssets();
    Texture GetTexture(string assetPath);
    AudioClip GetAudio(string assetPath);
}