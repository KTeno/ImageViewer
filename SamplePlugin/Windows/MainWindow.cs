using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
//using ImGuiNET;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private int currentImageIndex = 0;
    private float zoomLevel = 1.0f;
    private Vector2 scrollPosition = Vector2.Zero;
    private bool isDragging = false;
    private Vector2 dragStartMousePos = Vector2.Zero;
    private Vector2 dragStartScrollPos = Vector2.Zero;
    private const float ZoomStep = 0.25f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 10.0f;
    private const float PanStep = 50.0f;

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

        // Handle arrow key panning when window is focused
        if (ImGui.IsWindowFocused() && zoomLevel > 1.0f)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
            {
                scrollPosition.X -= PanStep;
            }
            if (ImGui.IsKeyPressed(ImGuiKey.RightArrow))
            {
                scrollPosition.X += PanStep;
            }
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
            {
                scrollPosition.Y -= PanStep;
            }
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            {
                scrollPosition.Y += PanStep;
            }
        }

        using (var child = ImRaii.Child("ImageArea", new Vector2(0, -30), true, ImGuiWindowFlags.HorizontalScrollbar))
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
                            float baseFitScale = Math.Min(scaleX, scaleY);
                            
                            // Apply upscaling preference for base fit
                            if (!plugin.Configuration.AllowUpscaling && baseFitScale > 1.0f)
                            {
                                baseFitScale = 1.0f; // Cap at native size
                            }
                            
                            // Apply zoom on top of the base fit scale
                            float finalScale = baseFitScale * zoomLevel;
                            var displaySize = imageSize * finalScale;
                            
                            // Handle mouse wheel zoom when hovering over the child window
                            if (ImGui.IsWindowHovered())
                            {
                                float wheel = ImGui.GetIO().MouseWheel;
                                if (wheel != 0)
                                {
                                    zoomLevel += wheel * ZoomStep;
                                    zoomLevel = Math.Clamp(zoomLevel, MinZoom, MaxZoom);
                                }
                                
                                // Handle right-click drag panning when zoomed
                                if (zoomLevel > 1.0f)
                                {
                                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                                    {
                                        isDragging = true;
                                        dragStartMousePos = ImGui.GetMousePos();
                                        dragStartScrollPos = scrollPosition;
                                    }
                                }
                            }
                            
                            // Update dragging
                            if (isDragging)
                            {
                                if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                                {
                                    var currentMousePos = ImGui.GetMousePos();
                                    var mouseDelta = dragStartMousePos - currentMousePos;
                                    scrollPosition = dragStartScrollPos + mouseDelta;
                                }
                                else
                                {
                                    isDragging = false;
                                }
                            }
                            
                            // Apply scroll position if zoomed
                            if (zoomLevel > 1.0f)
                            {
                                ImGui.SetScrollX(scrollPosition.X);
                                ImGui.SetScrollY(scrollPosition.Y);
                                
                                // Update scroll position to actual scroll (for clamping)
                                scrollPosition.X = ImGui.GetScrollX();
                                scrollPosition.Y = ImGui.GetScrollY();
                            }
                            else
                            {
                                // Reset scroll when not zoomed
                                scrollPosition = Vector2.Zero;
                                isDragging = false;
                            }
                            
                            ImGui.Image(texture.Handle, displaySize);
                            
                            ImGui.Spacing();
                            ImGui.TextUnformatted($"Image {currentImageIndex + 1} of {imagePath.Count}");
                            ImGui.TextUnformatted($"Native: {texture.Width}x{texture.Height} | Display: {(int)displaySize.X}x{(int)displaySize.Y}");
                            ImGui.TextUnformatted($"Zoom: {zoomLevel * 100:F0}%" + (zoomLevel > 1.0f ? " (Right-click drag or arrow keys to pan)" : ""));
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

        // Bottom controls
        var imagePaths = plugin.Configuration.ImagePaths;
        
        // Zoom controls
        if (ImGui.Button("-"))
        {
            zoomLevel -= ZoomStep;
            zoomLevel = Math.Max(zoomLevel, MinZoom);
        }
        
        ImGui.SameLine();
        if (ImGui.Button("+"))
        {
            zoomLevel += ZoomStep;
            zoomLevel = Math.Min(zoomLevel, MaxZoom);
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Reset Zoom"))
        {
            zoomLevel = 1.0f;
            scrollPosition = Vector2.Zero;
            isDragging = false;
        }
        
        ImGui.SameLine();
        ImGui.TextUnformatted($"{zoomLevel * 100:F0}%");
        
        // Next image button
        if (imagePaths != null && imagePaths.Count > 1)
        {
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            
            if (ImGui.Button("Next Image"))
            {
                currentImageIndex = (currentImageIndex + 1) % imagePaths.Count;
                zoomLevel = 1.0f;
                scrollPosition = Vector2.Zero;
                isDragging = false;
            }
            
            ImGui.SameLine();
            ImGui.TextUnformatted($"({currentImageIndex + 1}/{imagePaths.Count})");
        }
    }
}