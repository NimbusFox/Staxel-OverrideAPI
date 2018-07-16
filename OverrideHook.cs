using System;
using Plukit.Base;
using Staxel.Items;
using Staxel.Logic;
using Staxel.Modding;
using Staxel.Tiles;

namespace NimbusFox.OverrideAPI {
    public class OverrideHook : IModHookV3 {
        public void Dispose() { }
        public void GameContextInitializeInit() { }
        public void GameContextInitializeBefore() { }

        private static readonly DirectoryManager ContentRoot = new DirectoryManager();

        public void GameContextInitializeAfter() {
            var dir = ContentRoot.FetchDirectory("mods");

            foreach (var folder in dir.Directories) {
                CycleDirs(dir.FetchDirectory(folder));
            }
        }

        private void CycleDirs(DirectoryManager currentDir) {
            foreach (var dir in currentDir.Directories) {
                CycleDirs(currentDir.FetchDirectory(dir));
            }

            CycleFiles(currentDir);
        }

        private static void CycleFiles(DirectoryManager currentDir) {
            foreach (var file in currentDir.Files) {
                try {
                    var extension = file.Split('.').Last().ToLower();

                    if (extension != "override") {
                        continue;
                    }

                    var wait = true;

                    currentDir.ReadFile<Blob>(file, data => {
                        if (!data.Contains("target") && !data.Contains("overrides")) {
                            wait = false;
                            return;
                        }

                        if (data.KeyValueIteratable["target"].Kind != BlobEntryKind.String) {
                            wait = false;
                            return;
                        }

                        var target = data.GetString("target").Split('.').Last().ToLower();

                        if (target == "item") {
                            ProcessItem(data);
                        }

                        wait = false;
                    }, true);

                    while (wait) {

                    }
                } catch (Exception ex) {
                    Logger.LogException(ex);
                }
            }
        }

        private static void ProcessItem(Blob data) {
            if (TryGetConfig(data.GetString("target"), out var item)) {

            }
        }

        private static bool TryGetConfig(string target, out Blob blob) {
            var current = ContentRoot;

            foreach (var dir in target.Split('/')) {
                if (current.DirectoryExists(dir)) {
                    current = current.FetchDirectory(dir);
                }
            }

            if (current.FileExists(target.Split('/').Last())) {
                var wait = true;
                Blob output = null;
                current.ReadFile<Blob>(target.Split('/').Last(), data => {
                    output = data;
                    wait = false;
                }, true);

                while (wait) { }

                blob = output;
                return true;
            }

            blob = null;
            return false;
        }

        public void GameContextDeinitialize() { }
        public void GameContextReloadBefore() { }
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
