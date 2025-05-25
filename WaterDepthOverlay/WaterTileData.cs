using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using xTile.Tiles;
using StardewValley;
using xTile.Layers;
namespace Fai0.StardewValleyMods.WaterDepthOverlay;

/// <summary>
/// For drawing water tile on menu UI
/// </summary>
internal class WaterTileData
{
    public string LayerID;
    public int TileIndex;
    public Texture2D Texture; // water background texture
    public Rectangle SourceRect; // source rectangle of water background texture
    public int TileX; // Indicates which water tile this object refers to, required for drawing water animation
    public int TileY;
    public WaterTileData(string layerID, Tile tile, int tileX, int tileY)
    {
        LayerID = layerID;
        TileIndex = tile.TileIndex;
        Texture = Game1.content.Load<Texture2D>(tile.TileSheet.ImageSource);
        var xRect = tile.TileSheet.GetTileImageBounds(TileIndex);
        SourceRect = new Rectangle(xRect.X, xRect.Y, xRect.Width, xRect.Height);
        TileX = tileX;
        TileY = tileY;
    }
    public WaterTileData(string layerID, int tileIndex, Texture2D texture, Rectangle sourceRect, int tileX, int tileY)
    {
        LayerID = layerID;
        TileIndex = tileIndex;
        Texture = texture;
        SourceRect = sourceRect;
        TileX = tileX;
        TileY = tileY;
    }
}
