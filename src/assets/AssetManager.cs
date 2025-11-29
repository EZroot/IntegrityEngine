using StbImageSharp;

public class AssetManager : IAssetManager
{
    private const int COLOR_CHANNELS_RGBA = 4;

    public void InitializeAssets()
    {
        throw new NotImplementedException();
    }

    public ImageData LoadAsset(string assetPath)
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