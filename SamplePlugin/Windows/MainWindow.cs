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
        // Check for keybinds
        CheckKeybinds();
        
        // Debug: Show keybind status
        var config = plugin.Configuration;
        if (!string.IsNullOrEmpty(config.KeybindNextImage))
        {
            ImGui.TextUnformatted($"Next Image Keybind: {config.KeybindNextImage}");
            ImGui.TextUnformatted($"Window Focused: {ImGui.IsWindowFocused()}");
        }

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
            ZoomOut();
        }
        
        ImGui.SameLine();
        if (ImGui.Button("+"))
        {
            ZoomIn();
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
        
        // Image navigation buttons
        if (imagePaths != null && imagePaths.Count > 1)
        {
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            
            if (ImGui.Button("Previous Image"))
            {
                PreviousImage();
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Next Image"))
            {
                NextImage();
            }
            
            ImGui.SameLine();
            ImGui.TextUnformatted($"({currentImageIndex + 1}/{imagePaths.Count})");
        }
    }

    private void CheckKeybinds()
    {
        // Only check keybinds when window is focused
        if (!ImGui.IsWindowFocused())
            return;

        var config = plugin.Configuration;

        // Debug: Log what keybinds are set
        if (!string.IsNullOrEmpty(config.KeybindNextImage))
        {
            bool pressed = IsKeybindPressed(config.KeybindNextImage);
            if (pressed)
            {
                Plugin.Log.Information($"Next Image keybind triggered: {config.KeybindNextImage}");
                NextImage();
            }
        }
        
        if (!string.IsNullOrEmpty(config.KeybindPreviousImage) && IsKeybindPressed(config.KeybindPreviousImage))
        {
            Plugin.Log.Information($"Previous Image keybind triggered: {config.KeybindPreviousImage}");
            PreviousImage();
        }
        
        if (!string.IsNullOrEmpty(config.KeybindZoomIn) && IsKeybindPressed(config.KeybindZoomIn))
        {
            Plugin.Log.Information($"Zoom In keybind triggered: {config.KeybindZoomIn}");
            ZoomIn();
        }
        
        if (!string.IsNullOrEmpty(config.KeybindZoomOut) && IsKeybindPressed(config.KeybindZoomOut))
        {
            Plugin.Log.Information($"Zoom Out keybind triggered: {config.KeybindZoomOut}");
            ZoomOut();
        }
        
        if (!string.IsNullOrEmpty(config.KeybindToggleWindow) && IsKeybindPressed(config.KeybindToggleWindow))
        {
            Plugin.Log.Information($"Toggle Window keybind triggered: {config.KeybindToggleWindow}");
            Toggle();
        }
    }

    private bool IsKeybindPressed(string keybind)
    {
        if (string.IsNullOrEmpty(keybind))
            return false;

        // Parse the keybind string
        var parts = keybind.ToLower().Split('+');
        
        bool requiresCtrl = false;
        bool requiresShift = false;
        bool requiresAlt = false;
        string? keyName = null;

        foreach (var part in parts)
        {
            string trimmed = part.Trim();
            if (trimmed == "ctrl")
                requiresCtrl = true;
            else if (trimmed == "shift")
                requiresShift = true;
            else if (trimmed == "alt")
                requiresAlt = true;
            else
                keyName = trimmed;
        }

        if (keyName == null)
        {
            Plugin.Log.Warning($"No key name found in keybind: {keybind}");
            return false;
        }

        // Check modifiers
        bool ctrlPressed = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
        bool shiftPressed = ImGui.IsKeyDown(ImGuiKey.ModShift);
        bool altPressed = ImGui.IsKeyDown(ImGuiKey.ModAlt);

        if (ctrlPressed != requiresCtrl || shiftPressed != requiresShift || altPressed != requiresAlt)
            return false;

        // Check the main key
        ImGuiKey key = ParseKeyName(keyName);
        if (key == ImGuiKey.None)
        {
            Plugin.Log.Warning($"Could not parse key name: {keyName}");
            return false;
        }

        bool keyPressed = ImGui.IsKeyPressed(key);
        
        // Debug logging
        if (keyPressed)
        {
            Plugin.Log.Information($"Key {keyName} pressed! Full keybind: {keybind}");
        }

        return keyPressed;
    }

    private ImGuiKey ParseKeyName(string keyName)
    {
        // Parse letter keys
        if (keyName.Length == 1 && char.IsLetter(keyName[0]))
        {
            char upperKey = char.ToUpper(keyName[0]);
            return ImGuiKey.A + (upperKey - 'A');
        }

        // Parse number keys (0-9)
        if (keyName.Length == 1 && char.IsDigit(keyName[0]))
        {
            // For now, return None since we couldn't get number keys working
            // Can be added later if we figure out the correct enum values
            return ImGuiKey.None;
        }

        return ImGuiKey.None;
    }

    private void NextImage()
    {
        var imagePaths = plugin.Configuration.ImagePaths;
        if (imagePaths != null && imagePaths.Count > 1)
        {
            currentImageIndex = (currentImageIndex + 1) % imagePaths.Count;
            zoomLevel = 1.0f;
            scrollPosition = Vector2.Zero;
            isDragging = false;
        }
    }

    private void PreviousImage()
    {
        var imagePaths = plugin.Configuration.ImagePaths;
        if (imagePaths != null && imagePaths.Count > 1)
        {
            currentImageIndex--;
            if (currentImageIndex < 0)
            {
                currentImageIndex = imagePaths.Count - 1;
            }
            zoomLevel = 1.0f;
            scrollPosition = Vector2.Zero;
            isDragging = false;
        }
    }

    private void ZoomIn()
    {
        zoomLevel += ZoomStep;
        zoomLevel = Math.Min(zoomLevel, MaxZoom);
    }

    private void ZoomOut()
    {
        zoomLevel -= ZoomStep;
        zoomLevel = Math.Max(zoomLevel, MinZoom);
    }
}