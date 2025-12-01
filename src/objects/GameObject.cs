using Silk.NET.OpenGL;

public class GameObject
{
    private readonly Dictionary<Type, IComponent> m_ComponentMap = new();
    public TransformComponent Transform { get; }
    public Guid Id { get; }
    public string Name { get; }

    public GameObject(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Transform = new TransformComponent();
        AddComponent(Transform);
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

    public T GetComponent<T>() where T : IComponent
    {
        if (m_ComponentMap.TryGetValue(typeof(T), out var component))
            return (T)component;

        Logger.Log($"Component of type {typeof(T).Name} not found in GameObject.", Logger.LogSeverity.Warning);
        return default!;
    }
}