using System.Runtime.InteropServices;
using Integrity.Assets;
using Integrity.Core;
using Integrity.Interface;
using StbImageSharp;
using StbVorbisSharp;

namespace Integirty.Assets;

public class AssetManager : IAssetManager
{
    private const int COLOR_CHANNELS_RGBA = 4;

    private readonly IRenderPipeline m_RenderPipeline;
    private readonly Dictionary<string, Texture> m_TextureCache = new();
    private readonly Dictionary<string, AudioClip> m_AudioCache = new();
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
    /// Retrieves a Texture for the specified asset path, loading and caching it if necessary.
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Texture GetTexture(string assetPath)
    {
        if (m_TextureCache.TryGetValue(assetPath, out Texture? texture))
        {
            return texture;
        }

        ImageData data = LoadAsset<ImageData>(assetPath);
        if (data.PixelData == null)
        {
            throw new InvalidOperationException($"Asset not ready: {assetPath}");
        }
        var glApi = m_RenderPipeline.GlApi ?? throw new InvalidOperationException("OpenGL API is not initialized in RenderPipeline.");
        Texture newTexture = new Texture(glApi, data);
        m_TextureCache.Add(assetPath, newTexture);
        return newTexture;
    }

    /// <summary>
    /// Retrieves an AudioClip for the specified asset path, loading and caching it if necessary.
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public AudioClip GetAudio(string assetPath)
    {
        if (m_AudioCache.TryGetValue(assetPath, out AudioClip? audioClip))
        {
            return audioClip;
        }

        AudioData data = LoadAsset<AudioData>(assetPath);
        if (data.SampleData == null)
        {
            throw new InvalidOperationException($"Asset data is null after loading: {assetPath}");
        }

        AudioClip newAudioClip = new AudioClip(Service.Get<IAudioManager>()!.AlApi, data);
        m_AudioCache.Add(assetPath, newAudioClip);
        return newAudioClip;
    }

    private T LoadAsset<T>(string assetPath) where T : struct
    {
        if (!File.Exists(assetPath))
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

        if (typeof(T) == typeof(AudioData))
        {
            var audioData = LoadAudio(assetPath);
            var audio = (T)(object)audioData;
            m_AssetCache.Add(assetPath, new AssetInfo(assetPath, $"{typeof(AudioData)}", audioData.SampleData.Length));
            return audio;
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
            if (image.Data == null)
            {
                Logger.Log($"Failed to load image data from asset at path: {assetPath}", Logger.LogSeverity.Error);
                return default;
            }

            return new ImageData(image.Data, image.Width, image.Height, COLOR_CHANNELS_RGBA);
        }
    }

    private AudioData LoadAudio(string assetPath)
    {
        byte[] audioFileBytes = File.ReadAllBytes(assetPath);

        short[] shortData = StbVorbis.decode_vorbis_from_memory(audioFileBytes, out int sampleRate, out int channels);

        if (shortData == null || shortData.Length == 0)
        {
            Logger.Log($"Failed to decode audio file (StbVorbisSharp error) at path: {assetPath}", Logger.LogSeverity.Error);
            return default;
        }

        var shortSizeInBytes = shortData.Length * sizeof(short);
        byte[] byteData = new byte[shortSizeInBytes];

        var byteSpan = byteData.AsSpan();
        var shortSpan = shortData.AsSpan();

        MemoryMarshal.Cast<short, byte>(shortSpan).CopyTo(byteSpan);

        Logger.Log($"Audio loaded: {assetPath} ({channels} channels, {sampleRate} Hz, {byteData.Length} bytes)", Logger.LogSeverity.Info);

        return new AudioData(
            sampleData: byteData,
            sampleRate: sampleRate,
            channels: channels,
            bitsPerSample: 16
        );
    }
}