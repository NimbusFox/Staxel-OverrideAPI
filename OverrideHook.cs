using System;
using System.Collections.Generic;
using NimbusFox.FoxCore;
using NimbusFox.FoxCore.Managers;
using NimbusFox.OverrideAPI.Patches;
using Plukit.Base;
using Staxel;
using Staxel.Items;
using Staxel.Logic;
using Staxel.Modding;
using Staxel.Tiles;

namespace NimbusFox.OverrideAPI {
    public class OverrideHook : IModHookV3 {
        internal static IReadOnlyDictionary<string, string> Tiles => _tiles;
        internal static IReadOnlyDictionary<string, string> Items => _items;

        private static Dictionary<string, string> _tiles;
        private static Dictionary<string, string> _items;

        public OverrideHook() {
            var fxCore = new Fox_Core("NimbusFox", "OverrideAPI", "0.1");

            fxCore.PatchController.Override(typeof(TileDatabase), "LoadDefinitions", typeof(TileDatabasePatches),
                nameof(TileDatabasePatches.LoadDefinitionsInit), TileDatabasePatches.LoadDefinitionsTranspiler);
        }

        internal static void Reload() {
            _tiles = new Dictionary<string, string>();
            _items = new Dictionary<string, string>();

            foreach (var file in GameContext.AssetBundleManager.FindByExtension(".tile.override")) {
                var stream = GameContext.ContentLoader.ReadStream(file);

                var blob = BlobAllocator.Blob(true);

                blob.LoadJsonStream(stream);

                stream.Close();

                stream.Dispose();

                if (blob.Contains("__inherits") && blob.GetString("__inherits").EndsWith(".tile")) {
                    _tiles.Add(blob.GetString("__inherits"), file);
                }

                Blob.Deallocate(ref blob);
            }

            foreach (var file in GameContext.AssetBundleManager.FindByExtension(".item.override")) {
                var stream = GameContext.ContentLoader.ReadStream(file);

                var blob = BlobAllocator.Blob(true);

                blob.LoadJsonStream(stream);

                stream.Close();

                stream.Dispose();

                if (blob.Contains("__inherits") && blob.GetString("__inherits").EndsWith(".item")) {
                    _items.Add(blob.GetString("__inherits"), file);
                }

                Blob.Deallocate(ref blob);
            }
        }

        public void Dispose() { }

        public void GameContextInitializeInit() {
        }

        public void GameContextInitializeBefore() {
        }

        public void GameContextInitializeAfter() {
        }

        public void GameContextDeinitialize() { }

        public void GameContextReloadBefore() {
        }
        public void GameContextReloadAfter() { }
        public void UniverseUpdateBefore(Universe universe, Timestep step) { }
        public void UniverseUpdateAfter() { }
        public bool CanPlaceTile(Entity entity, Vector3I location, Tile tile, TileAccessFlags accessFlags) {
            return true;
        }

        public bool CanReplaceTile(Entity entity, Vector3I location, Tile tile, TileAccessFlags accessFlags) {
            return true;
        }

        public bool CanRemoveTile(Entity entity, Vector3I location, TileAccessFlags accessFlags) {
            return true;
        }

        public void ClientContextInitializeInit() { }
        public void ClientContextInitializeBefore() { }
        public void ClientContextInitializeAfter() { }
        public void ClientContextDeinitialize() { }
        public void ClientContextReloadBefore() { }
        public void ClientContextReloadAfter() { }
        public void CleanupOldSession() { }
        public bool CanInteractWithTile(Entity entity, Vector3F location, Tile tile) {
            return true;
        }

        public bool CanInteractWithEntity(Entity entity, Entity lookingAtEntity) {
            return true;
        }
    }
}
