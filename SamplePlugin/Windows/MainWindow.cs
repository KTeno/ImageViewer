using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Image Viewer##ImageViewerWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();

        using (var child = ImRaii.Child("ImageArea", Vector2.Zero, true))
        {
            if (child.Success)
            {
                // Get the image path from configuration
                string imagePath = plugin.Configuration.ImagePath;
                
                if (string.IsNullOrEmpty(imagePath))
                {
                    ImGui.TextUnformatted("No image path set.");
                    ImGui.Spacing();
                    ImGui.TextUnformatted("Click 'Show Settings' to set an image path.");
                }
                else
                {
                    var texture = Plugin.TextureProvider.GetFromFile(imagePath).GetWrapOrDefault();
                    
                    if (texture != null)
                    {
                        ImGui.Image(texture.Handle, new Vector2(texture.Width, texture.Height));
                    }
                    else
                    {
                        ImGui.TextUnformatted($"Image not found: {imagePath}");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("Check the path in settings.");
                    }
                }
            }
        }
    }
}