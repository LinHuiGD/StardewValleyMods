using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Mods;
using System;
using System.Collections.Generic;

namespace Fai0.StardewValleyMods.Common;

/// <summary>
/// HUDMessage can be drawn in any RenderSteps with this helper.<br/>
/// </summary>
/// <remarks>
/// HUDMessage added by Game1.addHUDMessage is only drawn in RenderSteps.HUD, and menu is drawn after this, menu will block hud.
/// </remarks>
public class HUDMessageHelper : IDisposable
{
    public static readonly PerScreen<Dictionary<RenderSteps, List<HUDMessage>>> hudMessageGroups = new(() => []);
    public static Dictionary<RenderSteps, List<HUDMessage>> HUDMessageGroups
    {
        get => hudMessageGroups.Value;
        set => hudMessageGroups.Value = value;
    }
    private readonly IModHelper Helper = null!;
    public HUDMessageHelper(IModHelper helper)
    {
        Helper = helper;
        Helper.Events.Display.RenderingStep += OnRenderingStep;
        Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    public void Dispose()
    {
        Helper.Events.Display.RenderingStep -= OnRenderingStep;
        Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        GC.SuppressFinalize(this);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        foreach (var hudMessages in HUDMessageGroups.Values)
        {
            hudMessages.RemoveAll(hudMessage => hudMessage.update(Game1.currentGameTime));
        }
    }

    // derived from Game1.addHUDMessage()
    public void AddHUDMessage(HUDMessage hudMessage, RenderSteps step = RenderSteps.Overlays)
    {
        if (!HUDMessageGroups.TryGetValue(step, out var hudMessages))
        {
            hudMessages = [];
            HUDMessageGroups[step] = hudMessages;
        }

        if (hudMessage.type != null || hudMessage.whatType != 0)
        {
            for (int i = 0; i < hudMessages.Count; i++)
            {
                if (hudMessage.type != null && hudMessage.type == hudMessages[i].type)
                {
                    hudMessages[i].number = hudMessages[i].number + hudMessage.number;
                    hudMessages[i].timeLeft = 3500f;
                    hudMessages[i].transparency = 1f;
                    if (hudMessages[i].number > 50000)
                    {
                        HUDMessage.numbersEasterEgg(hudMessages[i].number);
                    }
                    return;
                }
                if (hudMessage.whatType == hudMessages[i].whatType && hudMessage.whatType != 1 && hudMessage.message != null && hudMessage.message.Equals(hudMessages[i].message))
                {
                    hudMessages[i].timeLeft = hudMessage.timeLeft;
                    hudMessages[i].transparency = 1f;
                    return;
                }
            }
        }
        hudMessages.Add(hudMessage);
        for (int i2 = hudMessages.Count - 1; i2 >= 0; i2--)
        {
            if (hudMessages[i2].noIcon)
            {
                HUDMessage tmp = hudMessages[i2];
                hudMessages.RemoveAt(i2);
                hudMessages.Add(tmp);
            }
        }
    }

    private void OnRenderingStep(object? sender, RenderingStepEventArgs e)
    {
        if (HUDMessageGroups.TryGetValue(e.Step, out var hudMessages) && hudMessages.Count > 0 && !Game1.game1.takingMapScreenshot)
        {
            int heightUsed = 0;
            for (int i = hudMessages.Count - 1; i >= 0; i--)
            {
                hudMessages[i].draw(Game1.spriteBatch, i, ref heightUsed);
            }
        }
    }

}
