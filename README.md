![Asset Icon Library](https://imgur.com/z85SXkz.png)
# Asset Icon Library
Add icons to all of the game's assets, DLCs, and region packs!

From props, to buildings, and brands' billboards and signs. Add over **34'000 icons** to your game, and counting!



## Where will I see the icons?
Asset Icon Library populates the assets' icons in:
* The vanilla toolbars
* The "Add Object" menu
* Find It!
* Any mod that makes use of asset icons.



## Custom Styles
In Asset Icon Library's settings page, you can change the style of building icons from the selection of:
* Colored and no props
* White and no props
* Colored with props



## Adding your own icons
In Asset Icon Library's settings page, you can open the "Custom Icons Folder" where you can add your own icon files which are then automatically used in-game.

This allows you to add your own icons without needing to overwrite any file in the mod itself, where your icons could be lost on the next mod update.

Files must match the asset's prefab ID with this pattern: 'PrefabType.PrefabName.fileType'. 
For example: 'BuildingPrefab.EU_CommercialHigh01_L4_2x4.png'

You can however use the prefab name alone, although less accurate.



## Making mods that use Asset Icon Library
You can make your own icon packs, or make use of AIL functionalities in your mod with ease.
Custom mod icons take priority over default AIL icons, but not over the user's custom icons.


### Adding custom icons
Simply include either an '.ail' or 'ail' folder inside your mod with icon files inside of it, and AIL will automatically match them to available assets in-game.
This follows the same rules as adding your own icons (see above) for file names.


### Using an asset icon map
You can create a map of prefabs to icons, or prefabs to prefabs, in your mod.

Simply include a file with the '.ail' extension (for example: 'Map.ail') representing a JSON dictionary where the key is the asset name you want to change, and the value being a reference to another asset or a file inside your mod.

For example, the following JSON will replace Asset1 with VanillaAsset0's icon, and will set Asset2's icon to the file 'Icons\ABC.png' that's inside your mod folder.

{
   "PT.Asset1": "PT.VanillaAsset0",
   "PT.Asset2": "Icons\\ABC.png"
}

### Using the AIL API
If you have a code mod, you can supply AIL with an asset icon map, similar to the one above.
This allows you to have more advanced icons based on conditions, for example.


To do this, add a **public static Dictionary(string, string) GetIconsMap()** to your IMod and AIL will do the rest.
The dictionary uses the same method as the asset icon map section above.


For more information about the various APIs in Asset Icon Library, check the github's wiki page.
