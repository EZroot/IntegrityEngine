using System.Numerics;
using Integrity.Core;
using Integrity.Interface;
using Integrity.Objects;
using Integrity.Utils;

namespace Integrity.Rendering;
public class SpriteRenderSystem
{
    private IRenderPipeline m_RenderPipe;
    private readonly Dictionary<Assets.Texture, List<Matrix4x4>> m_RenderingBatchMap;
    private readonly Dictionary<Assets.Texture, List<Vector4>> m_UvBatchMap;

    private readonly List<SpriteObject> m_SpriteObjectList = new();

    public List<SpriteObject> SpriteObjectList => m_SpriteObjectList;

    public SpriteRenderSystem()
    {
        m_RenderingBatchMap = new();
        m_UvBatchMap = new();
        m_RenderPipe = Service.Get<IRenderPipeline>() ?? throw new Exception("Render pipeline couldn't be found by sprite render system!");
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

            float x = obj.Transform.X;
            float y = obj.Transform.Y;
            float scaleX = obj.Transform.ScaleX;
            float scaleY = obj.Transform.ScaleY;
            float rot = obj.Transform.Rotation; 
            
            float spriteWidth = sprite.SourceRect.Width * scaleX;
            float spriteHeight = sprite.SourceRect.Height * scaleY;
            Vector2 pivot = obj.Sprite.Pivot;

            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(spriteWidth, spriteHeight, 1.0f);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(rot); 
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(x, y, 0.0f);
            Matrix4x4 pivotTranslation = Matrix4x4.CreateTranslation(-pivot.X, -pivot.Y, 0.0f);
            
            Matrix4x4 model = pivotTranslation * scaleMatrix * rotationMatrix * translationMatrix;

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
    public void RenderSprites()
    {
        foreach (var kvp in m_RenderingBatchMap)
        {
            var texture = kvp.Key;
            var matrices = kvp.Value;
            if (m_UvBatchMap.TryGetValue(texture, out var uvrects))
            {
                m_RenderPipe.DrawSpritesInstanced(texture, matrices, uvrects, matrices.Count);
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