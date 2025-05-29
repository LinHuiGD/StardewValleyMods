using GenericModConfigMenu;
using GMCMOptions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Tools;
using StardewValley.Mods;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;
using System.Linq;
using xTile.Layers;
using xTile.Tiles;
using Pathoschild.Stardew.Common;
using Fai0.StardewValleyMods.Common;

namespace Fai0.StardewValleyMods.WaterDepthOverlay;

/// <summary>
/// The mod entry point.
/// </summary>
internal class ModEntry : Mod
{
    // constants
    private const int MaxWaterDistance = 5;

    // default water color, picked from desert
    private Color WaterColor = new Color(0xffd48332);

    // cache data for querying tile property
    private FishingTileSheet? fishingTileSheet = null;
    private string? currentLocationName = null;

    // config
    private ModConfig config = null!; // set in Entry
    private VaryingFields varyingfields = null!; // set in Entry

    // water tiles for menu UI
    private List<WaterTileData>? waterTileDataList;
    private Dictionary<(string, int), int>? waterTileIndexMapping;

#if DEBUG
    private bool enableSplashPointDrawing = false;
    private KeybindList DebugTileKey { get; set; } = KeybindList.Parse("MouseMiddle");
    private KeybindList DebugSplashPointKey { get; set; } = KeybindList.Parse("Home");
#endif
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        config = Helper.ReadConfig<ModConfig>();
        varyingfields = new VaryingFields();
        if (Constants.TargetPlatform == GamePlatform.Android) config.EnableOverlay = true;
        if (config.Enable)
        {
            Helper.Events.Display.RenderingStep += OnRenderingStep;
            Helper.Events.Display.RenderedStep += OnRenderedStep;
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
        Helper.Events.Display.RenderedStep -= OnRenderedStep;
        Helper.Events.Display.RenderingStep -= OnRenderingStep;
        base.Dispose();
    }

    private void SetEnableOverlay(bool enable)
    {
        // it seem that player cant set key binding on mobile phone
        config.EnableOverlay = Constants.TargetPlatform == GamePlatform.Android || enable;
    }


    public void SetEnable(bool enable)
    {
        bool old = config.Enable;
        if (old == enable) return;
        config.Enable = enable;
        if (enable)
        {
            GenerateFishingTileSheet(Game1.currentLocation);
            Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.Display.RenderingStep += OnRenderingStep;
            Helper.Events.Display.RenderedStep += OnRenderedStep;
        }
        else
        {
            CleanFishingTileSheet();
            Helper.Events.Player.Warped -= Player_Warped;
            Helper.Events.Display.RenderingStep -= OnRenderingStep;
            Helper.Events.Display.RenderedStep -= OnRenderedStep;
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

        FishingTile fTile;
        Layer backLayer = map.RequireLayer("Back");
        waterTileDataList = new List<WaterTileData>();
        waterTileIndexMapping = new Dictionary<(string, int), int>();
        varyingfields.OptionIndex = 0;

        fishingTileSheet = new FishingTileSheet(width, height);

        for (int tileX = 0; tileX < width; ++tileX)
        {
            for (int tileY = 0; tileY < height; ++tileY)
            {
                int tileIndex = -1;
                Tile? tile = null;
                fTile = new FishingTile();
                if (loc.isTileFishable(tileX, tileY))
                {
                    fishingTileSheet[tileX, tileY] = fTile;
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
                    fishingTileSheet[tileX, tileY] = fTile;
                    if (loc.isWaterTile(tileX, tileY)) fTile.IsWater = true;
                    if (loc.doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null) fTile.NoFishing = true;
                    if (loc.hasTileAt(tileX, tileY, "Buildings")) fTile.IsBuilding = true;
                }
            }
        }

#if DEBUG
        Monitor.Log($"Current location: {currentLocationName}", LogLevel.Debug);
        // print water depth map
        string line;
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

    private void DrawSplashPointRect(GameLocation location)
    {
        SpriteBatch b = Game1.spriteBatch;
        // draw splash point
        Point splashPoint = location.fishSplashPoint.Value;
        if (!splashPoint.Equals(Point.Zero))
        {
            int splashX = splashPoint.X * Game1.tileSize - Game1.viewport.X;
            int splashY = splashPoint.Y * Game1.tileSize - Game1.viewport.Y;
            // draw rectangle frame of splash point
            DrawHelper.DrawEdgeBorders(b, new Vector2(splashPoint.X, splashPoint.Y), Color.Red, true, true, true, true);
            b.DrawString(Game1.smallFont, "Splash Point", new Vector2(splashX + 4, splashY + 4), Color.Red);
            FishingRod? rod = Game1.player.CurrentTool as FishingRod;
            if (rod is not null)
            {
                // FishingRod.draw
                Vector2 bobber = rod.bobber.Value;
                int bobberScreenX = (int)bobber.X - Game1.viewport.X;
                int bobberScreenY = (int)bobber.Y - Game1.viewport.Y;

                // Reduce the waiting time for the hook
                // FishingRod.timeUntilFishingBite
                DrawHelper.DrawEdgeBorders(b, new Rectangle(bobberScreenX - 32, bobberScreenY - 32, 64, 64), Color.Yellow);
                b.DrawString(Game1.smallFont, "(-32,-32)\nReduce \ntimeUntilFishingBite", new Vector2(bobberScreenX - 32 + 4, bobberScreenY - 32 + 4), Color.Yellow);
                // isNibbling and getFish
                DrawHelper.DrawEdgeBorders(b, new Rectangle(bobberScreenX - 80, bobberScreenY - 80, 64, 64), Color.Blue);
                b.DrawString(Game1.smallFont, "(-80,-80)\nGetFish", new Vector2(bobberScreenX - 80 + 4, bobberScreenY - 80 + 4), Color.Blue);
                //// pullingOutOfWater
                //DrawHelper.DrawEdgeBorders(b, new Rectangle(bobberScreenX - 32, bobberScreenY - 48, 64, 64), Color.DeepPink);
                //b.DrawString(Game1.smallFont, "Bobber(-32, -48)", new Vector2(bobberScreenX - 32 + 4, bobberScreenY - 48 + 4), Color.Green);
                DrawHelper.DrawEdgeBorders(b, new Rectangle(bobberScreenX, bobberScreenY, 4, 4), Color.Black);
                b.DrawString(Game1.smallFont, "(0,0)\nBobber\nOrigin", new Vector2(bobberScreenX + 4, bobberScreenY + 4), Color.Black);
            }
        }
    }

    private void DrawOverlay(GameLocation location)
    {
        if (fishingTileSheet is null)
            return;
        if (!config.EnableOverlay)
            return;
        if (Game1.player.CurrentTool is not FishingRod)
            return;
        if (location is Submarine && !location.canFishHere())
            return;

        SpriteBatch b = Game1.spriteBatch;
        BlendState cachedBlendState = b.GraphicsDevice.BlendState;
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);

        var map = location.map;
        int width = map.Layers[0].LayerWidth, height = map.Layers[0].LayerHeight;
        int clearWaterDistance, colorIndex;
        FishingTile fishingTile;
        Color? color = null;

        foreach (Vector2 tile in TileHelper.GetVisibleTiles(expand: 1))
        {
            color = null;
            int tileX = (int)tile.X;
            int tileY = (int)tile.Y;
            if (tileX < 0 || tileX >= width || tileY < 0 || tileY >= height) continue;
            fishingTile = fishingTileSheet[tileX, tileY];
            if (fishingTile.Fishable)
            {
                clearWaterDistance = fishingTile.WaterDistance;
                if (clearWaterDistance < 0 || clearWaterDistance > MaxWaterDistance) continue;
                colorIndex = System.Math.Min(clearWaterDistance, MaxWaterDistance);
                color = config.DepthOverlayColors[colorIndex];
            }
            else
            {
                // This condition must be false: loc.doesTileHaveProperty(tileX, tileY, "Water", "Buildings") != null
                // This condition must be ture: !isWaterTile(tileX, tileY) || doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null || hasTileAt(tileX, tileY, "Buildings")

                // It's not water tile or building water tile, should be land.
                if (!fishingTile.IsWater) continue;
                // This tile has "NoFishing" property
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
            if (color is not null)
                b.Draw(DrawHelper.Pixel, new Rectangle((tileX * Game1.tileSize) - Game1.viewport.X, (tileY * Game1.tileSize) - Game1.viewport.Y, Game1.tileSize, Game1.tileSize), (Color)color);
        }
        b.End();
        // FrontToBack used in OnRendering RenderSteps.World_Sorted
        SpriteSortMode spriteSortMode = config.DrawOnTop ? SpriteSortMode.Deferred : SpriteSortMode.FrontToBack;
        b.Begin(spriteSortMode, cachedBlendState, SamplerState.PointClamp);
    }

    private void OnRenderingStep(object? sender, RenderingStepEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
        GameLocation location = Game1.currentLocation;
        var name = location?.NameOrUniqueName;
        if (location is null || name is null || name != currentLocationName)
            return;
        if (!config.DrawOnTop && e.Step == RenderSteps.World_Sorted) DrawOverlay(location);
    }

    private void OnRenderedStep(object? sender, RenderedStepEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
        GameLocation location = Game1.currentLocation;
        var name = location?.NameOrUniqueName;
        if (location is null || name is null || name != currentLocationName)
            return;
        if (config.DrawOnTop && e.Step == RenderSteps.World) DrawOverlay(location);
#if DEBUG
        if (enableSplashPointDrawing && e.Step == RenderSteps.World) DrawSplashPointRect(location);
#endif
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (config.ToggleEnableOverlayKey.JustPressed())
        {
            config.EnableOverlay = !config.EnableOverlay;
            Monitor.Log($"EnableOverlay: {config.EnableOverlay}", LogLevel.Debug);
            string msg = config.EnableOverlay ? I18n.Message_EnableOverlay() : I18n.Message_DisableOverlay();
            Game1.addHUDMessage(new HUDMessage($"{ModManifest.Name} {msg}", HUDMessage.newQuest_type));
            // Users are likely to mistakenly think that "enable" is the switch for drawing the overlay.
            if (config.EnableOverlay && !config.Enable) SetEnable(true);
        }
#if DEBUG
        if (DebugSplashPointKey.JustPressed())
        {
            enableSplashPointDrawing = !enableSplashPointDrawing;
            Monitor.Log($"enableSplashPointDrawing: {enableSplashPointDrawing}", LogLevel.Debug);
        }
        if (DebugTileKey.JustPressed())
        {
            var loc = Game1.currentLocation;
            do
            {
                if (loc is null) break;
                var tile = Game1.currentCursorTile;
                var tileX = (int)tile.X;
                var tileY = (int)tile.Y;
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
                    /// <see cref="StardewValley.GameLocation.performTenMinuteUpdate"/>
                    Point splashPoint = loc.fishSplashPoint.Value;
                    // FarmFishing
                    if (loc.isOutdoors.Value && Game1.IsMasterGame && (!(loc is Farm) || Game1.whichFarm == 1))
                    {
                        if (splashPoint.Equals(Point.Zero))
                        {
                            
                            if (!loc.isOpenWater(tileX, tileY) || loc.doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null)
                            {
                                Monitor.Log($"failed to set fishSplashPoint. isNotOpenWater={!loc.isOpenWater(tileX, tileY)}, NoFishing={loc.doesTileHaveProperty(tileX, tileY, "NoFishing", "Back") != null}", LogLevel.Debug);
                                break;
                            }
                            int toLand = FishingRod.distanceToLand(tileX, tileY, loc);
                            if (toLand <= 1 || toLand >= 5)
                            {
                                Monitor.Log($"failed to set fishSplashPoint. distanceToLand={toLand}", LogLevel.Debug);
                                break;
                            }

                            DebugHelper.SetPrivateField(loc, "fishSplashPointTime", Game1.timeOfDay);
                            loc.fishFrenzyFish.Value = "";
                            loc.fishSplashPoint.Value = new Point(tileX, tileY);
                            Monitor.Log($"Set fishSplashPoint to ({tileX}, {tileY})", LogLevel.Debug);
                        }
                        else
                        {
                            DebugHelper.SetPrivateField(loc, "fishSplashPointTime", 0);
                            loc.fishFrenzyFish.Value = "";
                            loc.fishSplashPoint.Value = Point.Zero;
                            Monitor.Log($"Reset fishSplashPoint.", LogLevel.Debug);
                        }
                    }

                }
                else
                {
                    Monitor.Log("This tile is outside the map.", LogLevel.Debug);
                }
            } while (false);

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
            b.Draw(DrawHelper.Pixel, destRect, WaterColor);
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
            setValue: SetEnable,
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
        configMenu.AddBoolOption(mod: ModManifest,
            getValue: () => config.DrawOnTop,
            setValue: (value) => config.DrawOnTop = value,
            name: I18n.Config_DrawOverlayOnTop_Name
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
