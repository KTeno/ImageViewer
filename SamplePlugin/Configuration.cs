using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    
    // Image paths list
    public List<string> ImagePaths { get; set; } = new List<string>();
    
    // Image scaling option
    public bool AllowUpscaling { get; set; } = false;
    
    // Keybinds (stored as string to support modifiers like "ctrl+a", "shift+1", etc.)
    public string KeybindNextImage { get; set; } = string.Empty;
    public string KeybindPreviousImage { get; set; } = string.Empty;
    public string KeybindZoomIn { get; set; } = string.Empty;
    public string KeybindZoomOut { get; set; } = string.Empty;
    public string KeybindToggleWindow { get; set; } = string.Empty;

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}