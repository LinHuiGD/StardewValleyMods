## Introduction
Inspired by [Fishing_Strategy](https://stardewvalleywiki.com/Fishing_Strategy), I decide to implement in-game visual effects of [Fishing_Strategy#Fishing_Zones](https://stardewvalleywiki.com/Fishing_Strategy#Fishing_Zones).

[简体中文](README.zh-Hans.md)

## **What is Fishing Zone/Water Depth?**
>*Here are extracts from [Fishing#Fishing_Zone](https://stardewvalleywiki.com/Fishing#Fishing_Zone)*  
>Every water tile is assigned a Fishing Zone of 0, 1, 2, 3, or 5 that controls many aspects of fishing. **The further from land (in every direction), the better the zone.** The game considers most walkable surfaces, including islands, piers, and stone bridges to be "**land**"; wooden footbridges are one exception.  
>If the bobber lands at least 5 tiles away from any land, it is located in zone 5\.  
>...  
>Higher fishing zone values provide several **benefits**:
>
>- The chances of catching trash decrease.
>- The size and quality (*e.g.,* normal, silver, or gold) of the fish is on average better.
>- The chances of hooking more difficult fish are slightly larger.
>- ...  
>
>For ease of understanding, I call the level of fishing zone "**water depth**" in this mod\.

As you can see below, tiles with different water depths will be covered by different color overlays with this mod.

![https://i.imgur.com/cMtI7U8.png](https://i.imgur.com/cMtI7U8.png)

## How to use?
Press [OemQuotes](https://stardewvalleywiki.com/Modding:Player_Guide/Key_Bindings#Keyboard) to enable/disable overlay drawing.(Configable)  
**Note:** The overlay will be drawn only when there is **at least a fishable water tile** in current map and the player's **current tool** is a fishing rod.

Players can **custom colors** of overlay on water tiles and preview the blended color in the mod  
**config menu U**I.  
Config menu in Ginger Island  
![https://i.imgur.com/dfJn3yn.gif](https://i.imgur.com/dfJn3yn.gif)  
Config menu in Volcano Caldera  
![https://i.imgur.com/ILrxvwN.gif](https://i.imgur.com/ILrxvwN.gif)  
Further more, overlays can be drawn on unfishable tiles if you like.  
Even if a tile is visible as water, if it is blocked by buildings (such as bridges, houses, shores), players cannot fish there.   
I call it **unfishable tile** in this mod.  
![https://i.imgur.com/uR3nFY1.png](https://i.imgur.com/uR3nFY1.png)  
## New feature
>**Since 1.0.2**: The water depth overlay no longer obscures the player's sprite by default.  

A switch named "Draw on top" has been added to the menu UI. It is off by default.  
If you turn it on, the water depth overlay will be drawn on top of the game world.  
As a result, the overlay will obscure the rendered game world, including the player's sprite.  
A small trade-off: In the default configuration, you can't see the overlay of the fish pond.  
Turn on the "Draw on top" switch if you want to see it, but then the water depth overlay will obscure the player's sprite again.

## Examples
Here are comparisons of manual editting(probably) and in-game drawing of fishing zones.  
Examples of manual editting([wiki](https://stardewvalleywiki.com/Fishing_Strategy#Fishing_Zones)),their images show the [Fishing Zone](https://stardewvalleywiki.com/Fishing#Fishing_Zone) based on the location where the bobber lands, color coded as: ![https://stardewvalleywiki.com/mediawiki/images/1/14/DistanceKey.png](https://stardewvalleywiki.com/mediawiki/images/1/14/DistanceKey.png)

* [Mine level 20](https://stardewvalleywiki.com/mediawiki/images/4/4d/MinesDistances.png)
* [Mountains](https://stardewvalleywiki.com/mediawiki/images/8/87/MountainDistances.png)

Examples of in-game drawing of this mod which show the water depth(same as Fishing Zone), color coded as:![https://i.imgur.com/OKXTUBN.png](https://i.imgur.com/OKXTUBN.png), "W" means background color for reference of water.

* [Mine level 20 dangerous](https://i.imgur.com/aA7XKeF.png)
* [Mountains](https://i.imgur.com/KWEjXY7.png)
* [More examples](https://imgur.com/gallery/waterdepthoverlay-aCbYfrw)

## Compatible with...
* Stardew Valley 1.6 or later;
* Windows; (Untested on MacOS or Linux)
* single-player;(Untested on multiplayer)
* [Visible Fish](https://www.nexusmods.com/stardewvalley/mods/8897)
* [Fishing Assistant 2](https://www.nexusmods.com/stardewvalley/mods/5815)

## Multi-language
Contributes are welcome.  
Available translations: `English`, `Chinese`

## See also
* [Release notes](release-notes.md)
* [Nexus mod](https://www.nexusmods.com/stardewvalley/mods/34207)
