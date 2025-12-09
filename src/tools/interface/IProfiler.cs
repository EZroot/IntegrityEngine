using System.Collections.ObjectModel;
using static Integrity.Tools.Profiler;
namespace Integrity.Interface;

public interface IProfiler : IService
{
    ReadOnlyDictionary<string, ProfileResultGpu> RenderProfileResults { get; }
    ReadOnlyDictionary<string, ProfileResultCpu> CpuProfileResults { get; }

    void InitializeRenderProfiler(string tag);
    void StartRenderProfile(string tag);
    void StopRenderProfile(string tag);
    ProfileResultGpu? GetRenderProfile(string tag);

    void StartCpuProfile(string tag);
    void StopCpuProfile(string tag);
    ProfileResultCpu? GetCpuProfile(string tag);
    void Cleanup();
}