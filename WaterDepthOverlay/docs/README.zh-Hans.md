## 介绍
受[钓鱼攻略](https://zh.stardewvalleywiki.com/钓鱼攻略)启发，我决定在游戏中实现[钓鱼区域](https://zh.stardewvalleywiki.com/钓鱼攻略#距离图片)的可视化效果。

[English](README.md)

## 什么是钓鱼区域/水深？
> *摘自[钓鱼#钓鱼区域](https://zh.stardewvalleywiki.com/钓鱼#钓鱼区)*  
水域的每个网格都按照离（最近）岸边的距离被划分为钓鱼区0、1、2、3或5[2] ，**距离越远，数字越大，等级越高，钓鱼收获越好**。游戏将陆地、码头、石桥都视为岸，木桥除外。  
> 钓鱼区等级在距离大于等于5时设为5（注：在当前版本1.6.15中，最大水深为5）。  
> ...  
> 更高的钓鱼区域等级提供以下**优势**：
>
> - 钓上垃圾几率减小。
> - 平均而言，鱼基础品质更好、尺寸更大。
> - 难上钩的鱼上钩几率略微增大。
> - ...  
>
> 为便于理解，本模组中将钓鱼区域等级称为"**水深**"。

如下图所示，使用本模组后，不同水深的网格将覆盖不同颜色的遮罩。  

![主要效果预览图](https://i.imgur.com/bzHjojo.png)


## 如何使用？
按下[引号键(OemQuotes)](https://zh.stardewvalleywiki.com/模组:使用指南/按键绑定)启用/禁用遮罩绘制（可配置）。  
**注意：** 只有当前地图中**至少有一个可钓鱼的水域网格**且玩家**当前工具**是鱼竿时，才会绘制遮罩。

玩家可以在模组**配置菜单UI**中自定义水域网格的叠加颜色并预览混合效果。  
姜岛配置菜单：  
![姜岛配置菜单](https://i.imgur.com/lTONtTU.gif)  
火山口配置菜单：  
![火山口配置菜单](https://i.imgur.com/kPgLUuw.gif)  
此外，如果您喜欢，也可以在不可钓鱼的网格上绘制遮罩。  
即使一个网格显示为水域，如果被建筑物（如桥梁、房屋、岸）阻挡，玩家也无法在那里钓鱼。  
本模组中称之为**不可钓鱼网格**。  
![不可钓鱼网格配置菜单](https://i.imgur.com/ClK9R3N.png)  

## 新特性
>**自1.0.2版本起**：水深遮罩默认不再遮挡玩家。  

菜单UI中添加了名为"是否在顶层绘制遮罩"的开关，默认关闭。  
如果开启，水深遮罩将在游戏世界顶层绘制。  
那样的话，遮罩将遮挡渲染好的游戏世界，包括玩家。  
一个小取舍：在默认配置下，您无法看到鱼塘的遮罩。  
如果想看到它，请开启"是否在顶层绘制遮罩"开关，但这会使水深遮罩再次遮挡玩家。

## 示例
以下是手动编辑（我猜的）和游戏内绘制的钓鱼区域对比。  
手动编辑示例（[wiki](https://zh.stardewvalleywiki.com/钓鱼攻略#距离图片)），其图像显示了基于浮标落点的[钓鱼区域](https://zh.stardewvalleywiki.com/钓鱼#钓鱼区)，颜色编码为：![wiki颜色编码](https://stardewvalleywiki.com/mediawiki/images/1/14/DistanceKey.png)

* [矿井20层](https://stardewvalleywiki.com/mediawiki/images/4/4d/MinesDistances.png)
* [深山](https://stardewvalleywiki.com/mediawiki/images/8/87/MountainDistances.png)

本模组游戏内绘制示例，显示水深（同钓鱼区域），颜色编码为：![Mod颜色编码](https://i.imgur.com/OKXTUBN.png)，"W"表示参考水域的背景色。

* [矿井20层（危险）](https://i.imgur.com/aA7XKeF.png)
* [深山](https://i.imgur.com/KWEjXY7.png)
* [更多示例](https://imgur.com/gallery/waterdepthoverlay-aCbYfrw)

## 兼容性
* 星露谷物语 1.6 或更高版本
* Windows系统（未在MacOS或Linux测试）
* 单人模式（未在多人模式测试）
* [Stardew Valley Expanded](https://www.nexusmods.com/stardewvalley/mods/3753)
* [Visible Fish](https://www.nexusmods.com/stardewvalley/mods/8897)
* [Fishing Assistant 2](https://www.nexusmods.com/stardewvalley/mods/5815)
* [Infinite Zoom](https://www.nexusmods.com/stardewvalley/mods/8808)

## 多语言支持
欢迎贡献翻译或修订文案。  
现有翻译：`英文`、`中文`

## 另请参阅
* [版本说明](release-notes.md)
* [Nexus模组页面](https://www.nexusmods.com/stardewvalley/mods/34207)
