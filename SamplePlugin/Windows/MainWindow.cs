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
    private readonly string imagePath;

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Image Viewer##ImageViewerWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
        
        // HARDCODED PATH - Change this to your image path!
        // imagePath = @"C:\Users\YourName\Pictures\test.png";
        imagePath = @"C:\Users\apkoh\Desktop\MyImage.png";
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
                var texture = Plugin.TextureProvider.GetFromFile(imagePath).GetWrapOrDefault();
                
                if (texture != null)
                {
                    ImGui.Image(texture.Handle, new Vector2(texture.Width, texture.Height));
                }
                else
                {
                    ImGui.TextUnformatted("Image not found - check the hardcoded path!");
                }
            }
        }
    }
}