using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace Fai0.StardewValleyMods.WaterDepthOverlay;

/// <summary>
/// Used together with GMCMOptions to deal with dynamic changes in fields
/// </summary>
public class VaryingFields
{
    public uint OptionIndex;
}

/// <summary>
/// The raw mod configuration.
/// </summary>
internal class ModConfig
{
    // enable this mod
    public bool Enable{ get; set; } = true;
    // enable overlay
    [Newtonsoft.Json.JsonIgnore]
    public bool EnableOverlay { get; set; } = true; 
    public KeybindList ToggleEnableOverlayKey { get; set; } = KeybindList.Parse("OemQuotes");
    public List<Color> DepthOverlayColors = new List<Color>()
    {
        // distance == 0, 1, 2, 3, 4(+1 from the fishing bubble/splash point), 5(max)
        new Color(0x6f000000),
        new Color(0x8200ff00),
        new Color(0x9bffffff),
        new Color(0xa600bbff),
        new Color(0xffffffff),
        new Color(0x91cc00d6),
    };
    public bool ShouldDrawNoFishingOverlay { get; set; } = true;
    public bool ShouldDrawBuildingOverlay { get; set; } = true;
    // This tile has "NoFising" property, it could be found in Maps/Mountain.tmx, Maps/Beach-NightMarket.tmx
    public Color NoFishingOverlayColor { get; set; } = new Color(0xc90000ff);
    // This tile is covered by building
    public Color BuildingOverlayColor { get; set; } = new Color(0xcb000000);
}
