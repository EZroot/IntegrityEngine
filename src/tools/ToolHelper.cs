using ImGuiNET;
using Integrity.Objects;
using System.Reflection;
using IComponent = Integrity.Interface.IComponent;

public static class ToolHelper
{
    public static Dictionary<Type, IComponent>? GetPrivateComponentMap(GameObject obj)
    {
        var mapField = typeof(GameObject).GetField(
            "m_ComponentMap",
            BindingFlags.NonPublic |
            BindingFlags.Instance
        );

        if (mapField == null)
        {
            Logger.Log("Failed to find private m_ComponentMap field.", Logger.LogSeverity.Error);
            return null;
        }

        return mapField.GetValue(obj) as Dictionary<Type, IComponent>;
    }

    public static void DrawObjectProperties(object targetObject, string label, System.Reflection.MemberInfo? memberInfo = null, object? parentObject = null)
    {
        if (targetObject == null)
        {
            ImGui.TextDisabled($"{label}: (null)");
            return;
        }

        Type targetType = targetObject.GetType();

        if (targetObject is GameObject && memberInfo == null)
        {
            ImGui.Text($"Name: {((GameObject)targetObject).Name}");
            return;
        }

        // FIX 1: Added Vector4 to the initial list of types to check.
        if (targetType.IsPrimitive || targetType.IsEnum || targetType == typeof(string) ||
            targetType == typeof(System.Numerics.Vector2) || targetType == typeof(System.Numerics.Vector3) || 
            targetType == typeof(System.Numerics.Vector4))
        {
            bool canWrite = false;
            if (memberInfo is PropertyInfo property)
            {
                canWrite = property.CanWrite;
            }
            else if (memberInfo is FieldInfo)
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
            else if (targetType == typeof(System.Numerics.Vector4))
            {
                System.Numerics.Vector4 value = (System.Numerics.Vector4)targetObject;
                if (ImGui.ColorEdit4(label, ref value))
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
            else if (targetType == typeof(System.Numerics.Vector2))
            {
                System.Numerics.Vector2 value = (System.Numerics.Vector2)targetObject;
                if (ImGui.InputFloat2(label, ref value))
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
            else if (targetType.IsEnum)
            {
                ImGui.Text($"{label}: {targetObject}");
            }
            else
            {
                ImGui.Text($"{label}: {targetObject}");
            }

            return;
        }

        string nodeId = memberInfo != null ? memberInfo.GetHashCode().ToString() : targetObject.GetHashCode().ToString();

        if (memberInfo == null && parentObject == null)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Once);
        }

        if (ImGui.TreeNode(nodeId, $"{label} ({targetType.Name})"))
        {
            var members = targetType.GetMembers(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            );

            foreach (var member in members)
            {
                if (member.Name is "GetType" or "ToString" or "Equals" or "GetHashCode" or "Component" ||
                    member.Name.StartsWith("<"))
                {
                    continue;
                }

                object? value = null;
                Type? valueType = null;
                string memberName = member.Name;

                if (member is PropertyInfo property)
                {
                    if (property.IsSpecialName) continue;

                    if (property.CanRead)
                    {
                        value = property.GetValue(targetObject);
                        valueType = property.PropertyType;
                    }
                }
                else if (member is FieldInfo field)
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

    public static void SetValue(System.Reflection.MemberInfo member, object target, object newValue)
    {
        if (member is PropertyInfo property)
        {
            if (property.CanWrite)
            {
                try
                {
                    object? convertedValue = Convert.ChangeType(newValue, property.PropertyType);
                    property.SetValue(target, convertedValue);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to set property '{property.Name}': {ex.Message}", Logger.LogSeverity.Error);
                }
            }
        }
        else if (member is FieldInfo field)
        {
            try
            {
                object? convertedValue = Convert.ChangeType(newValue, field.FieldType);
                field.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to set field '{field.Name}': {ex.Message}", Logger.LogSeverity.Error);
            }
        }
    }
}