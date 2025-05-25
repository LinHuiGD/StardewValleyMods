using Fai0.StardewValleyMods.Common;
using GenericModConfigMenu;
using GMCMOptions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Tools;
using System.Collections.Generic;
using System.Linq;
using xTile.Layers;
using xTile.Tiles;
using Pathoschild.Stardew.Common;
using StardewValley.Locations;


namespace Fai0.StardewValleyMods.WaterDepthOverlay;

/// <summary>
/// The mod entry point.
/// </summary>
internal class ModEntry : Mod
{
    // constants
    public const int MaxWaterDistance = 5;
    // default water color, picked from desert
    public Color WaterColor = new Color(0xffd48332);

    // cache data for querying tile property
    private FishingTile[,]? fishingTileSheet;
    private string? currentLocationName = null;

    // config
    private ModConfig config = null!; // set in Entry
    private VaryingFields varyingfields = null!; // set in Entry

    // water tiles for menu UI
    private List<WaterTileData>? waterTileDataList;
    private Dictionary<(string, int), int>? waterTileIndexMapping;

#if DEBUG
    public KeybindList PrintTileInfoKey { get; set; } = KeybindList.Parse("MouseMiddle");
#endif
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        config = Helper.ReadConfig<ModConfig>();
        varyingfields = new VaryingFields();
        if (Constants.TargetPlatform == GamePlatform.Android) config.EnableOverlay = true;
        if (config.Enable)
        {
            Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            Helper.Events.Player.Warped += Player_Warped;
        }
        Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }
    public new void Dispose()
    {
        Helper.Events.GameLoop.GameLaunched -= OnGameLaunched;
        Helper.Events.Input.ButtonPressed -= OnButtonPressed;
        Helper.Events.Player.Warped -= Player_Warped;
        Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        base.Dispose();
    }

    private void SetEnableOverlay(bool enable)
    {
        // it seem that player cant set key binding on mobile phone
        config.EnableOverlay = Constants.TargetPlatform == GamePlatform.Android || enable;
    }


    public void OnEnableChanged(bool enable)
    {
        bool old = config.Enable;
        if (old == enable) return;
        config.Enable = enable;
        if (enable)
        {
            Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            Helper.Events.Player.Warped += Player_Warped;
            GenerateFishingTileSheet(Game1.currentLocation);
        }
        else
        {
            CleanFishingTileSheet();
            Helper.Events.Player.Warped -= Player_Warped;
            Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        }
    }

    private void Player_Warped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        SetEnableOverlay(false);
        CleanFishingTileSheet();
        GenerateFishingTileSheet(e.NewLocation);
    }

    private void GenerateFishingTileSheet(GameLocation? loc)
    {
        if (!Context.IsWorldReady)
            return;
        var locationName = loc?.NameOrUniqueName ?? "";
        // Submarine.canFishHere will return varying value
        // generate property sheet but not display util fishable
        bool canFish = (loc is not null && (loc.canFishHere() || locationName.Equals("Submarine")));
        if (!canFish || !config.Enable)
            return;

        var map = loc!.map;
        currentLocationName = loc.NameOrUniqueName;
        int width = map.Layers[0].LayerWidth, height = map.Layers[0].LayerHeight;
        fishingTileSheet = new FishingTile[width, height];

        FishingTile fTile;
        Layer backLayer = map.RequireLayer("Back");
        waterTileDataList = new List<WaterTileData>();
        waterTileIndexMapping = new Dictionary<(string, int), int>();
        varyingfields.OptionIndex = 0;

        for (int tileX = 0; tileX < width; ++tileX)
        {
            for (int tileY = 0; tileY < height; ++tileY)
            {
                int tileIndex = -1;
                Tile? tile = null;
                fTile = fishingTileSheet[tileX, tileY] = new FishingTile();
                if (loc.isTileFishable(tileX, tileY))
                {
                    fTile.Fishable = true;
                    fTile.WaterDistance = FishingRod.distanceToLand(tileX, tileY, loc);

                    // only water tile on Back layer can be drawn in menu UI
                    // ignore water on Buildings layer or GameLocation.buildings
                    tileIndex = loc.getTileIndexAt(tileX, tileY, "Back");
                    if (tileIndex != -1 && loc.isWaterTile(tileX, tileY) && !waterTileIndexMapping.ContainsKey(("Back", tileIndex)))
                    {
                        tile = backLayer.Tiles[tileX, tileY];
                        waterTileIndexMapping[("Back", tileIndex)] = waterTileDataList.Count;
                        waterTileDataList.Add(new WaterTileData("Back", tile, tileX, tileY));
                    }
                }
                else
                {
                    if (loc.isWaterTile(tileX, tileY)) fTile.IsWater = true;
                    if (loc.doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null) fTile.NoFishing = true;
                    if (loc.hasTileAt(tileX, tileY, "Buildings")) fTile.IsBuilding = true;
                }
            }
        }
#if DEBUG
        foreach (KeyValuePair<(string, int), int> pair in waterTileIndexMapping)
        {
            Monitor.Log($"({pair.Key}: {waterTileDataList[pair.Value].TileIndex})", LogLevel.Debug);
        }

        // print water depth map
        string line;
        Monitor.Log($"Current Location: {currentLocationName}", LogLevel.Debug);
        for (int tileY = 0; tileY < height; ++tileY)
        {
            line = "";
            for (int tileX = 0; tileX < width; ++tileX)
            {
                fTile = fishingTileSheet[tileX, tileY];
                line += fTile + " ";
            }
            Monitor.Log(line, LogLevel.Debug);
        }
#endif
    }

    private void CleanFishingTileSheet()
    {
        fishingTileSheet = null;
        currentLocationName = null;
        waterTileDataList = null;
        waterTileIndexMapping = null;
        varyingfields.OptionIndex = 0;
    }
    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
        if (fishingTileSheet is null)
            return;
        if (!config.EnableOverlay)
            return;
        if (Game1.player.CurrentTool is not FishingRod)
            return;
        var location = Game1.currentLocation;
        var name = location?.NameOrUniqueName;
        if (location is null || name is null || name != currentLocationName)
            return;
        if (location is Submarine && !location.canFishHere())
            return;

        var map = location.map;
        int width = map.Layers[0].LayerWidth, height = map.Layers[0].LayerHeight;
        int clearWaterDistance, colorIndex;
        FishingTile fishingTile;
        var b = Game1.spriteBatch;
        Color? color = null;
        b.End();
        b.Begin(SpriteSortMode.Deferred, DrawHelper.OverlayBlendState, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
        foreach (Vector2 tile in TileHelper.GetVisibleTiles(expand: 1))
        {
            color = null;
            int tileX = (int)tile.X;
            int tileY = (int)tile.Y;
            if (tileX < 0 || tileX >= width || tileY < 0 || tileY >= height) continue;
            fishingTile = fishingTileSheet[tileX, tileY];
            clearWaterDistance = fishingTile.WaterDistance;
            if (!fishingTile.Fishable)
            {
                // This condition must be false: loc.doesTileHaveProperty(tileX, tileY, "Water", "Buildings") != null
                // This condition must be ture: !isWaterTile(tileX, tileY) || doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null || hasTileAt(tileX, tileY, "Buildings")

                // It's not water tile or building water tile, should be land.
                if (!fishingTile.IsWater) continue;
                // This tile has "NoFising" property
                if (fishingTile.NoFishing)
                {
                    if (config.ShouldDrawNoFishingOverlay)
                        color = config.NoFishingOverlayColor;
                }
                // This tile is covered by building
                else if (fishingTile.IsBuilding)
                {
                    if (config.ShouldDrawBuildingOverlay)
                        color = config.BuildingOverlayColor;
                }
            }
            else if (0 <= clearWaterDistance && clearWaterDistance <= MaxWaterDistance)
            {
                colorIndex = System.Math.Min(clearWaterDistance, MaxWaterDistance);
                color = config.DepthOverlayColors[colorIndex];
            }
#if DEBUG
            else
            {
                Monitor.Log($"Invalid water distance detected: locationName={currentLocationName}, tile=({tileX}, {tileY}), waterDistance={clearWaterDistance}", LogLevel.Debug);
            }
#endif
            if (color is not null) b.Draw(DrawHelper.Pixel, new Rectangle((tileX * Game1.tileSize) - Game1.viewport.X, (tileY * Game1.tileSize) - Game1.viewport.Y, Game1.tileSize, Game1.tileSize), (Color)color);
        }
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
    }


    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (config.ToggleEnableOverlayKey.JustPressed())
        {
            config.EnableOverlay = !config.EnableOverlay;
        }
#if DEBUG
        if (PrintTileInfoKey.JustPressed())
        {
            var tile = Game1.currentCursorTile;
            var tileX = (int)tile.X;
            var tileY = (int)tile.Y;
            var loc = Game1.currentLocation;
            var map = loc.map;
            int width = map.Layers[0].LayerWidth, height = map.Layers[0].LayerHeight;
            Monitor.Log($"currentCursorTile: {tile.X}, {tile.Y}", LogLevel.Debug);

            if (0 <= tileX && tileX < width && 0 <= tileY && tileY < height)
            {
                if (fishingTileSheet is not null)
                {
                    FishingTile fishingTile = fishingTileSheet[tileX, tileY];
                    Monitor.Log(DebugHelper.Dump(fishingTile), LogLevel.Debug);
                }

                var tileIndex = loc.getTileIndexAt(tileX, tileY, "Back");
                if (tileIndex >= 0 && waterTileDataList is not null && waterTileIndexMapping is not null && waterTileIndexMapping.ContainsKey(("Back", tileIndex)))
                {
                    Monitor.Log(DebugHelper.Dump(waterTileDataList[waterTileIndexMapping[("Back", tileIndex)]]), LogLevel.Debug);
                }
            }
            else
            {
                Monitor.Log("This tile is outside the map.", LogLevel.Debug);
            }
        }
#endif
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        AddOptionsToGMCM();
    }

    private void ResetConfig()
    {
        config = new ModConfig();
        varyingfields.OptionIndex = 0;
    }

    private void SaveConfig()
    {
        Helper.WriteConfig(config);
    }

    private void DrawWater(SpriteBatch b, Rectangle destRect)
    {
        WaterTileData? waterTileData = GetWaterTileDataSafe();
        if (waterTileData is null || Game1.currentLocation is null)
        {
            //Color[] colors = new Color[1];
            //DrawHelper.Pixel.GetData(colors);
            //Monitor.Log($"{colors[0]}", LogLevel.Debug);
            b.Draw(DrawHelper.Pixel, destRect, WaterColor);
        }
        else
            DrawHelper.DrawWater(b, destRect, waterTileData.Texture, waterTileData.SourceRect);
    }

    private WaterTileData? GetWaterTileDataSafe()
    {
        if (waterTileDataList == null) return null;
        int optionIndex = (int)varyingfields.OptionIndex;
        if (optionIndex < 0 || optionIndex >= waterTileDataList.Count) return null;
        return waterTileDataList[optionIndex];
    }

    public void AddOptionsToGMCM()
    {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        var configMenuExt = Helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");
        if (configMenu is null) return;

        configMenu.Register(
            mod: ModManifest,
            reset: ResetConfig,
            save: SaveConfig
        );

        bool featuresNotSupported = Constants.TargetPlatform == GamePlatform.Android;
        if (!featuresNotSupported)
            configMenu.OnFieldChanged(
                mod: ModManifest,
                onChange: (string str, object obj) =>
                {
                    switch (str)
                    {
                        case "OptionIdx":
                            varyingfields.OptionIndex = (uint)obj;
                            break;
                        default:
                            break;
                    }
                });
        configMenu.AddSectionTitle(ModManifest, I18n.Config_Title_Mainoptions);
        configMenu.AddBoolOption(mod: ModManifest,
            getValue: () => config.Enable,
            setValue: OnEnableChanged,
            name: I18n.Config_Enable_Name
        );
        // TODO of GMCM: Support virtual keyboard input. GenericModConfigMenu.Framework.SpecificModConfigMenu 
        if (!featuresNotSupported)
            configMenu.AddKeybindList(
                mod: ModManifest,
                getValue: () => config.ToggleEnableOverlayKey,
                setValue: value => config.ToggleEnableOverlayKey = value,
                name: I18n.Config_EnableOverlay_Name,
                tooltip: I18n.Config_EnableOverlay_Desc
            );

        // TODO of GMCMOptions: Color and image pickers are not interactive on Android
        if (configMenuExt is null || featuresNotSupported) return;
        configMenu.AddSectionTitle(ModManifest, I18n.Config_Title_Fishable);

        configMenuExt.AddImageOption(
            mod: ModManifest,
            getValue: () => varyingfields.OptionIndex,
            setValue: (v) =>
            {
                // This lambada will only be invoked when the player clicks the Save button
                varyingfields.OptionIndex = v;
            },
            name: I18n.Config_Background_Name,
            tooltip: I18n.Config_Background_Desc,
            getMaxValue: () =>
            {
                if (waterTileDataList is null || waterTileDataList.Count == 0)
                    return 0;
                else
                    return (uint)waterTileDataList.Count - 1;
            },
            maxImageHeight: () => SharedConstant.ColorBoxOuterSize,
            maxImageWidth: () => SharedConstant.ColorBoxOuterSize,
            drawImage: (v, b, pos) =>
            {
                // there is no TextureBox drawn by ImagePickerOption, draw it here
                int left = (int)pos.X;
                int top = (int)pos.Y;
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, SharedConstant.ColorBoxInnerSize, SharedConstant.ColorBoxInnerSize), left, top, SharedConstant.ColorBoxOuterSize, SharedConstant.ColorBoxOuterSize, Color.White, 1f, false);
                Rectangle destRect = new Rectangle(left + SharedConstant.ColorBoxBorder, top + SharedConstant.ColorBoxBorder, SharedConstant.ColorBoxInnerSize, SharedConstant.ColorBoxInnerSize);
                DrawWater(b, destRect);
            },
#if DEBUG
            label: (v) =>
            {
                WaterTileData? wTile = GetWaterTileDataSafe();
                if (wTile is null) return "";
                return $"[{wTile.TileIndex}], ({wTile.TileX}, {wTile.TileY})";
            },
#endif
            arrowLocation: (int)IGMCMOptionsAPI.ImageOptionArrowLocation.Sides,
            labelLocation: (int)IGMCMOptionsAPI.ImageOptionLabelLocation.None,
            fieldId: "OptionIdx"
         );

        // fishable tiles
        foreach (var i in Enumerable.Range(0, config.DepthOverlayColors.Count))
        {
            if (i == 4) continue; // TODO, to support extra water distance from dynamic splash point(fishing bubble)
            var currentI = i;
            configMenuExt.AddColorOption(
                    mod: ModManifest,
                    getValue: () => config.DepthOverlayColors[currentI],
                    setValue: (c) => config.DepthOverlayColors[currentI] = c,
                    name: () => I18n.Config_WaterDepth_Name(currentI.ToString()),
                    tooltip: I18n.Config_WaterDepth_Desc,
                    colorPickerStyle: (uint)IGMCMOptionsAPI.ColorPickerStyle.RGBSliders,
                    drawSample: configMenuExt.MakeColorSwatchDrawer(drawBackground: DrawWater)
                 );
        }
        // unfishable tiles
        configMenu.AddSectionTitle(ModManifest, I18n.Config_Title_Unfishable, I18n.Config_Unfishable_Desc);
        configMenu.AddBoolOption(ModManifest,
            () => config.ShouldDrawBuildingOverlay,
            (b) => config.ShouldDrawBuildingOverlay = b,
            I18n.Config_EnableBuildingOverlay_Name
        );
        configMenuExt.AddColorOption(
            mod: ModManifest,
            getValue: () => config.BuildingOverlayColor,
            setValue: (c) => config.BuildingOverlayColor = c,
            name: I18n.Config_BuildingOverlayColor_Name,
            tooltip: I18n.Config_BuildingOverlayColor_Desc,
            colorPickerStyle: (uint)IGMCMOptionsAPI.ColorPickerStyle.RGBSliders
         );
        configMenu.AddBoolOption(ModManifest,
            () => config.ShouldDrawNoFishingOverlay,
            (b) => config.ShouldDrawNoFishingOverlay = b,
            I18n.Config_EnableNofishingOverlay_Name
        );
        configMenuExt.AddColorOption(
            mod: ModManifest,
            getValue: () => config.NoFishingOverlayColor,
            setValue: (c) => config.NoFishingOverlayColor = c,
            name: I18n.Config_NofishingColor_Name,
            tooltip: I18n.Config_NofishingColor_Desc,
            colorPickerStyle: (uint)IGMCMOptionsAPI.ColorPickerStyle.RGBSliders
         );
    }
}
