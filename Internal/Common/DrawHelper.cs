using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;

namespace Fai0.StardewValleyMods.Common;

internal static class DrawHelper
{

    public static readonly BlendState OverlayBlendState = new()
    {
        // fix alpha to 1.0f
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
        // same as BlendState.NonPremultiplied
        ColorSourceBlend = Blend.SourceAlpha,
        ColorDestinationBlend = Blend.InverseSourceAlpha
    };

    public static readonly BlendState ColorBlendState = new()
    {
        // fix alpha to 1.0f
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
        // same as BlendState.AlphaBlend
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.InverseSourceAlpha
    };

    // src: https://github.com/Pathoschild/StardewMods/blob/stable/Common/CommonHelper.cs
    public static readonly System.Lazy<Texture2D> LazyPixel = new(() =>
    {
        Texture2D pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
        return pixel;
    });
    public static Texture2D Pixel => LazyPixel.Value;

    public static void AdaptiveDraw(SpriteBatch b, Texture2D srcTex, Rectangle srcRect, Rectangle destRect, Vector2 destOffset, Color? color = null, bool stretch = true, float rotation=0f, SpriteEffects effects=SpriteEffects.None, float layerDepth=-1f)
    {
        // valid range of layerDepth is [0f, 1f]
        // Depth is ignored in Deferred mode, used in BackToFront and FrontToBack modes.
        if (layerDepth < 0f)
        {
            /// <see cref="Menus.IClickableMenu.drawTextureBox"/>
            layerDepth = 0.8f - (float)destRect.Y * 1E-06f;
        }
        float scaleX = 1f * destRect.Width / srcRect.Width;
        float scaleY = 1f * destRect.Height / srcRect.Height;
        Vector2 scale;
        float targetRatio = (float)destRect.Width / destRect.Height;
        float sourceRatio = (float)srcRect.Width / srcRect.Height;
        if (stretch)
            scale = targetRatio > sourceRatio ? new Vector2(scaleX, scaleX) : new Vector2(scaleY, scaleY);
        else 
            scale = new Vector2(scaleX, scaleY);
        destRect.Offset(destOffset);
        // screen position
        b.Draw(srcTex, new Vector2(destRect.X, destRect.Y), srcRect, color ?? Color.White, rotation, Vector2.Zero, scale, effects, layerDepth);
    }

    /// <summary>
    /// Draw water animation in a rectangle.
    /// Reproduce visual effects of GameLocation.drawWaterTile
    /// </summary>
    /// <remarks>
    ///     <see cref="GameLocation.drawWaterTile"/><br/>
    ///     <see cref="VolcanoDungeon.drawWaterTile"/><br/>
    ///     <see cref="Caldera.drawWaterTile"/><br/>
    /// </remarks>
    /// <param name="b">The sprite batch being drawn.</param>
    /// <param name="destRect">Destination area to draw</param>
    public static void DrawWaterAnim(SpriteBatch b, Rectangle destRect)
    {
        GameLocation? loc = Game1.currentLocation;
        if (loc is null) return;
        if (loc is VolcanoDungeon)
        {
            VolcanoDungeon location = (VolcanoDungeon)loc;
            // Dwarfshop in VolcanoDungeon level 5
            if (location.level.Value == 5)
            {
                BaseDrawWaterAnim(b, destRect, loc);
                return;
            }
            // water pool area in VolcanoDungeon level 0
            //if (location.level.Value == 0 && x > 23 && x < 28 && y > 42 && y < 47)
            if (location.level.Value == 0)
            {
                BaseDrawWaterAnim(b, destRect, loc, Color.DeepSkyBlue * 0.8f);
                return;
            }
            DoDrawWaterAnim(b, destRect, location.mapBaseTilesheet, 16, 0, 320, loc, location.waterColor.Value);
        }
        else if (loc is Caldera) 
        {
            Caldera location = (Caldera)loc;
            DoDrawWaterAnim(b, destRect, location.mapBaseTilesheet, 16, 0, 320, loc, location.waterColor.Value);
        }
        else
        {
            BaseDrawWaterAnim(b,destRect, loc);
        }
    }

    private static void BaseDrawWaterAnim(SpriteBatch b, Rectangle destRect, GameLocation loc, Color? color = null)
    {
        DoDrawWaterAnim(b, destRect, Game1.mouseCursors, 64, 0, 2064, loc, color ?? loc.waterColor.Value);
    }

    private static void DoDrawWaterAnim(SpriteBatch b, Rectangle destRect, Texture2D texture, int texSize, int texOffsetX, int texOffsetY, GameLocation loc, Color color)
    {
        var destHeight = destRect.Height;
        // amount of pixels the water moves up
        int waterPosition = (int)System.Math.Round(loc.waterPosition);
        AdaptiveDraw(b, texture,
            new Rectangle(
                texOffsetX + loc.waterAnimationIndex * texSize,
                texOffsetY + ((!loc.waterTileFlip) ? texSize * 2 : 0), // (x + y) % 2 != 0
                texSize,
                texSize),
            destRect, 
            destOffset: new Vector2(0, - waterPosition),
            stretch: false, 
            color: color);

        if (true) // bottomY
        {
            AdaptiveDraw(b, texture,
                new Rectangle(
                    texOffsetX + loc.waterAnimationIndex * texSize,
                    texOffsetY + (loc.waterTileFlip ? texSize * 2 : 0), // (x + (y + 1)) % 2 == 0)
                    texSize,
                    texSize),
                destRect,
                destOffset: new Vector2(0, destHeight - waterPosition),
                stretch: false,
                color: color);
        }
    }

    /// <summary>
    /// Draw water in the destination rectangle, including water textures in the map background and water animation.<br/>
    /// <see cref="Game1.DrawWorld"/>
    /// </summary>
    public static void DrawWater(SpriteBatch b, Rectangle destRect, Texture2D bgTex, Rectangle bgSrcRect, bool enableScissor=true)
    {
        b.End();

        // background
        // using OverlayBlendState instead of AlphaBlend, avoid to blend with rendered world
        // Deferred: in order of draw call sequence
        b.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
        AdaptiveDraw(b, bgTex, bgSrcRect, destRect, Vector2.Zero);
        b.End();

        if (Game1.currentLocation is not null)
        {
            do
            {
                if (Game1.currentLocation.waterTiles is null) break;
                // animation
                Rectangle cachedScissorRect = b.GraphicsDevice.ScissorRectangle;
                if (enableScissor) b.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(destRect, cachedScissorRect);
                b.Begin(SpriteSortMode.Deferred, ColorBlendState, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
                DrawWaterAnim(b, destRect);
                b.End();
                if (enableScissor) b.GraphicsDevice.ScissorRectangle = cachedScissorRect;
            } while (false);

        }
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
    }

    /// <summary>Draw a sprite to the screen.</summary>
    /// <see href="https://github.com/Pathoschild/StardewMods/blob/stable/Common/CommonHelper.cs" />
    /// <param name="batch">The sprite batch.</param>
    /// <param name="x">The X-position at which to start the line.</param>
    /// <param name="y">The X-position at which to start the line.</param>
    /// <param name="size">The line dimensions.</param>
    /// <param name="color">The color to tint the sprite.</param>
    public static void DrawLine(this SpriteBatch batch, float x, float y, Vector2 size, Color? color = null)
    {
        batch.Draw(Pixel, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), color ?? Color.White);
    }

    /// <see href="https://github.com/Pathoschild/StardewMods/blob/stable/Automate/Framework/OverlayMenu.cs" />
    public static void DrawEdgeBorders(SpriteBatch spriteBatch, Vector2 tile, Color color, bool top, bool bottom, bool left, bool right)
    {
        int borderSize = 3;
        float screenX = tile.X * Game1.tileSize - Game1.viewport.X;
        float screenY = tile.Y * Game1.tileSize - Game1.viewport.Y;
        float tileSize = Game1.tileSize;

        // top
        if (top)
            spriteBatch.DrawLine(screenX, screenY, new Vector2(tileSize, borderSize), color);

        // bottom
        if (bottom)
            spriteBatch.DrawLine(screenX, screenY + tileSize, new Vector2(tileSize, borderSize), color);

        // left
        if (left)
            spriteBatch.DrawLine(screenX, screenY, new Vector2(borderSize, tileSize), color);

        // right
        if (right)
            spriteBatch.DrawLine(screenX + tileSize, screenY, new Vector2(borderSize, tileSize), color);
    }

    public static void DrawEdgeBorders(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        int borderSize = 3;

        // top
        spriteBatch.DrawLine(rect.X, rect.Y, new Vector2(rect.Width, borderSize), color);

        // bottom
        spriteBatch.DrawLine(rect.X, rect.Y + rect.Height, new Vector2(rect.Width, borderSize), color);

        // left
        spriteBatch.DrawLine(rect.X, rect.Y, new Vector2(borderSize, rect.Height), color);

        // right
        spriteBatch.DrawLine(rect.X + rect.Width, rect.Y, new Vector2(borderSize, rect.Height), color);
    }
}
