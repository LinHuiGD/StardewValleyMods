namespace Fai0.StardewValleyMods.WaterDepthOverlay;

/// <summary>
/// cache tile properties about fishing
/// </summary>
internal class FishingTile
{
    public int WaterDistance = 0;
	public bool Fishable = false;
    public bool IsWater = false;
    public bool NoFishing = false;
    public bool IsBuilding = false;
#if DEBUG
    public override string ToString()
    {
        if (!Fishable)
        {
            if (!IsWater) return "#";
            if (NoFishing) return "X";
            if (IsBuilding) return "B";
            return "?";
        }
        else
        {
            return WaterDistance.ToString();
        }
    }
#endif
}