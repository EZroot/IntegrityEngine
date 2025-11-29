using Silk.NET.OpenGL;

public class GameObject
{
    private readonly Dictionary<Type, IComponent> m_ComponentMap = new();
    public TransformComponent Transform { get; }
    public SpriteComponent Sprite { get; }

    public GameObject(GLTexture spriteGlTexture)
    {
        Transform = new TransformComponent();
        AddComponent(Transform);
        Sprite = new SpriteComponent(spriteGlTexture);
        AddComponent(Sprite);
    }

    public void AddComponent(IComponent component)
    {
        var type = component.GetType();
        if (m_ComponentMap.ContainsKey(type))
        {
            Logger.Log($"Component of type {type.Name} already exists in GameObject! Will skip this component", Logger.LogSeverity.Warning);
            return;
        }
        Logger.Log($"Adding component of type {type.Name} to GameObject", Logger.LogSeverity.Info);
        m_ComponentMap[type] = component;
    }
}