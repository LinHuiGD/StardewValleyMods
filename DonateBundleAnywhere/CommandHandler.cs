using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere;

internal class CommandHandler
{
	private readonly IMonitor Monitor = null!;
	private readonly IModHelper Helper = null!;
	public CommandHandler(IMonitor monitor, IModHelper helper)
	{
		Monitor = monitor;
		Helper = helper;
		Helper.ConsoleCommands.Add("dba", "TODO Usage", this.OnCommand);
	}

	private void OnCommand(string command, string[] args)
	{
		string subCMD = args[0];
		int baseIndex = 1;
		if (subCMD.Equals("area-items") || subCMD.Equals("a"))
		{
			int areaIndex = ArgUtility.GetInt(args, baseIndex, 1);
			if (CommunityCenter.AREA_Pantry > areaIndex || areaIndex > CommunityCenter.AREA_AbandonedJojaMart)
			{
				Monitor.Log($"area index should in range({CommunityCenter.AREA_Pantry}, {CommunityCenter.AREA_AbandonedJojaMart})", LogLevel.Error);
				return;
			}
			string areaName = CommunityCenter.getAreaNameFromNumber(areaIndex);

			CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			Dictionary<string, string> bundlesInfo = Game1.netWorldState.Value.BundleData; // DataLoader.Bundles(Game1.content)
			Dictionary<int, bool[]> bundlesComplete = cc.bundlesDict();
			List<Bundle> bundles = [];
			foreach (string k in bundlesInfo.Keys)
			{
				if (k.Contains(areaName))
				{
					int bundleIndex = Convert.ToInt32(k.Split('/')[1]);
					bundles.Add(new Bundle(bundleIndex, bundlesInfo[k], bundlesComplete[bundleIndex], Point.Zero, "LooseSprites\\JunimoNote", null));
				}
			}
			foreach (Bundle bundle in bundles)
			{
				foreach (BundleIngredientDescription ingredient in bundle.ingredients)
				{
					Game1.player.addItemByMenuIfNecessary(CreateItem(ingredient));
				}
			}
		}
		else if (subCMD.Equals("bundle-items") || subCMD.Equals("b"))
		{
			int bundleIndex = ArgUtility.GetInt(args, baseIndex, 1);
			if (0 > bundleIndex || bundleIndex > 36)
			{
				Monitor.Log($"bundle index should in range(0, 36), check Data/Bundles.json", LogLevel.Error);
				return;
			}

			CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			Dictionary<string, string> bundlesInfo = Game1.netWorldState.Value.BundleData; // DataLoader.Bundles(Game1.content)
			Dictionary<int, bool[]> bundlesComplete = cc.bundlesDict();
			Bundle? bundle = null;
			foreach (string k in bundlesInfo.Keys)
			{
				int _bundleIndex = Convert.ToInt32(k.Split('/')[1]);
				if (_bundleIndex == bundleIndex)
				{
					bundle = new Bundle(bundleIndex, bundlesInfo[k], bundlesComplete[bundleIndex], Point.Zero, "LooseSprites\\JunimoNote", null);
					break;
				}
			}
			if (bundle != null)
			{
				foreach (BundleIngredientDescription ingredient in bundle.ingredients)
				{
					Game1.player.addItemByMenuIfNecessary(CreateItem(ingredient));
				}
			}
		}
		else if (subCMD.Equals("raccoon-items") || subCMD.Equals("r"))
		{
			bool interim = Game1.netWorldState.Value.Date.TotalDays - Game1.netWorldState.Value.DaysPlayedWhenLastRaccoonBundleWasFinished < 7;
			int whichDialogue = Game1.netWorldState.Value.TimesFedRaccoons;
			if (whichDialogue == 0)
			{
				interim = false;
			}
			if (interim)
			{
				Monitor.Log($"{Game1.content.LoadString("Strings\\1_6_Strings:Raccoon_interim")}", LogLevel.Error);
				return;
			}
			Bundle bundle = Raccoon.GetBundle();
			foreach (BundleIngredientDescription ingredient in bundle.ingredients)
			{
				Game1.player.addItemByMenuIfNecessary(CreateItem(ingredient));
			}
		}
	}

	public static Item CreateItem(BundleIngredientDescription ingredient)
	{
		Item item = null!;

		if (ingredient.preservesId != null)
		{
			ItemQueryContext context = new(Game1.currentLocation, Game1.player, Game1.random, "query 'FLAVORED_ITEM'");
			if (ItemQueryResolver.TryResolve("FLAVORED_ITEM " + ingredient.id + " " + ingredient.preservesId, context).FirstOrDefault()?.Item is StardewValley.Object resultObj)
			{
				item = resultObj;
			}
		}
		item ??= ItemRegistry.Create(ingredient.id, ingredient.stack, quality: ingredient.quality);
		return item;
	}
}