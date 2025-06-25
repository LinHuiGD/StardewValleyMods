using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using Pathoschild.Stardew.Common.Patching;
using Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;
using Fai0.StardewValleyMods.Common;
using StardewValley.Locations;
using StardewValley;
using StardewValley.Characters;
using System;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere;

internal class ModEntry : Mod
{
    public static HUDMessageHelper MsgHelper = null!;
    public static CommandHandler commandHandler = null!;
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        MsgHelper = new HUDMessageHelper(helper);
        commandHandler = new CommandHandler(Monitor, Helper);
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        HarmonyPatcher.Apply(this,
            new Game1Patcher(Monitor, MsgHelper),
            new JunimoNoteMenuPatcher(Monitor, Helper, ModManifest),
            new RaccoonIconPatcher(Monitor, Helper, MsgHelper),
            new MultiplayerPatcher(),
            new GameLocationPatcher(),
            new RaccoonPatcher()
        );
    }

	private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
	{
        if (e.FromModID == ModManifest.UniqueID && e.Type == "AreaCompletedMessage")
        {
            AreaCompletedMessage message = e.ReadAs<AreaCompletedMessage>();
            JunimoNoteMenuPatcher.OnReceivedAreaCompletedMessage(message);
        }
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // add Raccoon for remote access
        // otherwise, raccoon will only be added utill player enters the forest 
        if (Game1.MasterPlayer.mailReceived.Contains("raccoonMovedIn"))
        {
            Forest forset = Game1.RequireLocation<Forest>("Forest");
            if (forset.getCharacterFromName("Raccoon") == null)
            {
                Monitor.Log("Add raccoon.", LogLevel.Trace);
                forset.characters.Add(new Raccoon(mrs_racooon: false));
            }
        }
    }

	private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
		string oldDetail = ModUtilities.MenuDetails(e.OldMenu as JunimoNoteMenu);
		string newDetail = ModUtilities.MenuDetails(e.NewMenu as JunimoNoteMenu);
        Monitor.Log($"Menu changed: {e.OldMenu?.GetType().Name}{oldDetail} -> {e.NewMenu?.GetType().Name}{newDetail}", LogLevel.Trace);
        if (e.NewMenu is JunimoNoteMenu junimoMenu)
        {
            foreach (Bundle bundle in junimoMenu.bundles)
            {
                bundle.depositsAllowed = true;
            }
        }
    }
}
