using System.Numerics;
using ImGuiNET;
using Integrity.Core;
using Integrity.Interface;
using Integrity.Objects;
using Integrity.Utils;

namespace Integrity.Tools;
public class ToolGui
{
    private const float MEMORY_UPDATE_INTERVAL = 1.0f;
    private float m_TimeSinceLastMemoryUpdate = 0.0f;
    private bool m_CaptureMemory;
    private MemoryMonitor.MemorySnapshot m_SnapshotCache;

    private bool m_IsVsyncEnabled;
    private Vector3 m_ClearColor = new(0.1f, 0.1f, 0.15f);
    private bool m_EngineStatusWindowOpened;
    private bool m_MonitorWindowOpened;
    private bool m_ProfilerWindowOpened;
    private bool m_SettingsWindowOpened;
    private bool m_AssetsWindowOpened;

    private const int HISTORY_LENGTH = 120;
    private readonly Queue<float> m_GCMemoryHistory = new Queue<float>(HISTORY_LENGTH);
    private readonly Queue<float> m_FrameTimeHistory = new Queue<float>(HISTORY_LENGTH);
    private readonly Queue<float> m_RenderTimeHistory = new Queue<float>(HISTORY_LENGTH);

    public void DrawToolsUpdate(float deltaTime)
    {
        UpdateProfilerHistory(Service.Get<IProfiler>()!);
        m_TimeSinceLastMemoryUpdate += deltaTime;
        if (m_TimeSinceLastMemoryUpdate >= MEMORY_UPDATE_INTERVAL)
        {
            if (m_CaptureMemory)
            {
                m_SnapshotCache = MemoryMonitor.LogMemoryUsage();
                UpdateMemoryHistory(m_SnapshotCache);
            }

            m_TimeSinceLastMemoryUpdate -= MEMORY_UPDATE_INTERVAL;
        }
    }

    public void UpdateProfilerHistory(IProfiler profiler)
    {
        if (m_FrameTimeHistory.Count >= HISTORY_LENGTH)
        {
            m_FrameTimeHistory.Dequeue();
        }

        if (m_RenderTimeHistory.Count >= HISTORY_LENGTH)
        {
            m_RenderTimeHistory.Dequeue();
        }

        float maxCpuTime = profiler.CpuProfileResults.Values.Max(r => (float?)r.LastTimeMs) ?? 0.0f;
        float maxRenderTime = profiler.RenderProfileResults.Values.Max(r => (float?)r.LastTimeMs) ?? 0.0f;

        m_FrameTimeHistory.Enqueue(maxCpuTime);
        m_RenderTimeHistory.Enqueue(maxRenderTime);
    }

    public void UpdateMemoryHistory(MemoryMonitor.MemorySnapshot currentSnapshot)
    {
        if (m_GCMemoryHistory.Count >= HISTORY_LENGTH)
        {
            m_GCMemoryHistory.Dequeue();
        }
        m_GCMemoryHistory.Enqueue(currentSnapshot.ManagedHeapSizeMB);
    }

    public void DrawTools(IProfiler profiler)
    {
        if (m_ProfilerWindowOpened)
        {
            DrawProfilerTool(profiler);
        }

        if (m_MonitorWindowOpened)
        {
            DrawMonitorStatusTool();
        }

        if (m_EngineStatusWindowOpened)
        {
            DrawEngineStatusTool(Service.Get<ISceneManager>()!, Service.Get<ICameraManager>()!);
        }

        if(m_AssetsWindowOpened)
        {
            DrawAssets();
        }

        if(m_SettingsWindowOpened)
        {
            DrawSettingsTool(Service.Get<IWindowPipeline>()!);
        }
    }

public void DrawEngineStatusTool(ISceneManager sceneManager, ICameraManager cameraManager)
    {
        // Must implement these in your code if they are not already.
        // I've provided placeholder implementations below.
        
        if (ImGui.Begin("Inspector"))
        {
            if (ImGui.BeginTabBar("EngineTabs"))
            {
                if (ImGui.BeginTabItem("Scene"))
                {
                    var currentScene = sceneManager.CurrentScene;

                    if (currentScene == null)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), "Error: No active scene loaded.");
                        ImGui.EndTabItem();
                        ImGui.EndTabBar(); // Clean up if we return early
                        ImGui.End(); // Clean up if we return early
                        return;
                    }

                    ImGui.Text("Current Scene Details:");

                    if (ImGui.BeginTable("SceneInfoTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 150.0f);
                        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableHeadersRow();
                        
                        // Scene Name
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("Name");
                        ImGui.TableSetColumnIndex(1); ImGui.TextColored(new Vector4(0.8f, 1.0f, 0.8f, 1.0f), $"{currentScene.Name}");

                        // Scene ID
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("ID");
                        ImGui.TableSetColumnIndex(1); ImGui.TextDisabled($"{currentScene.Id}");

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Separator();
                        ImGui.TableSetColumnIndex(1); ImGui.Separator();

                        // Total Game Objects
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("Total Game Objects");
                        ImGui.TableSetColumnIndex(1); ImGui.Text($"{currentScene.GetAllGameObjects().Count}");

                        // Total Sprite Objects
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("Total Sprite Objects");
                        ImGui.TableSetColumnIndex(1); ImGui.Text($"{currentScene.GetAllSpriteObjects().Count}");

                        ImGui.EndTable();
                    }

                    ImGui.Spacing();
                    ImGui.Separator();

                    if (ImGui.CollapsingHeader("Main Camera Settings"))
                    {
                        if (cameraManager.MainCamera != null)
                        {
                            var camera = cameraManager.MainCamera;

                            ImGui.PushID(camera.GetHashCode());
                            // NOTE: Assuming your Camera class implements IComponent or is compatible with DrawObjectProperties
                            if (ImGui.TreeNodeEx("CameraDetails", ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.DefaultOpen, $"Camera: {camera.Name}"))
                            {
                                DrawObjectProperties(camera, "MainCamera", null, null);
                                ImGui.TreePop();
                            }
                            ImGui.PopID();
                        }
                        else
                        {
                            ImGui.TextDisabled("Camera Manager: No Main Camera set.");
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();

                    if (ImGui.CollapsingHeader("Scene Game Objects"))
                    {
                        // Use a fixed height or ImGui.GetContentRegionAvail().Y for better layout
                        if (ImGui.BeginChild("SceneObjectList", new Vector2(0, ImGui.GetContentRegionAvail().Y)))
                        {
                            foreach (var obj in currentScene.GetAllGameObjects())
                            {
                                // Use PushID to ensure unique ImGui IDs inside the loop
                                ImGui.PushID(obj.Id.ToString()); 
                                
                                if (ImGui.TreeNode(obj.Id.ToString(), $"{obj.Name} ({obj.Id.ToString()[..8]}...)"))
                                {
                                    // Calls the helper function
                                    var componentMap = GetPrivateComponentMap(obj); 
                                    if (componentMap != null)
                                    {
                                        ImGui.SeparatorText("Components");

                                        foreach (var kvp in componentMap)
                                        {
                                            var component = kvp.Value;
                                            string componentName = component.GetType().Name;

                                            ImGui.PushID(component.GetHashCode());
                                            DrawObjectProperties(component, componentName, null, null);

                                            ImGui.PopID();
                                        }
                                    }

                                    ImGui.TreePop();
                                }
                                ImGui.PopID(); // Pop the GameObject ID
                            }
                            ImGui.EndChild();
                        }
                    }
                    ImGui.EndTabItem(); // END "Scene"
                }
                ImGui.EndTabBar(); // END "EngineTabs"
            }

            ImGui.End(); // END "Inspector"
        }
    }

    //-------------------------------------------------------------------------
    // REQUIRED HELPER FUNCTIONS (Placeholder/Example Implementations)
    //-------------------------------------------------------------------------
    


private void DrawAssets()
{
    // 1. Begin the Tab Bar
    if (ImGui.BeginTabBar("AssetManagerTabBar"))
    {
        // 2. Begin the Tab Item (This is the line that previously caused the error)
        if (ImGui.BeginTabItem("Assets"))
        {
            var assetManager = Service.Get<IAssetManager>();
            
            // Check if the service exists before accessing it
            if (assetManager != null)
            {
                var loadedAssets = assetManager.GetLoadedAssets();

                ImGui.Text($"Asset Manager Status");
                ImGui.Separator();

                ImGui.Text($"Total Loaded Assets: {loadedAssets.Count}");
                ImGui.Spacing();

                if (ImGui.BeginTable("LoadedAssetTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("Name / Details", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableSetupColumn("Size (MB)", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                    ImGui.TableHeadersRow();

                    const float MB = 1024f * 1024f;
                    foreach (var kvp in loadedAssets)
                    {
                        string path = kvp.Key;
                        var assetInfo = kvp.Value;

                        // Ensure GetTexture and TextureId access is safe if assetInfo.Type is "ImageData"
                        bool isTexture = assetInfo.Type.Equals("ImageData", StringComparison.OrdinalIgnoreCase);
                        Assets.Texture glTexture = isTexture ? assetManager.GetTexture(path) : null;
                        
                        // Check if texture is valid before trying to draw/access properties
                        bool textureValid = isTexture && glTexture != null && glTexture.TextureId != IntPtr.Zero;

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.PushID(path);

                        ImGuiTreeNodeFlags flags = textureValid ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                        if (ImGui.TreeNodeEx(path, flags, path))
                        {
                            ImGui.Text($"Full Path: {path}");
                            ImGui.Text($"Bytes: {assetInfo.MemoryFootprintBytes:N0}");
                            if (textureValid)
                            {
                                ImGui.Separator();
                                Vector2 previewSize = new Vector2(128, 128);
                                float aspect = (float)glTexture.Width / glTexture.Height;
                                if (glTexture.Width > glTexture.Height)
                                {
                                    previewSize.Y = previewSize.X / aspect;
                                }
                                else
                                {
                                    previewSize.X = previewSize.Y * aspect;
                                }
                                
                                // Casting IntPtr to nint is correct for ImGui.Image in ImGui.NET
                                ImGui.Image((nint)glTexture.TextureId, previewSize);
                                ImGui.Text($"Dimensions: {glTexture.Width}x{glTexture.Height}");
                            }
                            ImGui.TreePop();
                        }

                        ImGui.PopID();

                        ImGui.TableSetColumnIndex(1);
                        ImGui.Text(assetInfo.Type);

                        ImGui.TableSetColumnIndex(2);
                        float sizeMB = assetInfo.MemoryFootprintBytes / MB;

                        ImGui.Text($"{sizeMB:F2} MB");
                    }

                    ImGui.EndTable();
                }
            }
            
            // 3. End the Tab Item
            ImGui.EndTabItem();
        }

        // 4. End the Tab Bar
        ImGui.EndTabBar();
    }
}

    private void DrawSettingsTool(IWindowPipeline windowPipe)
    {
        if (ImGui.Begin("EngineSettings"))
        {
            if (ImGui.BeginTabBar("EngineTabSettings"))
            {
                if (ImGui.BeginTabItem("Config"))
                {
                    ImGui.Text("Graphics Settings:");

                    if (ImGui.Checkbox("Enable V-Sync", ref m_IsVsyncEnabled))
                    {
                        windowPipe.SetVSync(m_IsVsyncEnabled == true ? 1 : 0);
                    }

                    ImGui.Spacing();

                    // ImGui.Text("Audio Settings:");
                    // if (ImGui.SliderFloat("Global Volume", ref m_GlobalVolume, 0.0f, 1.0f))
                    // {
                    //     // m_AudioManager.SetGlobalVolume(m_GlobalVolume);
                    // }

                    // ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Text("Renderer:");
                    ImGui.Separator();
                    if (ImGui.ColorEdit3("GL Clear Color", ref m_ClearColor))
                    {
                        var color = System.Drawing.Color.FromArgb(
                            (int)(m_ClearColor.X * 255),
                            (int)(m_ClearColor.Y * 255),
                            (int)(m_ClearColor.Z * 255)
                        );
                        Service.Get<IRenderPipeline>()!.SetClearColor(color);
                    }

                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }

    private void DrawMonitorStatusTool()
    {
        if (ImGui.Begin("Memory Monitor"))
        {
            if (ImGui.BeginTabBar("MonitorTabs"))
            {
                if (ImGui.BeginTabItem("Monitor"))
                {
                    ImGui.Text("Memory Monitor:");

                    if (ImGui.Button("Force GC Collection"))
                    {
                        GC.Collect();
                        Logger.Log("Garbage Collection FORCED!", Logger.LogSeverity.Warning);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"Capture Snapshot ({m_CaptureMemory})"))
                    {
                        m_CaptureMemory = !m_CaptureMemory;
                    }

                    ImGui.Separator();
                    ImGui.Text("Current Metrics (Auto-Updating):");

                    if (ImGui.BeginTable("MemoryTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("Metric");
                        ImGui.TableSetupColumn("Value (MB)", ImGuiTableColumnFlags.WidthFixed, 80.0f);
                        ImGui.TableSetupColumn("Value (Bytes)");
                        ImGui.TableHeadersRow();

                        void DrawMemoryRow(string label, float mbValue, long byteValue)
                        {
                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            ImGui.Text(label);

                            ImGui.TableSetColumnIndex(1);
                            ImGui.TextDisabled($"{mbValue:F2}");

                            ImGui.TableSetColumnIndex(2);
                            ImGui.Text($"{byteValue:N0}");
                        }

                        DrawMemoryRow("GC Heap", m_SnapshotCache.ManagedHeapSizeMB, m_SnapshotCache.ManagedHeapSizeBytes);
                        DrawMemoryRow("Private Working Set", m_SnapshotCache.PrivateWorkingSetMB, m_SnapshotCache.PrivateWorkingSetBytes);
                        DrawMemoryRow("Virtual Memory", m_SnapshotCache.VirtualMemorySizeMB, m_SnapshotCache.VirtualMemorySizeBytes);

                        ImGui.EndTable();
                    }

                    ImGui.Separator();
                    ImGui.Text("GC Heap History (MB)");

                    if (ImGui.BeginChild("GCPlotChild", new System.Numerics.Vector2(-1, 130), ImGuiChildFlags.Borders))
                    {
                        if (m_GCMemoryHistory.Count > 0)
                        {
                            float[] gcData = m_GCMemoryHistory.ToArray();
                            float minGC = m_GCMemoryHistory.Min();
                            float maxGC = m_GCMemoryHistory.Max();
                            float plotMax = maxGC * 1.05f;
                            float plotMin = Math.Max(0f, minGC * 0.95f);
                            float avgGC = m_GCMemoryHistory.Average();

                            // Y-Axis Max Label
                            ImGui.TextDisabled($"Max: {plotMax:F2} MB");

                            // X-Axis Overlay Text (Time Labels)
                            string overlayText = $"Past {gcData.Length} Samples";

                            ImGui.PlotLines(
                                $"Avg: {avgGC:F2} MB | Samples: {gcData.Length}",
                                ref gcData[0],
                                gcData.Length,
                                0,
                                overlayText,
                                plotMin,
                                plotMax,
                                new System.Numerics.Vector2(-1, 80)
                            );

                            ImGui.TextDisabled($"Min: {plotMin:F2} MB");
                        }
                        else
                        {
                            ImGui.TextDisabled("Waiting for memory history data...");
                        }

                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }

    private void DrawProfilerTool(IProfiler profiler)
    {
        if (ImGui.Begin("Profiler"))
        {
            if (ImGui.BeginTabBar("ProfileTabs"))
            {
                if (ImGui.BeginTabItem("CPU Profile"))
                {
                    float maxCpuTime = profiler.CpuProfileResults.Values.Max(r => (float?)r.LastTimeMs) ?? 0.0f;
                    ImGui.Text($"Max CPU Scope Time (Current Frame): {maxCpuTime:F2} ms");
                    ImGui.Separator();

                    ImGui.Text("Frame Time History (Max Scope Time in ms)");

                    if (ImGui.BeginChild("CPUPlotChild", new System.Numerics.Vector2(-1, 130), ImGuiChildFlags.Borders))
                    {
                        if (m_FrameTimeHistory.Count > 0)
                        {
                            float[] cpuData = m_FrameTimeHistory.ToArray();
                            const float TargetMS = 16.67f;
                            float maxHistory = m_FrameTimeHistory.Max();
                            float plotMax = Math.Max(maxHistory * 1.1f, TargetMS * 1.2f);
                            float avgCPU = m_FrameTimeHistory.Average();
                            float plotMin = 0f;

                            ImGui.TextDisabled($"Max: {plotMax:F2} ms");

                            string overlayText = $"{TargetMS:F2} ms (60 FPS Target) | Past {cpuData.Length} Samples";

                            ImGui.PlotLines(
                                $"Avg: {avgCPU:F2} ms | Samples: {cpuData.Length}",
                                ref cpuData[0],
                                cpuData.Length,
                                0,
                                overlayText,
                                plotMin,
                                plotMax,
                                new System.Numerics.Vector2(-1, 80)
                            );

                            ImGui.TextDisabled($"Min: {plotMin:F2} ms");
                        }
                        else
                        {
                            ImGui.TextDisabled("Waiting for CPU profile history data...");
                        }

                        ImGui.EndChild();
                    }

                    ImGui.Separator();

                    if (profiler.CpuProfileResults.Count > 0)
                    {
                        ImGui.Text("Current Frame Scope Breakdown:");

                        if (ImGui.BeginTable("CPUScopes", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Sortable))
                        {
                            ImGui.TableSetupColumn("Scope Tag", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Time (%)", ImGuiTableColumnFlags.WidthFixed, 60.0f);
                            ImGui.TableSetupColumn("Time (ms)", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                            ImGui.TableHeadersRow();

                            foreach (var kvp in profiler.CpuProfileResults)
                            {
                                float timeMs = kvp.Value.LastTimeMs;
                                float percentage = (maxCpuTime > 0) ? (timeMs / maxCpuTime) * 100.0f : 0.0f;

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.Text(kvp.Value.Tag);

                                ImGui.TableNextColumn();
                                ImGui.TextDisabled($"{percentage:F1}%");

                                ImGui.TableNextColumn();
                                ImGui.Text($"{timeMs:F3}");
                            }

                            ImGui.EndTable();
                        }
                    }
                    else
                    {
                        ImGui.Text("No CPU profiling data available.");
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Render Profile"))
                {
                    float maxGpuTime = profiler.RenderProfileResults.Values.Max(r => (float?)r.LastTimeMs) ?? 0.0f;
                    ImGui.Text($"Max GPU Scope Time: {maxGpuTime:F2} ms");
                    ImGui.Separator();
                    ImGui.Text("Frame Time History (Max Scope Time in ms)");

                    if (ImGui.BeginChild("GpuPlotChild", new System.Numerics.Vector2(-1, 130), ImGuiChildFlags.Borders))
                    {
                        if (m_RenderTimeHistory.Count > 0)
                        {
                            float[] gpuData = m_RenderTimeHistory.ToArray();
                            const float TargetMS = 16.67f;
                            float maxHistory = m_RenderTimeHistory.Max();
                            float plotMax = Math.Max(maxHistory * 1.1f, TargetMS * 1.2f);
                            float avgCPU = m_RenderTimeHistory.Average();
                            float plotMin = 0f;

                            ImGui.TextDisabled($"Max: {plotMax:F2} ms");

                            string overlayText = $"{TargetMS:F2} ms (60 FPS Target) | Past {gpuData.Length} Samples";

                            ImGui.PlotLines(
                                $"Avg: {avgCPU:F2} ms | Samples: {gpuData.Length}",
                                ref gpuData[0],
                                gpuData.Length,
                                0,
                                overlayText,
                                plotMin,
                                plotMax,
                                new System.Numerics.Vector2(-1, 80)
                            );

                            ImGui.TextDisabled($"Min: {plotMin:F2} ms");
                        }
                        else
                        {
                            ImGui.TextDisabled("Waiting for GPU profile history data...");
                        }

                        ImGui.EndChild();
                    }

                    ImGui.Separator();
                    if (profiler.RenderProfileResults.Count > 0)
                    {
                        ImGui.Text("Current Frame GPU Scope Breakdown:");

                        if (ImGui.BeginTable("GPUScopes", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Sortable))
                        {
                            ImGui.TableSetupColumn("Scope Tag", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Time (%)", ImGuiTableColumnFlags.WidthFixed, 60.0f);
                            ImGui.TableSetupColumn("Time (ms)", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                            ImGui.TableHeadersRow();

                            foreach (var kvp in profiler.RenderProfileResults)
                            {
                                float timeMs = kvp.Value.LastTimeMs;
                                float percentage = (maxGpuTime > 0) ? (timeMs / maxGpuTime) * 100.0f : 0.0f;

                                ImGui.TableNextRow();

                                ImGui.TableNextColumn();
                                ImGui.Text(kvp.Value.Tag);

                                ImGui.TableNextColumn();
                                ImGui.TextDisabled($"{percentage:F1}%");

                                ImGui.TableNextColumn();
                                ImGui.Text($"{timeMs:F3}");
                            }

                            ImGui.EndTable();
                        }
                    }
                    else
                    {
                        ImGui.Text("No GPU profiling data available.");
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }

    public void DrawMenuBar(float fps)
    {
        if (ImGui.BeginMainMenuBar())
        {
            var engineSettings = Service.Get<IEngineSettings>()!;
            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Inspector", "", ref m_EngineStatusWindowOpened))
                {
                }
                if (ImGui.MenuItem("Assets", "", ref m_AssetsWindowOpened))
                {
                }
                if (ImGui.MenuItem("Memory Monitor", "", ref m_MonitorWindowOpened))
                {
                }
                if (ImGui.MenuItem("Profiler", "", ref m_ProfilerWindowOpened))
                {
                }
                if (ImGui.MenuItem("Settings", "", ref m_SettingsWindowOpened))
                {
                }

                ImGui.EndMenu();
            }
            ImGui.Separator();

            string statusText = $"FPS({fps}) {engineSettings.Data.EngineName} - {engineSettings.Data.EngineVersion}({BuildInfo.BuildNumber}) OS: {Environment.OSVersion} ({DateTime.Now:d} {DateTime.Now:t})";
            float menuBarWidth = ImGui.GetWindowWidth();
            float textWidth = ImGui.CalcTextSize(statusText).X;
            ImGui.SetCursorPosX(menuBarWidth - textWidth - ImGui.GetStyle().ItemSpacing.X);
            ImGui.Text(statusText);
            ImGui.EndMainMenuBar();
        }
    }

    /// <summary>
    /// Uses reflection to access the private m_ComponentMap field of a GameObject.
    /// </summary>
    /// <param name="obj">The GameObject instance.</param>
    /// <returns>The private component map, or null if reflection fails.</returns>
    private Dictionary<Type, IComponent>? GetPrivateComponentMap(GameObject obj)
    {
        var mapField = typeof(GameObject).GetField(
            "m_ComponentMap",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance
        );

        if (mapField == null)
        {
            Logger.Log("Failed to find private m_ComponentMap field.", Logger.LogSeverity.Error);
            return null;
        }

        return mapField.GetValue(obj) as Dictionary<Type, IComponent>;
    }

    private void DrawObjectProperties(object targetObject, string label, System.Reflection.MemberInfo? memberInfo = null, object? parentObject = null)
    {
        if (targetObject == null)
        {
            ImGui.TextDisabled($"{label}: (null)");
            return;
        }

        Type targetType = targetObject.GetType();

        if (targetType.IsPrimitive || targetType.IsEnum || targetType == typeof(string) ||
            targetType == typeof(System.Numerics.Vector2) || targetType == typeof(System.Numerics.Vector3))
        {
            bool canWrite = false;
            if (memberInfo is System.Reflection.PropertyInfo property)
            {
                canWrite = property.CanWrite;
            }
            else if (memberInfo is System.Reflection.FieldInfo)
            {
                canWrite = true;
            }

            if (!canWrite || parentObject == null)
            {
                ImGui.Text($"{label}: {targetObject}");
                return;
            }

            if (targetType == typeof(bool))
            {
                bool value = (bool)targetObject;
                if (ImGui.Checkbox(label, ref value))
                {
                    SetValue(memberInfo!, parentObject, value);
                }
            }
            else if (targetType == typeof(int))
            {
                int value = (int)targetObject;
                if (ImGui.InputInt(label, ref value))
                {
                    SetValue(memberInfo!, parentObject, value);
                }
            }
            else if (targetType == typeof(float))
            {
                float value = (float)targetObject;
                if (ImGui.InputFloat(label, ref value))
                {
                    SetValue(memberInfo!, parentObject, value);
                }
            }
            else if (targetType == typeof(System.Numerics.Vector3))
            {
                System.Numerics.Vector3 value = (System.Numerics.Vector3)targetObject;
                if (ImGui.InputFloat3(label, ref value))
                {
                    SetValue(memberInfo!, parentObject, value);
                }
            }
            else if (targetType == typeof(string))
            {
                string value = (string)targetObject;
                if (ImGui.InputText(label, ref value, 256))
                {
                    SetValue(memberInfo!, parentObject, value);
                }
            }
            else
            {
                ImGui.Text($"{label}: {targetObject}");
            }

            return;
        }

        if (ImGui.TreeNode(targetObject.GetHashCode().ToString(), $"{label} ({targetType.Name})"))
        {
            var members = targetType.GetMembers(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance
            );

            foreach (var member in members)
            {
                if (member.Name is "GetType" or "ToString" or "Equals" or "GetHashCode")
                {
                    continue;
                }

                object? value = null;
                Type? valueType = null;
                string memberName = member.Name;

                if (member is System.Reflection.PropertyInfo property)
                {
                    if (property.CanRead)
                    {
                        value = property.GetValue(targetObject);
                        valueType = property.PropertyType;
                    }
                }
                else if (member is System.Reflection.FieldInfo field)
                {
                    value = field.GetValue(targetObject);
                    valueType = field.FieldType;
                }

                if (value != null && valueType != null)
                {
                    ImGui.PushID(member.GetHashCode());

                    DrawObjectProperties(value, memberName, member, targetObject);

                    ImGui.PopID();
                }
            }
            ImGui.TreePop();
        }
    }

    /// <summary>
    /// Sets the value of a property or field using reflection.
    /// </summary>
    private void SetValue(System.Reflection.MemberInfo member, object target, object newValue)
    {
        if (member is System.Reflection.PropertyInfo property)
        {
            if (property.CanWrite)
            {
                try
                {
                    property.SetValue(target, newValue);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to set property '{property.Name}': {ex.Message}", Logger.LogSeverity.Error);
                }
            }
        }
        else if (member is System.Reflection.FieldInfo field)
        {
            try
            {
                field.SetValue(target, newValue);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to set field '{field.Name}': {ex.Message}", Logger.LogSeverity.Error);
            }
        }
    }
}