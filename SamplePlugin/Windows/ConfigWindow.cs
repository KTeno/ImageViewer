using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private List<string> imagePathInputs;
    private string? editingKeybindFor = null;
    private string keybindInputBuffer = string.Empty;

    public ConfigWindow(Plugin plugin) : base("Image Viewer Settings###ImageViewerConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
        
        // Initialize input list from configuration
        if (configuration.ImagePaths == null || configuration.ImagePaths.Count == 0)
        {
            imagePathInputs = new List<string> { string.Empty };
        }
        else
        {
            imagePathInputs = new List<string>(configuration.ImagePaths);
        }
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Image Viewer Settings");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("SettingsTabs"))
        {
            // General Settings Tab
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }

            // Keybinds Tab
            if (ImGui.BeginTabItem("Keybinds"))
            {
                DrawKeybindsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawGeneralTab()
    {
        ImGui.TextUnformatted("Image File Paths:");
        ImGui.Spacing();

        // Display each image path with remove button
        for (int i = 0; i < imagePathInputs.Count; i++)
        {
            ImGui.PushID(i);
            
            ImGui.SetNextItemWidth(400f);
            string currentPath = imagePathInputs[i];
            if (ImGui.InputText($"##imagepath{i}", ref currentPath, 500))
            {
                imagePathInputs[i] = currentPath;
                SavePaths();
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Remove") && imagePathInputs.Count > 1)
            {
                imagePathInputs.RemoveAt(i);
                SavePaths();
                ImGui.PopID();
                break; // Exit loop after removing to avoid index issues
            }
            
            ImGui.PopID();
        }

        ImGui.Spacing();
        
        // Add new path button
        if (ImGui.Button("Add Image Path"))
        {
            imagePathInputs.Add(string.Empty);
            SavePaths();
        }
        
        ImGui.Spacing();
        ImGui.TextUnformatted($"Total images: {imagePathInputs.Count}");
        
        ImGui.Spacing();
        ImGui.TextUnformatted("Example: C:\\Users\\YourName\\Pictures\\image.png");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Image scaling option
        var allowUpscaling = configuration.AllowUpscaling;
        if (ImGui.Checkbox("Allow images to scale larger than native size", ref allowUpscaling))
        {
            configuration.AllowUpscaling = allowUpscaling;
            configuration.Save();
        }
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }
    }

    private void DrawKeybindsTab()
    {
        ImGui.TextUnformatted("Keybind Settings");
        ImGui.Spacing();
        ImGui.TextUnformatted("Examples: a, ctrl+a, shift+z, ctrl+alt+b");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawKeybindRow("Next Image", "KeybindNextImage", configuration.KeybindNextImage);
        DrawKeybindRow("Previous Image", "KeybindPreviousImage", configuration.KeybindPreviousImage);
        DrawKeybindRow("Zoom In", "KeybindZoomIn", configuration.KeybindZoomIn);
        DrawKeybindRow("Zoom Out", "KeybindZoomOut", configuration.KeybindZoomOut);
        DrawKeybindRow("Toggle Window", "KeybindToggleWindow", configuration.KeybindToggleWindow);

        // Show input popup if editing
        if (editingKeybindFor != null)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextUnformatted("Enter keybind (e.g., ctrl+a, shift+z):");
            ImGui.SetNextItemWidth(300f);
            if (ImGui.InputText("##keybindinput", ref keybindInputBuffer, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                // Parse and save the keybind
                string normalizedKeybind = NormalizeKeybind(keybindInputBuffer);
                SetKeybind(editingKeybindFor, normalizedKeybind);
                editingKeybindFor = null;
                keybindInputBuffer = string.Empty;
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Save"))
            {
                string normalizedKeybind = NormalizeKeybind(keybindInputBuffer);
                SetKeybind(editingKeybindFor, normalizedKeybind);
                editingKeybindFor = null;
                keybindInputBuffer = string.Empty;
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                editingKeybindFor = null;
                keybindInputBuffer = string.Empty;
            }
        }
    }

    private void DrawKeybindRow(string label, string keybindId, string currentValue)
    {
        ImGui.TextUnformatted($"{label}:");
        ImGui.SameLine(150);
        
        string displayValue = string.IsNullOrEmpty(currentValue) ? "Not set" : currentValue;
        ImGui.TextUnformatted(displayValue);
        
        ImGui.SameLine(300);
        if (ImGui.Button($"Set##{keybindId}"))
        {
            editingKeybindFor = keybindId;
            keybindInputBuffer = currentValue; // Pre-fill with current value
        }
        
        ImGui.SameLine();
        if (ImGui.Button($"Clear##{keybindId}"))
        {
            SetKeybind(keybindId, string.Empty);
        }
        
        ImGui.Spacing();
    }

    private string NormalizeKeybind(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Split by + and trim each part
        var parts = input.ToLower().Split('+');
        var normalized = new List<string>();

        foreach (var part in parts)
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return string.Join("+", normalized);
    }

    private void SetKeybind(string keybindId, string value)
    {
        switch (keybindId)
        {
            case "KeybindNextImage":
                configuration.KeybindNextImage = value;
                break;
            case "KeybindPreviousImage":
                configuration.KeybindPreviousImage = value;
                break;
            case "KeybindZoomIn":
                configuration.KeybindZoomIn = value;
                break;
            case "KeybindZoomOut":
                configuration.KeybindZoomOut = value;
                break;
            case "KeybindToggleWindow":
                configuration.KeybindToggleWindow = value;
                break;
        }
        configuration.Save();
    }

    private void SavePaths()
    {
        configuration.ImagePaths = new List<string>(imagePathInputs);
        configuration.Save();
    }
}