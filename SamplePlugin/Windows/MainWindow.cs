using System;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
// using Dalamud.Bindings.ImGui; // Keep this line commented or removed
using Dalamud.Interface; // <--- NEW: This replaces Dalamud.Interface.Internal and fixes the IDalamudTextureWrap error
using Dalamud.Interface.Textures; // <-- this contains IDalamudTextureWrap
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET; // <--- KEPT: This is correct for the rest of your ImGui calls

namespace SamplePlugin.Windows;
// ... rest of the file

public class MainWindow : Window, IDisposable
{
    // private readonly string goatImagePath; // <--- REMOVED
    private readonly Plugin plugin;
    private static readonly HttpClient httpClient = new();

    // Image viewer state
    private string imagePathInput = string.Empty;
    private string imageUrlInput = string.Empty;
    private IDalamudTextureWrap? currentImage = null;
    private string loadStatus = string.Empty;
    private bool isLoading = false;

    // MainWindow is now the centralized place to hold the loaded image.
    // The ImageService (to be created) will likely load and return this.

    public MainWindow(Plugin plugin) // <--- CHANGED: Removed goatImagePath parameter
        : base("Image Viewer##ImageViewerMain", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        // this.goatImagePath = goatImagePath; // <--- REMOVED
        this.plugin = plugin;

        // Load last image if remember setting is enabled
        if (plugin.Configuration.RememberLastImage)
        {
            imagePathInput = plugin.Configuration.LastImagePath;
            imageUrlInput = plugin.Configuration.LastImageUrl;
            
            // Immediately try to load the last used URL or Path (URL takes precedence)
            if (!string.IsNullOrWhiteSpace(imageUrlInput))
            {
                _ = LoadImageFromUrlAsync(imageUrlInput, saveOnSuccess: false);
            }
            else if (!string.IsNullOrWhiteSpace(imagePathInput))
            {
                LoadImageFromFile(imagePathInput, saveOnSuccess: false);
            }
        }
    }

    public void Dispose()
    {
        currentImage?.Dispose();
    }

    public override void Draw()
    {
        // Image loading controls
        // --- FILE PATH LOAD ---
        ImGui.TextUnformatted("Load Image from File:");
        ImGui.SetNextItemWidth(350f);
        ImGui.InputText("##filepath", ref imagePathInput, 500); // <--- SIMPLIFIED: Removed direct config save on change
        
        ImGui.SameLine();
        if (ImGui.Button("Load File") && !isLoading)
        {
            LoadImageFromFile(imagePathInput, saveOnSuccess: true); // <--- ADDED saveOnSuccess parameter
        }

        ImGui.Spacing();
        // --- URL LOAD ---
        ImGui.TextUnformatted("Load Image from URL:");
        ImGui.SetNextItemWidth(350f);
        ImGui.InputText("##url", ref imageUrlInput, 1000); // <--- SIMPLIFIED: Removed direct config save on change
        
        ImGui.SameLine();
        if (ImGui.Button("Load URL") && !isLoading)
        {
            _ = LoadImageFromUrlAsync(imageUrlInput, saveOnSuccess: true); // <--- ADDED saveOnSuccess parameter
        }

        ImGui.Spacing();

        // Clear button
        if (ImGui.Button("Clear Image"))
        {
            ClearImage();
        }

        ImGui.SameLine();
        
        // Settings button
        if (ImGui.Button("Settings"))
        {
            plugin.ToggleConfigUi();
        }
        
        // --- KEYBIND SUPPORT SETUP (Placeholder) ---
        ImGui.SameLine();
        if (ImGui.Button("Set Next Image Keybind (Coming Soon!)") && !isLoading)
        {
            // Placeholder for future keybind logic
            loadStatus = "Keybind feature is next!";
        }
        // ------------------------------------------

        // Status message
        if (!string.IsNullOrEmpty(loadStatus))
        {
            ImGui.SameLine();
            var statusColor = new Vector4(1f, 1f, 1f, 1f); // White default
            if (isLoading)
            {
                statusColor = new Vector4(1f, 1f, 0f, 1f); // Yellow for loading
            }
            else if (loadStatus.Contains("Error") || loadStatus.Contains("Failed"))
            {
                statusColor = new Vector4(1f, 0f, 0f, 1f); // Red for error
            }
            else if (loadStatus.Contains("Loaded successfully") || loadStatus.Contains("cleared"))
            {
                statusColor = new Vector4(0f, 1f, 0f, 1f); // Green for success/clear
            }
            
            ImGui.TextColored(statusColor, loadStatus); // <--- SIMPLIFIED color logic
        }

        ImGui.Separator();
        ImGui.Spacing();

        // Image display area with scrolling
        // The rest of Draw() remains mostly the same, ensuring aspect ratio display is intact.
        using (var child = ImRaii.Child("ImageDisplay", Vector2.Zero, true))
        {
            if (child.Success)
            {
                if (currentImage != null)
                {
                    // Calculate scaling to fit window while maintaining aspect ratio
                    var availableSize = ImGui.GetContentRegionAvail();
                    var imageSize = new Vector2(currentImage.Width, currentImage.Height);
                    
                    // Scale to fit window
                    var scale = Math.Min(
                        availableSize.X / imageSize.X,
                        availableSize.Y / imageSize.Y
                    );
                    
                    // Don't scale up, only down
                    scale = Math.Min(scale, 1.0f);
                    
                    var displaySize = imageSize * scale;

                    ImGui.Image(currentImage.Handle, displaySize);
                    
                    ImGui.Spacing();
                    ImGui.TextUnformatted($"Image Size: {currentImage.Width}x{currentImage.Height}");
                    ImGui.TextUnformatted($"Display Size: {(int)displaySize.X}x{(int)displaySize.Y} ({scale * 100:F1}%)");
                }
                else
                {
                    ImGui.TextUnformatted("No image loaded.");
                    ImGui.Spacing();
                    ImGui.TextUnformatted("Load an image from a file path or URL above.");
                    
                    ImGuiHelpers.ScaledDummy(10.0f);
                    ImGui.TextUnformatted("Example file path:");
                    ImGui.TextUnformatted("  C:\\Users\\YourName\\Pictures\\image.png");
                    
                    ImGuiHelpers.ScaledDummy(5.0f);
                    ImGui.TextUnformatted("Example URL:");
                    ImGui.TextUnformatted("  https://example.com/image.png");
                }
            }
        }
    }

    // --- REFACTORED LOAD METHODS TO INCLUDE saveOnSuccess ---
    private void LoadImageFromFile(string path, bool saveOnSuccess)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                loadStatus = "Error: Path is empty";
                return;
            }

            if (!File.Exists(path))
            {
                loadStatus = $"Error: File not found - {path}";
                return;
            }

            loadStatus = "Loading...";
            isLoading = true;

            // Dispose previous image
            currentImage?.Dispose();
            currentImage = null;

            // Load new image
            currentImage = Plugin.TextureProvider.GetFromFile(path).GetWrapOrDefault();

            if (currentImage != null)
            {
                loadStatus = "Loaded successfully!";
                Plugin.Log.Information($"Loaded image from file: {path}");

                // Save only on successful load if requested
                if (saveOnSuccess && plugin.Configuration.RememberLastImage)
                {
                    plugin.Configuration.LastImagePath = path;
                    plugin.Configuration.LastImageUrl = string.Empty; // Clear URL on file load
                    plugin.Configuration.Save();
                }
            }
            else
            {
                loadStatus = "Error: Failed to load image";
            }
        }
        catch (Exception ex)
        {
            loadStatus = $"Error: {ex.Message}";
            Plugin.Log.Error($"Failed to load image from file: {ex}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadImageFromUrlAsync(string url, bool saveOnSuccess)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                loadStatus = "Error: URL is empty";
                return;
            }

            loadStatus = "Downloading...";
            isLoading = true;

            // Download image
            var imageData = await httpClient.GetByteArrayAsync(url);

            loadStatus = "Loading...";

            // Dispose previous image
            currentImage?.Dispose();
            currentImage = null;

            // Load image from bytes
            // Note: .Result on an async method's return value is generally discouraged, 
            // but necessary here because GetFromImageAsync returns Task<IDalamudTextureWrap>
            // and we need to block briefly to get the result before the UI thread continues.
            // Using .Result inside an outer async Task is a common pattern in Dalamud plugins 
            // when loading textures from bytes.
            currentImage = Plugin.TextureProvider.GetFromImageAsync(imageData).Result?.GetWrapOrDefault();

            if (currentImage != null)
            {
                loadStatus = "Loaded successfully!";
                Plugin.Log.Information($"Loaded image from URL: {url}");

                // Save only on successful load if requested
                if (saveOnSuccess && plugin.Configuration.RememberLastImage)
                {
                    plugin.Configuration.LastImageUrl = url;
                    plugin.Configuration.LastImagePath = string.Empty; // Clear path on URL load
                    plugin.Configuration.Save();
                }
            }
            else
            {
                loadStatus = "Error: Failed to load image";
            }
        }
        catch (HttpRequestException ex)
        {
            loadStatus = $"Error: Failed to download - {ex.Message}";
            Plugin.Log.Error($"Failed to download image from URL: {ex}");
        }
        catch (Exception ex)
        {
            loadStatus = $"Error: {ex.Message}";
            Plugin.Log.Error($"Failed to load image from URL: {ex}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ClearImage()
    {
        currentImage?.Dispose();
        currentImage = null;
        loadStatus = "Image cleared";
        
        // Clear last used URL/path from config
        if (plugin.Configuration.RememberLastImage)
        {
            plugin.Configuration.LastImagePath = string.Empty;
            plugin.Configuration.LastImageUrl = string.Empty;
            plugin.Configuration.Save();
        }
    }
}