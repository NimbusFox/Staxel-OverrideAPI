using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Plukit.Base;
using Staxel;

namespace NimbusFox.OverrideAPI {
    public class DirectoryManager {
        private static List<string> FilesInUse = new List<string>();
        private string _localContentLocation;
        private string _root;
        private DirectoryManager _parent { get; set; }
        
        public string Folder { get; private set; }

        public DirectoryManager Parent => _parent ?? this;

        public DirectoryManager TopLevel {
            get {
                if (_parent != null) {
                    return _parent.TopLevel;
                }

                return this;
            }
        }

        internal string GetPath(char seperator) {
            return Regex.Replace(_localContentLocation.Replace(_root, ""), @"\/|\\", seperator.ToString()) + seperator;
        }

        internal DirectoryManager(string author, string mod) {
            var streamLocation = Path.Combine("Mods", author, mod);
            _localContentLocation = Path.Combine(GameContext.ContentLoader.LocalContentDirectory, streamLocation);
            _root = _localContentLocation;
            Folder = mod;

            if (!Directory.Exists(_localContentLocation)) {
                Directory.CreateDirectory(_localContentLocation);
            }
        }

        internal DirectoryManager(string mod) {
            _localContentLocation = Path.Combine(GameContext.ContentLoader.RootDirectory, "mods", mod);
            _root = GameContext.ContentLoader.RootDirectory;
            Folder = mod;
        }

        internal DirectoryManager() {
            _localContentLocation = GameContext.ContentLoader.RootDirectory;
            _root = _localContentLocation;
            Folder = "content";
        }

        public DirectoryManager FetchDirectory(string name) {
            var dir = new DirectoryManager {
                _localContentLocation = Path.Combine(_localContentLocation, name),
                _root = _root,
                _parent = this,
                Folder = name
            };

            CreateDirectory(name);

            return dir;
        }

        private void CreateDirectory(string name) {
            if (!DirectoryExists(name)) {
                Directory.CreateDirectory(Path.Combine(_localContentLocation, name));
            }
        }

        public IReadOnlyList<string> Directories => new DirectoryInfo(_localContentLocation).GetDirectories().Select(dir => dir.Name).ToList();

        public IReadOnlyList<string> Files => new DirectoryInfo(_localContentLocation).GetFiles().Select(file => file.Name).ToList();

        public bool FileExists(string name) {
            return Files.Contains(name);
        }

        public bool DirectoryExists(string name) {
            return Directories.Contains(name);
        }

        public static Blob SerializeObject<T>(T data) {
            if (data is Blob o) {
                return o;
            }
            var blob = BlobAllocator.AcquireAllocator().NewBlob(true);
            blob.ObjectToBlob(null, data);
            return blob;
        }

        public void WriteFile<T>(string fileName, T data, Action onFinish = null, bool outputAsText = false) {
            new Thread(() => {
                var target = Path.Combine(GetPath(Path.DirectorySeparatorChar), fileName);
                var collection = new List<string>();
                collection.AddAll(FilesInUse);
                while (collection.Any(x => x == target)) { 
                    collection.Clear();
                    collection.AddAll(FilesInUse);
                }

                FilesInUse.Add(target);

                var stream = new MemoryStream();
                var output = SerializeObject(data);
                stream.Seek(0L, SeekOrigin.Begin);
                if (!outputAsText) {
                    stream.WriteBlob(output);
                } else {
                    output.SaveJsonStream(stream);
                }
                stream.Seek(0L, SeekOrigin.Begin);
                File.WriteAllBytes(Path.Combine(_localContentLocation, fileName), stream.ReadAllBytes());
                onFinish?.Invoke();

                FilesInUse.Remove(target);
            }).Start();
        }

        public void WriteFileStream(string fileName, Stream stream, Action onWrite = null) {
            new Thread(() => {
                var target = Path.Combine(GetPath(Path.DirectorySeparatorChar), fileName);
                var collection = new List<string>();
                collection.AddAll(FilesInUse);
                while (collection.Any(x => x == target)) {
                    collection.Clear();
                    collection.AddAll(FilesInUse);
                }

                FilesInUse.Add(target);

                stream.Seek(0L, SeekOrigin.Begin);
                File.WriteAllBytes(Path.Combine(_localContentLocation, fileName), stream.ReadAllBytes());
                onWrite?.Invoke();

                FilesInUse.Remove(target);
            }).Start();
        }

        public void ReadFile<T>(string fileName, Action<T> onLoad, bool inputIsText = false) {
            new Thread(() => {
                var target = Path.Combine(GetPath(Path.DirectorySeparatorChar), fileName);
                var collection = new List<string>();
                collection.AddAll(FilesInUse);
                while (collection.Any(x => x == target)) {
                    collection.Clear();
                    collection.AddAll(FilesInUse);
                }

                if (FileExists(fileName)) {
                    var stream =
                        GameContext.ContentLoader.ReadStream(Path.Combine(GetPath('/'), fileName));
                    Blob input;
                    if (!inputIsText) {
                        input = stream.ReadBlob();
                    } else {
                        if (typeof(T) == typeof(string)) {
                            onLoad((T)(object)stream.ReadAllText());
                            return;
                        }
                        input = BlobAllocator.AcquireAllocator().NewBlob(false);
                        var sr = new StreamReader(stream);
                        input.ReadJson(sr.ReadToEnd());
                    }

                    stream.Seek(0L, SeekOrigin.Begin);
                    if (typeof(T) == input.GetType()) {
                        onLoad((T)(object)input);
                        return;
                    }

                    onLoad(input.BlobToObject(null, (T)Activator.CreateInstance(typeof(T))));
                    return;
                }

                onLoad((T)Activator.CreateInstance(typeof(T)));
            }).Start();
        }

        public void ReadFileStream(string fileName, Action<Stream> onLoad, bool required = false) {
            new Thread(() => {
                var target = Path.Combine(GetPath(Path.DirectorySeparatorChar), fileName);
                var collection = new List<string>();
                collection.AddAll(FilesInUse);
                while (collection.Any(x => x == target)) {
                    collection.Clear();
                    collection.AddAll(FilesInUse);
                }

                var stream =
                    GameContext.ContentLoader.ReadStream(Path.Combine(GetPath('/'), fileName));
                stream.Seek(0L, SeekOrigin.Begin);
                onLoad?.Invoke(stream);
            }).Start();
        }

        public void DeleteFile(string name) {
            if (FileExists(name)) {
                var target = Path.Combine(GetPath(Path.DirectorySeparatorChar), name);
                var collection = new List<string>();
                collection.AddAll(FilesInUse);
                while (collection.Any(x => x == target)) {
                    collection.Clear();
                    collection.AddAll(FilesInUse);
                }

                File.Delete(Path.Combine(_localContentLocation, name));
            }
        }

        public void DeleteDirectory(string name, bool recursive) {
            if (DirectoryExists(name)) {
                var target = Path.Combine(GetPath(Path.DirectorySeparatorChar), name);
                var collection = new List<string>();
                collection.AddAll(FilesInUse);
                while (collection.Any(x => x == target)) {
                    collection.Clear();
                    collection.AddAll(FilesInUse);
                }

                Directory.Delete(Path.Combine(_localContentLocation, name), recursive);
            }
        }
    }
}
