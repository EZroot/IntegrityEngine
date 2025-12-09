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
    // Animation Definitions (Key: Animation Name, Value: List of Frames)
    public readonly Dictionary<string, List<AnimationFrame>> Animations;

    // State Tracking (Current Data)
    public string CurrentAnimationName { get; set; }
    public float FrameTimeAccumulator { get; set; }
    public int CurrentFrameIndex { get; set; }
    public bool IsPlaying { get; set; }

    public AnimationComponent()
    {
        Animations = new Dictionary<string, List<AnimationFrame>>();
        CurrentAnimationName = string.Empty;
        FrameTimeAccumulator = 0.0f;
        CurrentFrameIndex = 0;
        IsPlaying = false;
    }
    
    /// <summary>
    /// Adds a new animation sequence under a specific name.
    /// </summary>
    public void AddAnimation(string name, List<AnimationFrame> frames)
    {
        if (string.IsNullOrWhiteSpace(name) || frames == null || frames.Count == 0)
            return;

        Animations[name] = frames;

        // Automatically start playing the first animation added
        if (string.IsNullOrEmpty(CurrentAnimationName))
        {
            CurrentAnimationName = name;
            IsPlaying = true;
        }
    }

    public void Shutdown()
    {
        // Pure data, no resources to clean up.
    }
}