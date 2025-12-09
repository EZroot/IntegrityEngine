using Integrity.Interface;
using Integrity.Utils;

namespace Integrity.Components;

/// <summary>
/// Defines a single frame in an animation sequence.
/// </summary>
public class AnimationFrame
{
    public Rect SourceRect { get; }
    public float Duration { get; }

    public AnimationFrame(Rect sourceRect, float duration)
    {
        SourceRect = sourceRect;
        Duration = duration;
    }
}

/// <summary>
/// Data container for all animation definitions and current playback state.
/// </summary>
public class AnimationComponent : IComponent
{
    public readonly Dictionary<string, AnimationFrame[]> Animations;

    public string CurrentAnimationName { get; set; }
    public float FrameTimeAccumulator { get; set; }
    public int CurrentFrameIndex { get; set; }
    public bool IsPlaying { get; set; }

    public AnimationComponent()
    {
        Animations = new Dictionary<string, AnimationFrame[]>();
        CurrentAnimationName = string.Empty;
        FrameTimeAccumulator = 0.0f;
        CurrentFrameIndex = 0;
        IsPlaying = false;
    }

    /// <summary>
    /// Adds a new animation sequence under a specific name.
    /// </summary>
    public void AddAnimation(string name, AnimationFrame[] frames)
    {
        if (string.IsNullOrWhiteSpace(name) || frames == null || frames.Length == 0)
            return;

        Animations[name] = frames;

        // Automatically start playing the first animation added
        if (string.IsNullOrEmpty(CurrentAnimationName))
        {
            CurrentAnimationName = name;
            IsPlaying = true;
        }
    }

}