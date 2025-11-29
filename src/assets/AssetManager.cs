using StbImageSharp;

public class AssetManager : IAssetManager
{
    private const int COLOR_CHANNELS_RGBA = 4;

    private readonly IRenderPipeline m_RenderPipeline;
    private readonly Dictionary<string, GLTexture> m_TextureCache = new();

    public AssetManager()
    {
        m_RenderPipeline = Service.Get<IRenderPipeline>() ?? throw new InvalidOperationException("RenderPipeline service is not available in ServiceLocator.");
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

        ImageData data = LoadAsset(assetPath);
        if (data.PixelData == null)
        {
            throw new InvalidOperationException($"Asset not ready: {assetPath}");
        }
        var glApi = m_RenderPipeline.GlApi ?? throw new InvalidOperationException("OpenGL API is not initialized in RenderPipeline.");
        GLTexture newTexture = new GLTexture(glApi, data); 
        m_TextureCache.Add(assetPath, newTexture);
        return newTexture;
    }

    private ImageData LoadAsset(string assetPath)
    {
        if(!File.Exists(assetPath))
        {
            Logger.Log($"Asset not found at path: {assetPath}", Logger.LogSeverity.Error);
            return default;
        }
        
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