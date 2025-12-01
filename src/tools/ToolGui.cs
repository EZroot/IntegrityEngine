using ImGuiNET;

public class ToolGui
{
    private bool m_IsVsyncEnabled = true;
    private float m_GlobalVolume = 0.5f;
    private System.Numerics.Vector3 m_ClearColor = new(0.1f, 0.1f, 0.15f);
    private bool m_EngineStatusWindowOpened;

    public void DrawTools(ISceneManager sceneManager, ICameraManager cameraManager, IEngineSettings engineSettings)
    {
        if(m_EngineStatusWindowOpened)
        {
            DrawEngineStatusTool(sceneManager, cameraManager, engineSettings);
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
                    ImGui.Text($"Active Game Objects: {sceneManager.CurrentScene?.GetAllGameObjects().Count ?? 0}");
                    ImGui.Text($"Active Sprite Objects: {sceneManager.CurrentScene?.GetAllSpriteObjects().Count ?? 0}");
                    ImGui.Separator();
                    ImGui.Text("Camera:" );
                    if(cameraManager.MainCamera != null)
                    {
                        var camera = cameraManager.MainCamera;
                        if (ImGui.TreeNode(camera.Id.ToString(), $"{camera.Name} ({camera.Id.ToString()[..16]}...)"))
                        {
                            ImGui.Text($"Position: {camera.Position}");
                            ImGui.Text($"Rotation: {camera.Zoom}");

                            ImGui.TreePop(); 
                        }
                    }

                    if(sceneManager.CurrentScene == null)
                    {
                        Logger.Log("Scene is null for some reason!", Logger.LogSeverity.Error);
                        return;
                    }
                    ImGui.Separator();
                    ImGui.Text($"Scene {sceneManager.CurrentScene.Name} ({sceneManager.CurrentScene.Id.ToString()[..16]}...)");
                    ImGui.Separator();
                    ImGui.Text("Scene Objects:");
                    
                    if (sceneManager.CurrentScene != null)
                    {
                        foreach (var obj in sceneManager.CurrentScene.GetAllGameObjects())
                        {
                            if (ImGui.TreeNode(obj.Id.ToString(), $"{obj.Name} ({obj.Id.ToString()[..16]}...)"))
                            {
                                var componentMap = GetPrivateComponentMap(obj);
                                if (componentMap != null)
                                {
                                    ImGui.Text("Components:");
                                    foreach (var kvp in componentMap)
                                    {
                                        var component = kvp.Value;
                                        var componentType = component.GetType();
                                        string componentName = componentType.Name;
                                        ImGui.PushID(component.GetHashCode()); 
                                        if (ImGui.TreeNode(componentName))
                                        {
                                            var members = componentType.GetMembers(
                                                System.Reflection.BindingFlags.Public | 
                                                System.Reflection.BindingFlags.Instance
                                            );
                                            
                                            foreach (var member in members)
                                            {
                                                if (member is System.Reflection.PropertyInfo property)
                                                {
                                                    if (property.CanRead)
                                                    {
                                                        var value = property.GetValue(component);
                                                        ImGui.Text($"  {property.Name}: {value}");
                                                    }
                                                }
                                                else if (member is System.Reflection.FieldInfo field)
                                                {
                                                    var value = field.GetValue(component);
                                                    ImGui.Text($"  {field.Name}: {value}");
                                                }
                                            }
                                            
                                            ImGui.TreePop(); 
                                        }
                                        
                                        ImGui.PopID(); 
                                    }
                                }
                                
                                ImGui.TreePop(); 
                            }
                        }
                    }
                    else
                    {
                        ImGui.Text("No scene loaded.");
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
                    
                    ImGui.Text("Renderer:");
                    if (ImGui.ColorEdit3("Clear Color", ref m_ClearColor))
                    {
                        // m_RenderPipe.SetClearColor(m_ClearColor);
                    }

                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }

    public void DrawMenuBar(ISceneManager sceneManager, ICameraManager cameraManager, IEngineSettings engineSettings)
    {
        if (ImGui.BeginMainMenuBar())
        {
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
}