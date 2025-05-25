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

    public static void AdaptiveDraw(SpriteBatch b, Texture2D srcTex, Rectangle srcRect, Rectangle destRect, Vector2 destOffset, Color? color = null, bool stretch = true, float rotation=0f, SpriteEffects effects=SpriteEffects.None, float layerDepth=1f)
    {
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
	/// Reproduce visual effects of GameLocation.drawWaterTile
	/// </summary>
	/// <remarks>
	///     <see cref="GameLocation.drawWaterTile"/><br/>
	///     <see cref="VolcanoDungeon.drawWaterTile"/><br/>
	///     <see cref="Caldera.drawWaterTile"/><br/>
	/// </remarks>
	/// <param name="b">The sprite batch being drawn.</param>
	/// <param name="x">The tile's X tile position within the grid.</param>
	/// <param name="y">The tile's X tile position within the grid.</param>
	/// <param name="destRect">Destination area to draw</param>
	/// <param name="loc">Used to ,it will be set to Game1.currentLocation by default</param>
	public static void DrawWaterAnim(SpriteBatch b, int x, int y, Rectangle destRect, GameLocation? loc = null)
    {
        loc = loc ?? Game1.currentLocation;
        if (loc is null || loc.waterTiles is null) return;
        if (loc is VolcanoDungeon)
        {
            VolcanoDungeon location = (VolcanoDungeon)loc;
			// Dwarfshop in VolcanoDungeon level 5
			if (location.level.Value == 5)
            {
                BaseDrawWaterAnim(b, x, y, destRect, loc);
                return;
            }
			// water pool area in VolcanoDungeon level 0
			if (location.level.Value == 0 && x > 23 && x < 28 && y > 42 && y < 47)
            {
                BaseDrawWaterAnim(b, x, y, destRect, loc, Color.DeepSkyBlue * 0.8f);
                return;
            }
            DoDrawWaterAnim(b, x, y, destRect, location.mapBaseTilesheet, 16, 0, 320, location.waterColor.Value, loc);
        }
        else if (loc is Caldera) 
        {
            Caldera location = (Caldera)loc;
            DoDrawWaterAnim(b, x, y, destRect, location.mapBaseTilesheet, 16, 0, 320, location.waterColor.Value, loc);
        }
        else
        {
            BaseDrawWaterAnim(b, x, y, destRect, loc);
        }
    }

    private static void BaseDrawWaterAnim(SpriteBatch b, int x, int y, Rectangle destRect, GameLocation loc, Color? color = null)
    {
        DoDrawWaterAnim(b, x, y, destRect, Game1.mouseCursors, 64, 0, 2064, color ?? loc.waterColor.Value, loc);
    }

	private static void DoDrawWaterAnim(SpriteBatch b, int x, int y, Rectangle destRect, Texture2D texture, int texSize, int texOffsetX, int texOffsetY, Color color, GameLocation loc)
    {
        bool bottomY = y == loc.map.Layers[0].LayerHeight - 1 || !loc.waterTiles[x, y + 1];
        bool topY = y == 0 || !loc.waterTiles[x, y - 1];

        var destHeight = destRect.Height;
        // amount of pixels the water moves up
        float waterPosition = loc.waterPosition;
        float scale = 1f * destHeight / texSize;

        AdaptiveDraw(b, texture,
            new Rectangle(
                texOffsetX + loc.waterAnimationIndex * texSize,
                texOffsetY + (((x + y) % 2 != 0) ? ((!loc.waterTileFlip) ? texSize * 2 : 0) : (loc.waterTileFlip ? texSize * 2 : 0)) + (topY ? (int)System.Math.Round(waterPosition / scale) : 0),
                texSize,
                texSize + (topY ? ((int)System.Math.Round(0f - waterPosition / scale)) : 0)),
            destRect, 
            destOffset: new Vector2(0, -(int)System.Math.Round((!topY) ? waterPosition : 0f)), 
            stretch: false, 
            color: color);

        if (true) // bottomY
        {
            AdaptiveDraw(b, texture,
                new Rectangle(
                    texOffsetX + loc.waterAnimationIndex * texSize,
                    texOffsetY + (((x + (y + 1)) % 2 != 0) ? ((!loc.waterTileFlip) ? texSize * 2 : 0) : (loc.waterTileFlip ? texSize * 2 : 0)),
                    texSize,
                    texSize - (int)System.Math.Round(texSize - waterPosition / scale) + 1), // hacked, "- 1" in original code
                destRect,
                destOffset: new Vector2(0, destHeight - (int)System.Math.Round(waterPosition)),
                stretch: false,
                color: color);
        }
    }

	/// <summary>
	/// Draw water in the destination rectangle, including water textures in the map background and water animation.<br/>
	/// <see cref="Game1.DrawWorld"/>
	/// </summary>
	public static void DrawWater(SpriteBatch b, Rectangle destRect, Texture2D bgTex, Rectangle bgSrcRect, int x=-1, int y=-1, bool enableScissor=true)
    {
        b.End();

        // background
        // using OverlayBlendState instead of AlphaBlend, avoid to blend with rendered world
        b.Begin(SpriteSortMode.Texture, BlendState.Opaque, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
        AdaptiveDraw(b, bgTex, bgSrcRect, destRect, Vector2.Zero);
        b.End();

        if (x >= 0 && y >=0)
        {
            // animation
            Rectangle cachedScissorRect = b.GraphicsDevice.ScissorRectangle;
            if (enableScissor) b.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(destRect, cachedScissorRect);
            b.Begin(SpriteSortMode.Deferred, ColorBlendState, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
            DrawWaterAnim(b, x, y, destRect);
            b.End();
            if (enableScissor) b.GraphicsDevice.ScissorRectangle = cachedScissorRect;
        }
        // recover
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });
    }
}
