using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Auto9Slicer
{
    [CreateAssetMenu(menuName = "Auto 9Slice/Tester", fileName = nameof(Auto9SliceTester))]
    public class Auto9SliceTester : ScriptableObject
    {
        public SliceOptions Options => options;
        [SerializeField] private SliceOptions options = new SliceOptions();

        public bool CreateBackup => createBackup;
        [SerializeField] private bool createBackup = true;

        public void RunDirectory()
        {
            // var directoryPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            foreach (var dir in options.directories)
            {
                if (dir == null) continue;

                var path = AssetDatabase.GetAssetPath(dir);
                Debug.Log(path);
                RunDirectory(path);
            }

            AssetDatabase.Refresh();
        }

        protected void RunDirectory(string directoryPath)
        {
            if (directoryPath == null) throw new Exception($"directoryPath == null");
            if (Directory.Exists(directoryPath) == false) return;

            var fullDirectoryPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", directoryPath);
            var targets = Directory.GetFiles(fullDirectoryPath)
                .Select(Path.GetFileName)
                .Where(x => x.EndsWith(".png") || x.EndsWith(".jpg") || x.EndsWith(".jpeg"))
                .Where(x => !x.Contains(".original"))
                .Select(x => Path.Combine(directoryPath, x))
                .Select(x => (Path: x, Texture: AssetDatabase.LoadAssetAtPath<Texture2D>(x)))
                .Where(x => x.Item2 != null)
                .ToArray();

            var backupDirPath = Path.Combine(Directory.GetParent(directoryPath).FullName, Path.GetFileName(directoryPath) + "_Backup");
            if (Directory.Exists(backupDirPath) == false)
            {
                Directory.CreateDirectory(backupDirPath);
            }

            foreach (var target in targets)
            {
                SliceTexture(target.Path, backupDirPath);
            }
        }

        public void RunTextures()
        {
            var assetPath = AssetDatabase.GetAssetPath(this);
            var backupDirPath = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + "_Backup");
            if (Directory.Exists(backupDirPath) == false)
            {
                Directory.CreateDirectory(backupDirPath);
            }

            foreach (var tex in options.textures)
            {
                if (tex == null) continue;

                var path = AssetDatabase.GetAssetPath(tex);
                SliceTexture(path, backupDirPath);
            }

            AssetDatabase.Refresh();
        }

        protected void SliceTexture(string path, string backupDirPath)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer is TextureImporter textureImporter)
            {
                if (textureImporter.spriteBorder != Vector4.zero) return;
                var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", path);
                var bytes = File.ReadAllBytes(fullPath);

                // importerのreadable設定に依らずに読み込むために直接読む
                var targetTexture = new Texture2D(2, 2);
                targetTexture.LoadImage(bytes);

                var slicedTexture = Slicer.Slice(targetTexture, Options);
                if (slicedTexture.Texture == null) return;

                // バックアップ
                if (CreateBackup)
                {
                    var fileName = Path.GetFileNameWithoutExtension(fullPath);
                    File.WriteAllBytes(Path.Combine(backupDirPath, fileName + ".original" + Path.GetExtension(fullPath)), bytes);
                }

                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteBorder = slicedTexture.Border.ToVector4();
                if (fullPath.EndsWith(".png")) File.WriteAllBytes(fullPath, slicedTexture.Texture.EncodeToPNG());
                if (fullPath.EndsWith(".jpg")) File.WriteAllBytes(fullPath, slicedTexture.Texture.EncodeToJPG());
                if (fullPath.EndsWith(".jpeg")) File.WriteAllBytes(fullPath, slicedTexture.Texture.EncodeToJPG());

                Debug.Log($"Auto 9Slice {Path.GetFileName(path)} = {textureImporter.spriteBorder}");
            }
        }
    }

    [CustomEditor(typeof(Auto9SliceTester))]
    public class Auto9SliceTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space(20);

            if (GUILayout.Button("Run Directories"))
            {
                ((Auto9SliceTester)target).RunDirectory();
            }

            if (GUILayout.Button("Run Textures"))
            {
                ((Auto9SliceTester)target).RunTextures();
            }
        }
    }
}