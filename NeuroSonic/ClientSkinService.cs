using System.IO;
using System.Linq;

using theori.Resources;

using NeuroSonic.Platform;

namespace NeuroSonic
{
    public static class ClientSkinService
    {
        public const string SkinsDirectory = "skins/";
        public const string DefaultSkinName = "Lasergame";

        public static readonly ClientResourceLocator DefaultSkin;

        public static ClientResourceLocator CurrentlySelectedSkin { get; private set; }

        static ClientSkinService()
        {
            DefaultSkin = new ClientResourceLocator("skins/user-custom", "materials/basic");
            DefaultSkin.AddManifestResourceLoader(ManifestResourceLoader.GetResourceLoader(typeof(ClientResourceLocator).Assembly, "theori.Resources"));
            DefaultSkin.AddManifestResourceLoader(ManifestResourceLoader.GetResourceLoader(typeof(NscClient).Assembly, "NeuroSonic.Resources"));

            CurrentlySelectedSkin = DefaultSkin;
        }

        public static string[] GetInstalledSkinNames()
        {
            return Directory.EnumerateDirectories(SkinsDirectory).ToArray();
        }

        public static ClientResourceLocator? GetSkinByName(string name)
        {
            string skinDirectory = $"{ SkinsDirectory }{ name }";
            if (Directory.Exists(skinDirectory))
                return new ClientResourceLocator(skinDirectory, "materials/basic");
            else return null;
        }

        public static void SelectSkinByName(string name)
        {
            if (GetSkinByName(name) is { } skin)
                SelectSkin(skin);
        }

        public static void SelectSkin(ClientResourceLocator skin)
        {
            CurrentlySelectedSkin = skin;
            // TODO(local): Update everything, load all necessary skin elements, etc etc
        }
    }
}
