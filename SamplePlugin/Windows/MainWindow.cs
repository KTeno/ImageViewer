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
    private int currentImageIndex = 0;

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

        using (var child = ImRaii.Child("ImageArea", new Vector2(0, -30), true))
        {
            if (child.Success)
            {
                var imagePath = plugin.Configuration.ImagePaths;
                
                if (imagePath == null || imagePath.Count == 0 || string.IsNullOrEmpty(imagePath[0]))
                {
                    ImGui.TextUnformatted("No image paths set.");
                    ImGui.Spacing();
                    ImGui.TextUnformatted("Click 'Show Settings' to add image paths.");
                }
                else
                {
                    // Ensure currentImageIndex is valid
                    if (currentImageIndex >= imagePath.Count)
                    {
                        currentImageIndex = 0;
                    }
                    
                    string imagePathStr = imagePath[currentImageIndex];
                    
                    if (string.IsNullOrEmpty(imagePathStr))
                    {
                        ImGui.TextUnformatted($"Image {currentImageIndex + 1} has no path set.");
                    }
                    else
                    {
                        var texture = Plugin.TextureProvider.GetFromFile(imagePathStr).GetWrapOrDefault();
                        
                        if (texture != null)
                        {
                            // Get available space in the child window
                            var availableSize = ImGui.GetContentRegionAvail();
                            var imageSize = new Vector2(texture.Width, texture.Height);
                            
                            // Calculate scale to fit window while maintaining aspect ratio
                            float scaleX = availableSize.X / imageSize.X;
                            float scaleY = availableSize.Y / imageSize.Y;
                            float scale = Math.Min(scaleX, scaleY);
                            
                            // Apply upscaling preference
                            if (!plugin.Configuration.AllowUpscaling && scale > 1.0f)
                            {
                                scale = 1.0f; // Cap at native size
                            }
                            
                            var displaySize = imageSize * scale;
                            
                            ImGui.Image(texture.Handle, displaySize);
                            ImGui.Spacing();
                            ImGui.TextUnformatted($"Image {currentImageIndex + 1} of {imagePath.Count}");
                            ImGui.TextUnformatted($"Native: {texture.Width}x{texture.Height} | Display: {(int)displaySize.X}x{(int)displaySize.Y} ({scale * 100:F1}%)");
                        }
                        else
                        {
                            ImGui.TextUnformatted($"Image not found: {imagePathStr}");
                            ImGui.Spacing();
                            ImGui.TextUnformatted("Check the path in settings.");
                        }
                    }
                }
            }
        }

        // Next Image button at the bottom
        var imagePaths = plugin.Configuration.ImagePaths;
        if (imagePaths != null && imagePaths.Count > 1)
        {
            if (ImGui.Button("Next Image"))
            {
                currentImageIndex = (currentImageIndex + 1) % imagePaths.Count;
            }
            
            ImGui.SameLine();
            ImGui.TextUnformatted($"({currentImageIndex + 1}/{imagePaths.Count})");
        }
    }
}