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

    public ConfigWindow(Plugin plugin) : base("Image Viewer Settings###ImageViewerConfig")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 150),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        configuration = plugin.Configuration;
        
        // Initialize input list from configuration
        imagePathInputs = new List<string>(configuration.ImagePaths);
        
        // Ensure at least one entry exists
        if (imagePathInputs.Count == 0)
        {
            imagePathInputs.Add(string.Empty);
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

        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            configuration.Save();
        }
    }

    private void SavePaths()
    {
        configuration.ImagePaths = new List<string>(imagePathInputs);
        configuration.Save();
    }
}