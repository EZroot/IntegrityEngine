public class DefaultGame : IGame
{
    public void Initialize()
    {
        Logger.Log("DefaultGame initialized.", Logger.LogSeverity.Info);
    }

    public void Update(float deltaTime)
    {
        // Game update logic goes here
    }

    public void Render()
    {
        // Game rendering logic goes here
    }

    public void Cleanup()
    {
        Logger.Log("DefaultGame cleaned up.", Logger.LogSeverity.Info);
    }
}