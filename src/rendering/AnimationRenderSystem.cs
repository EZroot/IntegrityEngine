using Integrity.Components;
using Integrity.Objects;

namespace Integrity.Systems;

public class AnimationRenderSystem 
{
    private readonly List<(GameObject Object, AnimationComponent Animation, SpriteComponent Sprite)> m_AnimatedObjects;

    public AnimationRenderSystem()
    {
        m_AnimatedObjects = new List<(GameObject, AnimationComponent, SpriteComponent)>();
    }

    public void RegisterObject(GameObject obj)
    {
        var animationComp = obj.GetComponent<AnimationComponent>();
        var spriteComp = obj.GetComponent<SpriteComponent>();

        // Only register if all required components for animation logic are present
        if (animationComp != null && spriteComp != null)
        {
            m_AnimatedObjects.Add((obj, animationComp, spriteComp));
        }
        else
        {
            throw new Exception($"Game Object {obj.Name} ({obj.Id}) Missing AnimationComponent & SpriteComponent");
        }
    }

    public void Update(float deltaTime)
    {
        foreach (var tuple in m_AnimatedObjects)
        {
            var animComp = tuple.Animation;
            var spriteComp = tuple.Sprite;

            if (!animComp.IsPlaying || string.IsNullOrEmpty(animComp.CurrentAnimationName))
                continue;

            if (animComp.Animations.TryGetValue(animComp.CurrentAnimationName, out var frames) && frames.Count > 1)
            {
                animComp.FrameTimeAccumulator += deltaTime;
                var currentFrame = frames[animComp.CurrentFrameIndex];

                if (animComp.FrameTimeAccumulator >= currentFrame.Duration)
                {
                    animComp.CurrentFrameIndex = (animComp.CurrentFrameIndex + 1) % frames.Count;
                    animComp.FrameTimeAccumulator -= currentFrame.Duration;
                    
                    spriteComp.SourceRect = frames[animComp.CurrentFrameIndex].SourceRect;
                }
            }
        }
    }

    public void Shutdown()
    {
        m_AnimatedObjects.Clear();
    }
}