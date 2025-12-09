using System.Numerics;
using Integrity.Core;
using Integrity.Interface;
using Integrity.Assets;
using Integrity.Utils; // Assuming Texture is in Assets

namespace Integrity.Rendering;

/// <summary>
/// Data for a single tile
/// </summary>
public class TileData
{
    public Texture Texture { get; set; }
    public Rect SourceRect { get; set; }
    public bool IsVisible { get; set; } = true;

    public TileData(Texture texture, Rect sourceRect)
    {
        Texture = texture;
        SourceRect = sourceRect;
    }
}

/// <summary>
/// Represents the geometry data for a single tile chunk (e.g., 32x32 tiles).
/// </summary>
public class TileChunk
{
    public List<float> Vertices { get; private set; } = new List<float>();
    public Dictionary<Vector2, TileData> TileDataMap { get; private set; } = new(); // Stores the data for all tiles in this chunk
    public Texture Texture { get; private set; }
    public uint VboId { get; set; }
    public int VertexCount => Vertices.Count / 4;
    public Vector2 ChunkId { get; private set; }

    public TileChunk(Texture texture, Vector2 chunkId)
    {
        Texture = texture;
        ChunkId = chunkId;
    }

    public bool IsDirty { get; set; } = true;
}


public class TileRenderSystem
{
    private readonly Dictionary<Vector2, TileChunk> m_TileChunks = new();
    private readonly IRenderPipeline m_RenderPipe;

    private const int CHUNK_SIZE_TILES = 32;
    private int m_TileSize = 32;

    private Matrix4x4 m_MatrixModel = Matrix4x4.Identity;

    public IReadOnlyDictionary<Vector2, TileChunk> TileChunks => m_TileChunks;

    public TileRenderSystem()
    {
        m_RenderPipe = Service.Get<IRenderPipeline>() ?? throw new Exception("RenderPipeline couldn't be found for TileRenderSystem!");
    }

    /// <summary>
    /// Adds or updates a tile in a chunk and flags the chunk for re-upload.
    /// </summary>
    public void SetTile(int mapX, int mapY, Texture texture, Rect sourceRect, bool isVisible = true)
    {
        int chunkX = mapX / CHUNK_SIZE_TILES;
        int chunkY = mapY / CHUNK_SIZE_TILES;
        Vector2 chunkId = new Vector2(chunkX, chunkY);

        if (!m_TileChunks.TryGetValue(chunkId, out var chunk))
        {
            // Use the first texture received for the chunk, assuming it's the atlas
            chunk = new TileChunk(texture, chunkId);
            m_TileChunks[chunkId] = chunk;
        }

        int localX = mapX % CHUNK_SIZE_TILES;
        int localY = mapY % CHUNK_SIZE_TILES;
        Vector2 localId = new Vector2(localX, localY);

        if (chunk.TileDataMap.TryGetValue(localId, out var tileData))
        {
            tileData.Texture = texture;
            tileData.SourceRect = sourceRect;
            tileData.IsVisible = isVisible;
        }
        else
        {
            chunk.TileDataMap[localId] = new TileData(texture, sourceRect) { IsVisible = isVisible };
        }

        RegenerateChunkMesh(chunk);
        chunk.IsDirty = true;
    }

    /// <summary>
    /// Sets a tilesize for regenerate chunk mesh. This should be called before you set the tiles.
    /// </summary>
    /// <param name="tileSize"></param>
    public void SetTileSize(int tileSize = 32)
    {
        m_TileSize = tileSize;
    }

    /// <summary>
    /// Builds a static mesh for a chunk 
    /// </summary>
    private void RegenerateChunkMesh(TileChunk chunk)
    {
        chunk.Vertices.Clear();

        float texW = chunk.Texture.Width;
        float texH = chunk.Texture.Height;

        float chunkOffsetX = chunk.ChunkId.X * CHUNK_SIZE_TILES * m_TileSize;
        float chunkOffsetY = chunk.ChunkId.Y * CHUNK_SIZE_TILES * m_TileSize;

        foreach (var kvp in chunk.TileDataMap)
        {
            TileData tileData = kvp.Value;
            if (!tileData.IsVisible) continue;

            int localX = (int)kvp.Key.X;
            int localY = (int)kvp.Key.Y;

            Rect sourceRect = tileData.SourceRect;

            float worldX = chunkOffsetX + localX * m_TileSize;
            float worldY = chunkOffsetY + localY * m_TileSize;

            float rectX = sourceRect.X / texW;
            float rectY = sourceRect.Y / texH;
            float rectW = sourceRect.Width / texW;
            float rectH = sourceRect.Height / texH;

            float uv_left_u = rectX;
            float uv_right_u = rectX + rectW;

            float uv_top_v = rectY;

            float uv_bottom_v = rectY + rectH;

            chunk.Vertices.Add(worldX); chunk.Vertices.Add(worldY + m_TileSize);
            chunk.Vertices.Add(uv_left_u); chunk.Vertices.Add(uv_bottom_v);

            chunk.Vertices.Add(worldX); chunk.Vertices.Add(worldY);
            chunk.Vertices.Add(uv_left_u); chunk.Vertices.Add(uv_top_v);

            chunk.Vertices.Add(worldX + m_TileSize); chunk.Vertices.Add(worldY);
            chunk.Vertices.Add(uv_right_u); chunk.Vertices.Add(uv_top_v);

            chunk.Vertices.Add(worldX); chunk.Vertices.Add(worldY + m_TileSize);
            chunk.Vertices.Add(uv_left_u); chunk.Vertices.Add(uv_bottom_v);

            chunk.Vertices.Add(worldX + m_TileSize); chunk.Vertices.Add(worldY);
            chunk.Vertices.Add(uv_right_u); chunk.Vertices.Add(uv_top_v);

            chunk.Vertices.Add(worldX + m_TileSize); chunk.Vertices.Add(worldY + m_TileSize);
            chunk.Vertices.Add(uv_right_u); chunk.Vertices.Add(uv_bottom_v);
        }
    }

    /// <summary>
    /// Processes and renders all tile chunks.
    /// </summary>
    public void RenderTiles()
    {
        var chunksToRender = m_TileChunks.Values.OrderBy(c => c.Texture?.TextureId ?? 0);

        foreach (var chunk in chunksToRender)
        {
            if (chunk.VertexCount == 0) continue;

            if (chunk.IsDirty)
            {
                m_RenderPipe.UpdateTileChunkVbo(chunk);
                chunk.IsDirty = false;
            }

            m_RenderPipe.DrawStaticMesh(chunk.Texture, chunk.VboId, chunk.VertexCount, in m_MatrixModel);
        }
    }
}