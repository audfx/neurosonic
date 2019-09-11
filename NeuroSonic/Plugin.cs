﻿using System.IO;
using System.Globalization;

using theori.Configuration;
using theori.IO;
using theori.Platform;
using theori.Resources;

using NeuroSonic.IO;
using NeuroSonic.Properties;

namespace NeuroSonic
{
    internal static class Plugin
    {
        public const string NSC_CONFIG_FILE = "nsc-config.ini";

        private static ClientHost host;

        public static NscConfig Config { get; private set; }

        public static Gamepad? Gamepad { get; private set; }

        public static CultureInfo UICulture
        {
            get => Strings.Culture;
            set => Strings.Culture = value;
        }

        public static ClientResourceLocator DefaultResourceLocator { get; private set; }

        public static void Initialize(ClientHost host)
        {
            Plugin.host = host;

            Config = new NscConfig();
            if (File.Exists(NSC_CONFIG_FILE))
                LoadNscConfig();
            // save the defaults on init
            else SaveNscConfig();

            DefaultResourceLocator = ClientResourceLocator.Default.Clone();
            DefaultResourceLocator.AddManifestResourceLoader(ManifestResourceLoader.GetResourceLoader(typeof(Plugin).Assembly, "NeuroSonic.Resources"));

            //UICulture = CultureInfo.CreateSpecificCulture("ja-JP");

            SwitchActiveGamepad(host.Config.GetInt(TheoriConfigKey.Controller_DeviceID));
        }

        public static void SwitchActiveGamepad(int newDeviceIndex)
        {
            if (Gamepad != null && newDeviceIndex == Gamepad.DeviceIndex) return;
            Gamepad?.Close();

            host.Config.Set(TheoriConfigKey.Controller_DeviceID, newDeviceIndex);
            if (Gamepad.TryGet(newDeviceIndex) is { } gamepad)
            {
                Gamepad = gamepad;
                gamepad.Open();
            }
            Input.CreateController();

            SaveNscConfig();
        }

        internal static void LoadNscConfig()
        {
            using var reader = new StreamReader(File.OpenRead(NSC_CONFIG_FILE));
            Config.Load(reader);
        }

        public static void LoadConfig()
        {
            LoadNscConfig();
            host.LoadConfig();
        }

        internal static void SaveNscConfig()
        {
            using var writer = new StreamWriter(File.Open(NSC_CONFIG_FILE, FileMode.Create));
            Config.Save(writer);
        }

        public static void SaveConfig()
        {
            SaveNscConfig();
            host.SaveConfig();
        }
    }
}
