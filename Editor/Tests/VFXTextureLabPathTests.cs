#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PudinKiller.VFXTextureLab.Tests
{
    public class VFXTextureLabPathTests
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("Assets/../../VFXTextureLabEscape")]
        [TestCase("Assets\\..\\..\\VFXTextureLabEscape")]
        [TestCase("Assets/../AssetsBackup/Escape")]
        [TestCase("Assets/Subfolder/../../Escape")]
        [TestCase("Assets//Escape")]
        [TestCase("AssetsBackup/Escape")]
        [TestCase("Packages/Escape")]
        [TestCase("/Assets/Escape")]
        [TestCase("Assets/Bad\0Name")]
        public void UnsafePaths_AreRejectedByValidationAndConversion(string path)
        {
            Assert.That(Validate(path), Is.False);
            AssertRejected("AssetPathToAbsolutePath", path);
        }

        [TestCase("Assets/C:/Escape")]
        [TestCase("Assets/Name:stream")]
        [TestCase("Assets/.. /Escape")]
        [TestCase("Assets/.../Escape")]
        [TestCase("Assets/Name. /Escape")]
        public void WindowsAliases_AreRejected(string path)
        {
            if (Path.DirectorySeparatorChar != '\\') Assert.Ignore("Windows path syntax only.");
            Assert.That(Validate(path), Is.False);
            AssertRejected("AssetPathToAbsolutePath", path);
        }

        [TestCase("Assets", "")]
        [TestCase("Assets/", "")]
        [TestCase("Assets/VFXTextureLabOutput", "VFXTextureLabOutput")]
        [TestCase("Assets\\Nested\\Output\\", "Nested/Output")]
        [TestCase("Assets/./Nested/Output", "Nested/Output")]
        [TestCase("Assets/Folder With Spaces/texture.png", "Folder With Spaces/texture.png")]
        public void ValidPaths_ResolveInsideAssets(string path, string relativePath)
        {
            Assert.That(Validate(path), Is.True);
            string expected = Path.GetFullPath(Path.Combine(Application.dataPath, relativePath));
            Assert.That(Invoke("AssetPathToAbsolutePath", path), Is.EqualTo(expected));
        }

        [Test]
        public void AbsolutePaths_AreRejected()
        {
            Assert.That(Validate(Application.dataPath), Is.False);
            AssertRejected("AssetPathToAbsolutePath", Application.dataPath);
        }

        [Test]
        public void ExistingJunction_AreRejectedBeforeDirectoryOrFileCreation()
        {
            if (Path.DirectorySeparatorChar != '\\') Assert.Ignore("Windows junction syntax only.");
            string id = Guid.NewGuid().ToString("N");
            string assetPath = "Assets/VFXTextureLabJunction_" + id;
            string link = Path.Combine(Application.dataPath, "VFXTextureLabJunction_" + id);
            string target = Path.Combine(Path.GetDirectoryName(Application.dataPath), "VFXTextureLabJunctionTarget_" + id);
            Directory.CreateDirectory(target);
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    process.WaitForExit();
                    Assert.That(process.ExitCode, Is.Zero, process.StandardError.ReadToEnd());
                }
                Assert.That(Validate(assetPath), Is.False);
                Assert.That(Validate(assetPath + "/Nested"), Is.False);
                AssertRejected("EnsureAssetFolder", assetPath + "/Nested");
                AssertRejected("AssetPathToAbsolutePath", assetPath + "/texture.png");
                Assert.That(Directory.GetFileSystemEntries(target), Is.Empty);
            }
            finally
            {
                if (Directory.Exists(link)) Directory.Delete(link);
                Directory.Delete(target);
            }
        }

        [Test]
        public void EnsureAssetFolder_TraversalDoesNotCreateDirectory()
        {
            string path = "Assets/../VFXTextureLabEscape_" + Guid.NewGuid().ToString("N");
            string escaped = Path.GetFullPath(Path.Combine(Application.dataPath, path.Substring(7)));
            Assert.That(Directory.Exists(escaped), Is.False);
            AssertRejected("EnsureAssetFolder", path);
            Assert.That(Directory.Exists(escaped), Is.False);
        }

        [TestCase(VFXOutputFormat.PNG8Bit)]
        [TestCase(VFXOutputFormat.EXRFloat)]
        public void WriteTexture_TraversalDoesNotCreateFile(VFXOutputFormat format)
        {
            string path = "Assets/../VFXTextureLabEscape_" + Guid.NewGuid().ToString("N") + ".png";
            string escaped = Path.GetFullPath(Path.Combine(Application.dataPath, path.Substring(7)));
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            try
            {
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                AssertRejected("WriteTexture", texture, path, format);
                Assert.That(File.Exists(escaped), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [TestCase(VFXOutputFormat.PNG8Bit)]
        [TestCase(VFXOutputFormat.EXRFloat)]
        public void ValidFolderAndTexture_CanBeCreated(VFXOutputFormat format)
        {
            string folder = "Assets/VFXTextureLabPathTests_" + Guid.NewGuid().ToString("N");
            string path = folder + (format == VFXOutputFormat.PNG8Bit ? "/texture.png" : "/texture.exr");
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            try
            {
                Invoke("EnsureAssetFolder", folder);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                Invoke("WriteTexture", texture, path, format);
                string absolutePath = (string)Invoke("AssetPathToAbsolutePath", path);
                Assert.That(new FileInfo(absolutePath).Length, Is.GreaterThan(0));
                AssetDatabase.ImportAsset(path);
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static bool Validate(string path) => (bool)Invoke("ValidateOutputFolder", path);

        private static object Invoke(string method, params object[] arguments)
        {
            return typeof(VFXTextureLabWindow).GetMethod(method, StaticPrivate).Invoke(null, arguments);
        }

        private static void AssertRejected(string method, params object[] arguments)
        {
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => Invoke(method, arguments));
            Assert.That(exception.InnerException, Is.TypeOf<ArgumentException>());
        }
    }
}
#endif
