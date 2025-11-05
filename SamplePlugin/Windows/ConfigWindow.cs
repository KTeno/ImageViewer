using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private string imagePathInput;

    public ConfigWindow(Plugin plugin) : base("Image Viewer Settings###ImageViewerConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(500, 150);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
        imagePathInput = configuration.ImagePath;
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

        ImGui.TextUnformatted("Image File Path:");
        ImGui.SetNextItemWidth(450f);
        if (ImGui.InputText("##imagepath", ref imagePathInput, 500))
        {
            configuration.ImagePath = imagePathInput;
            configuration.Save();
        }
        
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
}