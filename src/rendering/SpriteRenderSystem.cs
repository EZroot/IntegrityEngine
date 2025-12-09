using System.Numerics;
using Integrity.Core;
using Integrity.Interface;
using Integrity.Objects;
using Integrity.Utils;

namespace Integrity.Rendering;
public class SpriteRenderSystem
{
    private readonly Dictionary<Assets.Texture, List<Matrix4x4>> m_RenderingBatchMap;
    private readonly Dictionary<Assets.Texture, List<Vector4>> m_UvBatchMap;

    private readonly List<SpriteObject> m_SpriteObjectList = new();

    public List<SpriteObject> SpriteObjectList => m_SpriteObjectList;

    public SpriteRenderSystem()
    {
        m_RenderingBatchMap = new();
        m_UvBatchMap = new();
    }

    /// <summary>
    /// Register a GameObject to the sprite render system to be sorted and batched
    /// </summary>
    /// <param name="obj"></param>
    public void RegisterObject(GameObject obj)
    {
        if (obj is SpriteObject spriteObj)
        {
            m_SpriteObjectList.Add(spriteObj);
        }
    }

    /// <summary>
    /// Batch sprite objects by texture id for efficient GPU upload
    /// </summary>
    public void UpdateSpriteBatchByTexture()
    {
        // Batch object positions by texture
        UpdateSpriteSortByDepth();

        var sceneGameObjects = m_SpriteObjectList;
        m_RenderingBatchMap.Clear();
        m_UvBatchMap.Clear();

        foreach (var obj in sceneGameObjects)
        {
            if (obj.Sprite == null) continue;

            var sprite = obj.Sprite;
            var texture = sprite.Texture;

            // Sort positions by texture
            if (!m_RenderingBatchMap.TryGetValue(texture, out var instancedSpritePositionList))
            {
                instancedSpritePositionList = new List<Matrix4x4>();
                m_RenderingBatchMap[texture] = instancedSpritePositionList;
            }

            var model = MathHelper.Translation(
                obj.Transform.X, obj.Transform.Y,
                obj.Sprite.SourceRect.Width * obj.Transform.ScaleX,
                obj.Sprite.SourceRect.Height * obj.Transform.ScaleY
            );

            instancedSpritePositionList.Add(model);

            // Sort UVs on atlas by texture
            if (!m_UvBatchMap.TryGetValue(texture, out var instancedUvRectList))
            {
                instancedUvRectList = new List<Vector4>();
                m_UvBatchMap[texture] = instancedUvRectList;
            }

            float texW = texture.Width;
            float texH = texture.Height;

            float rectX = sprite.SourceRect.X / texW;
            float rectY = sprite.SourceRect.Y / texH;
            float rectW = sprite.SourceRect.Width / texW;
            float rectH = sprite.SourceRect.Height / texH;

            instancedUvRectList.Add(new Vector4(rectX, rectY, rectW, rectH));
        }
    }

    /// <summary>
    /// Render sprites by batch
    /// </summary>
    /// <param name="renderPipe"></param>
    public void RenderSprites(IRenderPipeline renderPipe)
    {
        foreach (var kvp in m_RenderingBatchMap)
        {
            var texture = kvp.Key;
            var matrices = kvp.Value;
            if (m_UvBatchMap.TryGetValue(texture, out var uvrects))
            {
                renderPipe.DrawSpritesInstanced(texture, matrices, uvrects, matrices.Count);
            }
        }
    }

    /// <summary>
    /// Sort Sprite Objects by Y-depth. ** WARNING: SLOW **
    /// </summary>
    private void UpdateSpriteSortByDepth()
    {
        // Incremental sort, should be faster
        var list = m_SpriteObjectList;
        int n = list.Count;
        for (int i = 1; i < n; ++i)
        {
            SpriteObject key = list[i];
            int j = i - 1;
            while (j >= 0 && key.Transform.Y.CompareTo(list[j].Transform.Y) < 0)
            {
                list[j + 1] = list[j];
                j = j - 1;
            }
            list[j + 1] = key;
        }
    }
}