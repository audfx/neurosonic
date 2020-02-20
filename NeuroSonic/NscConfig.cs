using theori.Configuration;
using theori.Gui;
using theori.IO;

namespace NeuroSonic
{
#if false
    public enum ControllerInput
    {
        Start,
        Back,
        BT0, BT1, BT2, BT3,
        FX0, FX1,
        Laser0Axis, Laser1Axis,
        Laser0Negative, Laser0Positive,
        Laser1Negative, Laser1Positive
    }

    public enum NscConfigKey
    {
        StandaloneChartsDirectory,
        FileBrowserLastDirectory,

        HiSpeed,
        HiSpeedModKind,
        ModSpeed,
        VideoOffset,
        InputOffset,
        Skin,
        Laser0Color,
        Laser1Color,
        Allow3BtStart,
        LaserInputSmoothing,

        // Input device setting per element
        LaserInputDevice,
        ButtonInputDevice,

        // Mouse settings (primary axes are x=0, y=1)
        Mouse_Laser0Axis,
        Mouse_Laser1Axis,
        Mouse_Sensitivity,

        // Key bindings
        Key_Start,
        Key_Back,
        Key_StartAlt,
        Key_BackAlt,
        Key_BT0,
        Key_BT1,
        Key_BT2,
        Key_BT3,
        Key_BT0Alt,
        Key_BT1Alt,
        Key_BT2Alt,
        Key_BT3Alt,
        Key_FX0,
        Key_FX1,
        Key_FX0Alt,
        Key_FX1Alt,
        Key_Laser0Pos,
        Key_Laser1Pos,
        Key_Laser0Neg,
        Key_Laser1Neg,
        Key_Laser0PosAlt,
        Key_Laser1PosAlt,
        Key_Laser0NegAlt,
        Key_Laser1NegAlt,
        Key_Sensitivity,
        Key_LaserReleaseTime,

        // Controller bindings
        Controller_Start,
        Controller_Back,
        Controller_BT0,
        Controller_BT1,
        Controller_BT2,
        Controller_BT3,
        Controller_FX0,
        Controller_FX1,
        Controller_Laser0Axis,
        Controller_Laser1Axis,
        Controller_Deadzone,
        Controller_Sensitivity
    }
#endif

    public enum HiSpeedMod
    {
        Default = 0,
        MMod, CMod,
    }

    [ConfigGroup("NeuroSonic")]
    public static class NscConfig// : Config<NscConfigKey>
    {
        [Config] public static float HiSpeed { get; set; } = 1;
        [Config] public static int VideoOffset { get; set; }
        [Config] public static int InputOffset { get; set; }
        [Config] public static HiSpeedMod HiSpeedModKind { get; set; }
        [Config] public static float ModSpeed { get; set; } = 300;
        [Config] public static float LaserSensitivity { get; set; } = 1.0f;
        [Config] public static int Laser0Color { get; set; } = 200;
        [Config] public static int Laser1Color { get; set; } = 300;
        [Config(Name = "3 BT + Start = Back")]
        public static bool Allow3BtStart { get; set; } = false;
        [Config] public static int LaserInputSmoothing { get; set; } = 2;

#if false
        protected override void SetDefaults()
        {
            Set(NscConfigKey.HiSpeed, 1.0f);
            Set(NscConfigKey.VideoOffset, 0);
            Set(NscConfigKey.InputOffset, 0);
            Set(NscConfigKey.HiSpeedModKind, HiSpeedMod.Default);
            Set(NscConfigKey.ModSpeed, 300.0f);
            Set(NscConfigKey.StandaloneChartsDirectory, "charts");
            Set(NscConfigKey.FileBrowserLastDirectory, "");
            Set(NscConfigKey.Skin, "Default");
            Set(NscConfigKey.Laser0Color, 200);
            Set(NscConfigKey.Laser1Color, 300);
            Set(NscConfigKey.Allow3BtStart, false);
            Set(NscConfigKey.LaserInputSmoothing, 2);

            // TODO(local): change this to Keyboard for both by default
            // Input settings
            Set(NscConfigKey.ButtonInputDevice, InputDevice.Controller);
            Set(NscConfigKey.LaserInputDevice, InputDevice.Controller);

            // Default keyboard bindings
            Set(NscConfigKey.Key_Start, KeyCode.D1); // Start button on Dao controllers
            Set(NscConfigKey.Key_Back, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_StartAlt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_BackAlt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_BT0, KeyCode.D);
            Set(NscConfigKey.Key_BT1, KeyCode.F);
            Set(NscConfigKey.Key_BT2, KeyCode.J);
            Set(NscConfigKey.Key_BT3, KeyCode.K);
            Set(NscConfigKey.Key_BT0Alt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_BT1Alt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_BT2Alt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_BT3Alt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_FX0, KeyCode.C);
            Set(NscConfigKey.Key_FX1, KeyCode.M);
            Set(NscConfigKey.Key_FX0Alt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_FX1Alt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_Laser0Neg, KeyCode.W);
            Set(NscConfigKey.Key_Laser0Pos, KeyCode.E);
            Set(NscConfigKey.Key_Laser1Neg, KeyCode.O);
            Set(NscConfigKey.Key_Laser1Pos, KeyCode.P);
            Set(NscConfigKey.Key_Laser0NegAlt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_Laser0PosAlt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_Laser1NegAlt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_Laser1PosAlt, KeyCode.UNKNOWN);
            Set(NscConfigKey.Key_Sensitivity, 1.0f);
            Set(NscConfigKey.Key_LaserReleaseTime, 0.0f);

            // Default controller settings
            Set(NscConfigKey.Controller_Start, 0);
            Set(NscConfigKey.Controller_Back, -1);
            Set(NscConfigKey.Controller_BT0, 1);
            Set(NscConfigKey.Controller_BT1, 2);
            Set(NscConfigKey.Controller_BT2, 3);
            Set(NscConfigKey.Controller_BT3, 4);
            Set(NscConfigKey.Controller_FX0, 5);
            Set(NscConfigKey.Controller_FX1, 6);
            Set(NscConfigKey.Controller_Laser0Axis, 0);
            Set(NscConfigKey.Controller_Laser1Axis, 1);
            Set(NscConfigKey.Controller_Sensitivity, 1.0f);
            Set(NscConfigKey.Controller_Deadzone, 0.0f);

            // Default mouse settings
            Set(NscConfigKey.Mouse_Laser0Axis, Axes.X);
            Set(NscConfigKey.Mouse_Laser1Axis, Axes.Y);
            Set(NscConfigKey.Mouse_Sensitivity, 1.0f);
        }
#endif
    }
}
