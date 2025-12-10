using System.Numerics;
using Integrity.Core;
using Integrity.Interface;
using Integrity.Objects;
using Integrity.Assets;

namespace Integrity.Rendering;

public class SpriteRenderSystem
{
    private IRenderPipeline m_RenderPipe;
    private readonly Dictionary<Texture, List<Matrix4x4>> m_RenderingBatchMap;
    private readonly Dictionary<Texture, List<Vector4>> m_UvBatchMap;
    private readonly Dictionary<Texture, List<Vector4>> m_ColorBatchMap;

    private readonly List<SpriteObject> m_SpriteObjectList = new();

    public List<SpriteObject> SpriteObjectList => m_SpriteObjectList;

    public SpriteRenderSystem()
    {
        m_RenderingBatchMap = new();
        m_UvBatchMap = new();
        m_ColorBatchMap = new(); 
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
        UpdateSpriteSortByDepth();

        var sceneGameObjects = m_SpriteObjectList;
        m_RenderingBatchMap.Clear();
        m_UvBatchMap.Clear();
        m_ColorBatchMap.Clear(); 

        foreach (var obj in sceneGameObjects)
        {
            if (obj.Sprite == null) continue;

            var sprite = obj.Sprite;
            var texture = sprite.Texture;
            
            // Model matrices
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

            // Batch uv rects
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

            // Batch colors
            if (!m_ColorBatchMap.TryGetValue(texture, out var instancedColorList))
            {
                instancedColorList = new List<Vector4>();
                m_ColorBatchMap[texture] = instancedColorList;
            }

            instancedColorList.Add(sprite.Color); 
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

            if (m_UvBatchMap.TryGetValue(texture, out var uvrects) && 
                m_ColorBatchMap.TryGetValue(texture, out var tintColors) && // Get the color list
                matrices.Count == uvrects.Count && 
                matrices.Count == tintColors.Count) 
            {
                m_RenderPipe.DrawSpritesInstanced(
                    texture, 
                    matrices, 
                    uvrects, 
                    tintColors,
                    matrices.Count
                );
            }
        }
    }

    /// <summary>
    /// Sort Sprite Objects by Y-depth and Layer id
    /// </summary>
    private void UpdateSpriteSortByDepth()
    {
        var list = m_SpriteObjectList;
        var n = list.Count;
        for (var i = 1; i < n; ++i)
        {
            var key = list[i];
            var j = i - 1;
            var swapNeeded = true;
            
            while (j >= 0 && swapNeeded)
            {
                var current = list[j];

                if (key.Sprite.Layer.CompareTo(current.Sprite.Layer) < 0)
                {
                    swapNeeded = true;
                }
                else if (key.Sprite.Layer.CompareTo(current.Sprite.Layer) == 0)
                {
                    if (key.Transform.Y.CompareTo(current.Transform.Y) < 0)
                    {
                        swapNeeded = true;
                    }
                    else
                    {
                        swapNeeded = false;
                    }
                }
                else 
                {
                    swapNeeded = false;
                }

                if (swapNeeded)
                {
                    list[j + 1] = list[j];
                    j = j - 1;
                }
            }
            
            list[j + 1] = key;
        }
    }

    /// <summary>
    /// Sort sprite objects by Y-depth
    /// </summary>
    private void UpdateSpriteSortByDepthOnly()
    {
        var list = m_SpriteObjectList;
        var n = list.Count;
        for (var i = 1; i < n; ++i)
        {
            SpriteObject key = list[i];
            var j = i - 1;
            while (j >= 0 && key.Transform.Y.CompareTo(list[j].Transform.Y) < 0)
            {
                list[j + 1] = list[j];
                j = j - 1;
            }
            list[j + 1] = key;
        }
    }
}