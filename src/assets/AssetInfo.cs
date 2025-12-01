public struct AssetInfo
{
    public string AssetPath { get; set; }
    public string Type { get; set; }    
    public long MemoryFootprintBytes { get; set; }

    public AssetInfo(string assetPath, string type, long memoryFootprintBytes)
    {
        AssetPath = assetPath;
        Type = type;
        MemoryFootprintBytes = memoryFootprintBytes;
    }
}