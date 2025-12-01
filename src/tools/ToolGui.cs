using System.Numerics;
using ImGuiNET;

public class ToolGui
{
    private const float MEMORY_UPDATE_INTERVAL = 1.0f;
    private float m_TimeSinceLastMemoryUpdate = 0.0f;
    private bool m_CaptureMemory;
    private MemoryMonitor.MemorySnapshot m_SnapshotCache;

    private bool m_IsVsyncEnabled = true;
    private float m_GlobalVolume = 0.5f;
    private System.Numerics.Vector3 m_ClearColor = new(0.1f, 0.1f, 0.15f);
    private bool m_EngineStatusWindowOpened;

    public void DrawToolsUpdate(float deltaTime)
    {
        if (!m_CaptureMemory) return;
        m_TimeSinceLastMemoryUpdate += deltaTime;
        if (m_TimeSinceLastMemoryUpdate >= MEMORY_UPDATE_INTERVAL)
        {
            m_SnapshotCache = MemoryMonitor.LogMemoryUsage();

            m_TimeSinceLastMemoryUpdate -= MEMORY_UPDATE_INTERVAL;
        }
    }

    public void DrawTools()
    {
        if (m_EngineStatusWindowOpened)
        {
            DrawEngineStatusTool(Service.Get<ISceneManager>()!, Service.Get<ICameraManager>()!, Service.Get<IEngineSettings>()!);
        }
    }

    private void DrawEngineStatusTool(ISceneManager sceneManager, ICameraManager cameraManager, IEngineSettings engineSettings)
    {
        if (ImGui.Begin("Engine Status & Debug"))
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
                        return;
                    }

                    ImGui.Text("Current Scene Details:");

                    if (ImGui.BeginTable("SceneInfoTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
                    {
                        ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 150.0f);
                        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableHeadersRow();
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("Name");
                        ImGui.TableSetColumnIndex(1); ImGui.TextColored(new Vector4(0.8f, 1.0f, 0.8f, 1.0f), $"{currentScene.Name}");

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("ID");
                        ImGui.TableSetColumnIndex(1); ImGui.TextDisabled($"{currentScene.Id}");

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Separator();
                        ImGui.TableSetColumnIndex(1); ImGui.Separator();

                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0); ImGui.Text("Total Game Objects");
                        ImGui.TableSetColumnIndex(1); ImGui.Text($"{currentScene.GetAllGameObjects().Count}");

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
                            if (ImGui.TreeNodeEx("CameraDetails", ImGuiTreeNodeFlags.Bullet, $"Camera: {camera.Name}"))
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
                        if (ImGui.BeginChild("SceneObjectList", new Vector2(0, -1)))
                        {
                            foreach (var obj in currentScene.GetAllGameObjects())
                            {
                                if (ImGui.TreeNode(obj.Id.ToString(), $"{obj.Name} ({obj.Id.ToString()[..8]}...)"))
                                {
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
                            }
                            ImGui.EndChild();
                        }
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Assets"))
                {
                    var loadedAssets = Service.Get<IAssetManager>()!.GetLoadedAssets();

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

                            var glTexture = Service.Get<IAssetManager>()!.GetTexture(path);
                            bool isTexture = assetInfo.Type.Equals("ImageData", StringComparison.OrdinalIgnoreCase) && glTexture.TextureId != IntPtr.Zero;

                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ImGui.PushID(path);

                            ImGuiTreeNodeFlags flags = isTexture ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;

                            if (ImGui.TreeNodeEx(path, flags, path))
                            {
                                ImGui.Text($"Full Path: {path}");
                                ImGui.Text($"Bytes: {assetInfo.MemoryFootprintBytes:N0}");
                                if (isTexture)
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
                                    ImGui.Image((nint)glTexture.TextureId, previewSize);
                                    ImGui.Text($"Dimensions: {glTexture.Width}x{glTexture.Height}");
                                }
                                if (isTexture)
                                {
                                    ImGui.TreePop();
                                }
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

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Config"))
                {
                    ImGui.Text("Graphics Settings:");

                    if (ImGui.Checkbox("Enable V-Sync", ref m_IsVsyncEnabled))
                    {
                        // m_RenderPipe.SetVsync(m_IsVsyncEnabled);
                    }

                    ImGui.Spacing();

                    ImGui.Text("Audio Settings:");
                    if (ImGui.SliderFloat("Global Volume", ref m_GlobalVolume, 0.0f, 1.0f))
                    {
                        // m_AudioManager.SetGlobalVolume(m_GlobalVolume);
                    }

                    ImGui.Spacing();
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

                if (ImGui.BeginTabItem("Monitor"))
                {
                    ImGui.Text("Memory Monitor:");

                    if (ImGui.Button("Force GC Collection"))
                    {
                        GC.Collect();
                        Logger.Log("Garbage Collection FORCED!", Logger.LogSeverity.Warning);
                    }

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
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }

    public void DrawMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            var engineSettings = Service.Get<IEngineSettings>()!;
            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Engine Status", "", ref m_EngineStatusWindowOpened))
                {
                }

                ImGui.EndMenu();
            }
            ImGui.Separator();

            string statusText = $"{engineSettings.Data.EngineName} - {engineSettings.Data.EngineVersion}({BuildInfo.BuildNumber}) OS: {Environment.OSVersion} ({DateTime.Now:d} {DateTime.Now:t})";
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