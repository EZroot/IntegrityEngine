using StbImageSharp;

public class AssetManager : IAssetManager
{
    private const int COLOR_CHANNELS_RGBA = 4;

    private readonly IRenderPipeline m_RenderPipeline;
    private readonly Dictionary<string, GLTexture> m_TextureCache = new();
    private readonly Dictionary<string, AssetInfo> m_AssetCache = new();

    public AssetManager()
    {
        m_RenderPipeline = Service.Get<IRenderPipeline>() ?? throw new InvalidOperationException("RenderPipeline service is not available in ServiceLocator.");
    }

    public IReadOnlyDictionary<string, AssetInfo> GetLoadedAssets()
    {
        return m_AssetCache;
    }

    /// <summary>
    /// Retrieves a GLTexture for the specified asset path, loading and caching it if necessary.
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public GLTexture GetTexture(string assetPath)
    {
        if (m_TextureCache.TryGetValue(assetPath, out GLTexture? texture))
        {
            return texture; 
        }

        ImageData data = LoadAsset<ImageData>(assetPath);
        if (data.PixelData == null)
        {
            throw new InvalidOperationException($"Asset not ready: {assetPath}");
        }
        var glApi = m_RenderPipeline.GlApi ?? throw new InvalidOperationException("OpenGL API is not initialized in RenderPipeline.");
        GLTexture newTexture = new GLTexture(glApi, data); 
        m_TextureCache.Add(assetPath, newTexture);
        return newTexture;
    }

    private T LoadAsset<T>(string assetPath) where T : struct
    {
        if(!File.Exists(assetPath))
        {
            Logger.Log($"Asset not found at path: {assetPath}", Logger.LogSeverity.Error);
            return default;
        }
        
        if (typeof(T) == typeof(ImageData))
        {
            var sprite = (T)(object)LoadSprite(assetPath);
            var imageData = (ImageData)(object)sprite;
            m_AssetCache.Add(assetPath, new AssetInfo(assetPath, $"{typeof(ImageData)}", imageData.PixelData.Length));
            return sprite;
        }

        Logger.Log($"Unsupported asset type requested: {typeof(T).FullName}", Logger.LogSeverity.Error);
        return default;
    }

    private ImageData LoadSprite(string assetPath)
    {
        using (var stream = File.OpenRead(assetPath))
        {
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            Logger.Log($"Asset loaded: {image.Width}x{image.Height}, {image.Data.Length} bytes", Logger.LogSeverity.Info);
            if(image.Data == null)
            {
                Logger.Log($"Failed to load image data from asset at path: {assetPath}", Logger.LogSeverity.Error);
                return default;
            }

            return new ImageData(image.Data, image.Width, image.Height, COLOR_CHANNELS_RGBA);
        }
    }
}