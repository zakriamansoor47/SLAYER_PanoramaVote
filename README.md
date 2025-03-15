# SLAYER_PanoramaVote
# Accepting Paid Request! Discord: Slayer47#7002
# Donation
<a href="https://www.buymeacoffee.com/slayer47" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>

## Description:
Panorama Vote (Default CS2 Vote) for CounterStrikeSharp.

## Installation:
**1.** Upload file **platform_english.txt** to your server and Game **\game\csgo\resource**
### Note: For Custom Vote text this file should be installed in both server and client game. (Download to players by Workshop I guess).

## How to Add Custom TEXT in Vote:
When you send the Panorama Vote to Player(s) like this
```c#
voteHandler.SendYesNoVoteToAll(30.0f, VoteConstants.VOTE_CALLER_SERVER, "#SFUI_vote_panorama_vote_default", $"Hold on, Let me Cook", VoteResultCallback, VoteHandlerCallback);
```

Replace `$"Hold on, Let me Cook"` with your Text.

To change Text Color, replace `"#SFUI_vote_panorama_vote_default"` with the following pre-defined colors by me in **platform_english.txt**:

```c#
"SFUI_vote_panorama_vote_default"			      "{s:s1}"
"SFUI_vote_panorama_vote_darkred"           "<font color='#8B0000'>{s:s1}</font>"
"SFUI_vote_panorama_vote_green"             "<font color='#00FF00'>{s:s1}</font>"
"SFUI_vote_panorama_vote_lightyellow"       "<font color='#FFFF00'>{s:s1}</font>"
"SFUI_vote_panorama_vote_lightblue"         "<font color='#ADD8E6'>{s:s1}</font>"
"SFUI_vote_panorama_vote_olive"             "<font color='#808000'>{s:s1}</font>"
"SFUI_vote_panorama_vote_lime"              "<font color='#00FF00'>{s:s1}</font>"
"SFUI_vote_panorama_vote_red"               "<font color='#FF0000'>{s:s1}</font>"
"SFUI_vote_panorama_vote_lightpurple"       "<font color='#DDA0DD'>{s:s1}</font>"
"SFUI_vote_panorama_vote_purple"            "<font color='#800080'>{s:s1}</font>"
"SFUI_vote_panorama_vote_grey"              "<font color='#808080'>{s:s1}</font>"
"SFUI_vote_panorama_vote_yellow"            "<font color='#FFFF00'>{s:s1}</font>"
"SFUI_vote_panorama_vote_gold"              "<font color='#FFD700'>{s:s1}</font>"
"SFUI_vote_panorama_vote_silver"            "<font color='#C0C0C0'>{s:s1}</font>"
"SFUI_vote_panorama_vote_blue"              "<font color='#0000FF'>{s:s1}</font>"
"SFUI_vote_panorama_vote_darkblue"          "<font color='#00008B'>{s:s1}</font>"
"SFUI_vote_panorama_vote_bluegrey"          "<font color='#B0C4DE'>{s:s1}</font>"
"SFUI_vote_panorama_vote_magenta"           "<font color='#FF00FF'>{s:s1}</font>"
"SFUI_vote_panorama_vote_lightrred"         "<font color='#FF6347'>{s:s1}</font>"
"SFUI_vote_panorama_vote_orange"            "<font color='#FFA500'>{s:s1}</font>"
"SFUI_vote_panorama_vote_black"             "<font color='#000000'>{s:s1}</font>"
"SFUI_vote_panorama_vote_white"             "<font color='#FFFFFF'>{s:s1}</font>"
"SFUI_vote_panorama_vote_cyan"              "<font color='#00FFFF'>{s:s1}</font>"
"SFUI_vote_panorama_vote_pink"              "<font color='#FFC0CB'>{s:s1}</font>"
"SFUI_vote_panorama_vote_brown"             "<font color='#A52A2A'>{s:s1}</font>"
"SFUI_vote_panorama_vote_turquoise"         "<font color='#40E0D0'>{s:s1}</font>"
"SFUI_vote_panorama_vote_teal"              "<font color='#008080'>{s:s1}</font>"
"SFUI_vote_panorama_vote_indigo"            "<font color='#4B0082'>{s:s1}</font>"
"SFUI_vote_panorama_vote_violet"            "<font color='#EE82EE'>{s:s1}</font>"
"SFUI_vote_panorama_vote_skyblue"           "<font color='#87CEEB'>{s:s1}</font>"
"SFUI_vote_panorama_vote_beige"             "<font color='#F5F5DC'>{s:s1}</font>"
"SFUI_vote_panorama_vote_maroon"            "<font color='#800000'>{s:s1}</font>"
"SFUI_vote_panorama_vote_chocolate"         "<font color='#D2691E'>{s:s1}</font>"
"SFUI_vote_panorama_vote_salmon"            "<font color='#FA8072'>{s:s1}</font>"
"SFUI_vote_panorama_vote_periwinkle"        "<font color='#CCCCFF'>{s:s1}</font>"
"SFUI_vote_panorama_vote_silver"            "<font color='#C0C0C0'>{s:s1}</font>"
"SFUI_vote_panorama_vote_lavender"          "<font color='#E6E6FA'>{s:s1}</font>"
"SFUI_vote_panorama_vote_aquamarine"        "<font color='#7FFFD4'>{s:s1}</font>"
"SFUI_vote_panorama_vote_charcoal"          "<font color='#36454F'>{s:s1}</font>"
```

To send Image to Player, replace `"#SFUI_vote_panorama_vote_default"` with `$"SFUI_vote_panorama_vote_image"` which is pre-defined by me in **platform_english.txt**:

```c#
"SFUI_vote_panorama_vote_image"				"<img src='{s:s1}'>"
```

## How to Add your custom HTML in `platform_english.txt`:

First of all, whenever you EDIT **platform_english.txt** you have to restart both server and player game to take effect.

Vote Title should must start with `SFUI_vote_`:

```c#
"SFUI_vote_chinese" "是否投降?<br> (<font color='#00FF00'>需要全队玩家同意</font>)"
```

To add/change the Vote Passed and Vote Failed Text you have to add `SFUI_vote_passed_` or `SFUI_vote_failed_` at the start of the title like in my **platform_english.txt**:

```c#
"SFUI_vote_passed_panorama_vote"			"<font color='#00ff12'>{s:s1}</font>"
"SFUI_vote_failed_panorama_vote"			"<font color='#ff0000'>{s:s1}</font>"
