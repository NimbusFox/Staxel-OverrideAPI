using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using Harmony.ILCopying;
using NimbusFox.FoxCore.Classes;
using Plukit.Base;
using Staxel;
using Staxel.Collections;
using Staxel.Tiles;

namespace NimbusFox.OverrideAPI.Patches {
    internal static class TileDatabasePatches {
        internal static IEnumerable<CodeInstruction> LoadDefinitionsTranspiler(
            IEnumerable<CodeInstruction> instructions) {
            var replacementMethod = AccessTools.Method(typeof(TileDatabasePatches), nameof(LoadDefinitionsInit));
            var methodIlInstructions = MethodBodyReader.GetInstructions(new DynamicMethod(Guid.NewGuid().ToString(), typeof(object), new Type[0]).GetILGenerator(), replacementMethod);
            var replacementMethodInstructions = new List<CodeInstruction>();

            foreach (var methodIlInstruction in methodIlInstructions) {
                replacementMethodInstructions.Add(methodIlInstruction.GetCodeInstruction());
            }

            return replacementMethodInstructions;
        }

        internal static void LoadDefinitionsInit(bool revalidate, bool disposeDrawables, bool storeBundle) {
            LoadDefinitions(revalidate, disposeDrawables, storeBundle);
        }

        private static void LoadDefinitions(bool revalidate, bool disposeDrawables, bool storeBundle) {
            var instance = GameContext.TileDatabase;

            var defenitions = instance.GetPrivateFieldValue<Dictionary<string, TileConfiguration>>("_definitions");

            foreach (KeyValuePair<string, TileConfiguration> definition in defenitions)
                definition.Value.Dispose();
            defenitions.Clear();

            var client = instance.GetPrivateFieldValue<bool>("_client");
            var storedMapping = instance.GetPrivateFieldValue<UInt32Map<string>>("_storedMapping");

            OverrideHook.Reload();

            foreach (var tileConfiguration1 in GameContext.Worker.Foreach(GameContext.AssetBundleManager.FindByExtension(".tile"), entryCapture => {
                var target = entryCapture;

                if (OverrideHook.Tiles.ContainsKey(target)) {
                    Logger.WriteLine($"OverrideAPI: Replacing {target} with {OverrideHook.Tiles[target]}");
                    target = OverrideHook.Tiles[target];
                }

                try {
                    return new TileConfiguration(target, storeBundle, true, disposeDrawables);
                } catch (Exception ex) {
                    throw new Exception("Exception while processing tile: " + entryCapture, ex);
                }
            }, 0, true)) {
                TileConfiguration tileConfiguration2;
                if (defenitions.TryGetValue(tileConfiguration1.Code, out tileConfiguration2) && tileConfiguration2 != null)
                    throw new Exception(string.Format((IFormatProvider)CultureInfo.InvariantCulture, "Duplicate tile Code {0} found in {1} and {2}", (object)tileConfiguration1.Code, (object)tileConfiguration1.Source, (object)tileConfiguration2.Source));
                defenitions.Add(tileConfiguration1.Code, tileConfiguration1);
            }
            TileConfiguration tileConfiguration3;
            if (!defenitions.TryGetValue("staxel.tile.Sky", out tileConfiguration3))
                throw new Exception("Required TileDefintion 'staxel.tile.Sky' not found.");
            foreach (TileConfiguration tileConfiguration1 in defenitions.Values) {
                TileConfiguration configuration = tileConfiguration1;
                GameContext.CategoryDatabase.AddCategories(configuration.Categories);
                configuration.CompoundStandin = ((IEnumerable<Wrapper<string>>)configuration.CompoundStandinCodes).Select<Wrapper<string>, TileConfiguration>((Func<Wrapper<string>, TileConfiguration>)(x => {
                    if (x.Value == "")
                        return configuration;
                    return defenitions[x.Value];
                })).ToArray<TileConfiguration>();
            }
            instance.SetPrivateFieldValue("_totemTiles", new Dictionary<string, TileConfiguration>());
            var totemTiles = instance.GetPrivateFieldValue<Dictionary<string, TileConfiguration>>("_totemTiles");
            foreach (KeyValuePair<string, TileConfiguration> definition in defenitions) {
                var orDefault = definition.Value.Components.Select<object>()
                    .FirstOrDefault(x => x.GetType().Name == "TotemComponent");
                if (orDefault != null) {
                    var totemCode = orDefault.GetPrivatePropertyValue<string>("TotemCode");
                    TileConfiguration tileConfiguration1;
                    if (totemTiles.TryGetValue(totemCode, out tileConfiguration1))
                        throw new Exception("Totem tile conflict for totem: " + totemCode + " tile: " + definition.Value.Code + " and tile: " + tileConfiguration1.Code);
                    totemTiles.Add(totemCode, definition.Value);
                }
            }

            AccessTools.Method(instance.GetType(), "ResolveAutoTileRefrences").Invoke(instance, new object[0]);
            AccessTools.Method(instance.GetType(), "ResolveSeasonalAliasInheritedValues")
                .Invoke(instance, new object[0]);
            if (!client) {
                AccessTools.Method(instance.GetType(), "BuildTileMapping").Invoke(instance, new object[] { revalidate });
            } else {
                storedMapping.Clear();
                GameContext.TileMapping = (TileConfigurationHolder[])null;
                GameContext.SolidTileMapping = (bool[])null;
            }

            AccessTools.Method(instance.GetType(), "SanityCheck").Invoke(instance, new object[0]);
        }
    }
}
