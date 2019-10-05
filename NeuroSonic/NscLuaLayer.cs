using System;

using theori;
using theori.IO;
using theori.Graphics;
using theori.Graphics.OpenGL;
using theori.Resources;
using theori.Scripting;

using NeuroSonic.IO;
using NeuroSonic.Platform;

using MoonSharp.Interpreter;
using theori.Audio;
using NeuroSonic.Startup;
using NeuroSonic.GamePlay;
using theori.Charting.Playback;
using theori.Charting;
using theori.Charting.Serialization;
using theori.Configuration;
using System.IO;
using System.Linq;
using System.Numerics;

using static MoonSharp.Interpreter.DynValue;
using System.Collections.Generic;

namespace NeuroSonic
{
    class GameSystem
    {
        public readonly ClientResourceLocator ResourceLocator;

        public readonly Chart Chart;

        private readonly AudioTrack m_audioTrack;
        public readonly AudioEffectController AudioEffectController;

        public readonly SlidingChartPlayback InputPlayback;
        public readonly SlidingChartPlayback VisualPlayback;

        public readonly HighwayControl HighwayControl;

        public GameSystem(ClientResourceLocator locator, Chart chart)
        {
            ResourceLocator = locator;
            Chart = chart;

            string chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);
            string audioFile = Path.Combine(chartsDir, chart.SetInfo.FilePath, chart.Info.SongFileName);
            m_audioTrack = AudioTrack.FromFile(audioFile);
            m_audioTrack.Channel = Mixer.MasterChannel;
            m_audioTrack.Volume = chart.Info.SongVolume / 100.0f;

            AudioEffectController = new AudioEffectController(8, m_audioTrack, true)
            {
                RemoveFromChannelOnFinish = true,
            };
            AudioEffectController.Position = MathL.Min(0.0, (double)chart.TimeStart - 2);

            InputPlayback = new SlidingChartPlayback(chart, false);
            VisualPlayback = new SlidingChartPlayback(chart, true);

            HighwayControl = new HighwayControl(HighwayControlConfig.CreateDefaultKsh168());
        }
    }

    abstract class NscLuaObjectHandle<T>
    {
        public static implicit operator T(NscLuaObjectHandle<T> handle) => handle.Object;

        protected readonly T Object;

        protected NscLuaObjectHandle(T obj)
        {
            Object = obj;
        }
    }

    class NscChartSetInfoHandle : NscLuaObjectHandle<ChartSetInfo>
    {
        //public static implicit operator NscChartSetInfoHandle(ChartSetInfo audio) => new NscChartSetInfoHandle(audio);

        private readonly ClientResourceManager m_resources;

        private List<NscChartInfoHandle>? m_charts;

        public NscChartSetInfoHandle(ClientResourceManager resources, ChartSetInfo info)
            : base(info)
        {
            m_resources = resources;
        }

        /// <summary>
        /// The database primary key.
        /// </summary>
        public long ID => Object.ID;

        public long LastWriteTime => Object.LastWriteTime;

        public long? OnlineID => Object.OnlineID;

        /// <summary>
        /// Parent path relative to the selected chart storage directory.
        /// </summary>
        public string FilePath => Object.FilePath;

        /// <summary>
        /// The name of the chart set file.
        /// </summary>
        public string FileName => Object.FileName;

        public List<NscChartInfoHandle> Charts => m_charts ?? (m_charts = Object.Charts
            .OrderBy(info => MathL.Clamp(info.DifficultyIndex ?? 0, 0, 4))
            .ThenBy(info => info.DifficultyLevel)
            .ThenBy(info => info.DifficultyName)
            //.OrderBy(info => info.SongTitle)
            .Select(info => new NscChartInfoHandle(m_resources, this, info)).ToList());
    }

    class NscChartInfoHandle : NscLuaObjectHandle<ChartInfo>
    {
        //public static implicit operator NscChartInfoHandle(ChartInfo audio) => new NscChartInfoHandle(audio);

        private readonly ClientResourceManager m_resources;
        private readonly NscChartSetInfoHandle m_setInfo;

        private Texture? m_jacketTexture;

        public NscChartInfoHandle(ClientResourceManager resources, NscChartSetInfoHandle setInfo, ChartInfo info)
            : base(info)
        {
            m_resources = resources;
            m_setInfo = setInfo;
        }

        /// <summary>
        /// The database primary key.
        /// </summary>
        public long ID => Object.ID;

        public long LastWriteTime => Object.LastWriteTime;

        public long SetID => Object.SetID;
        public NscChartSetInfoHandle Set => m_setInfo;

        /// <summary>
        /// The name of the chart file inside of the Set directory.
        /// </summary>
        public string FileName => Object.FileName;

        public string SongTitle => Object.SongTitle;
        public string SongArtist => Object.SongArtist;
        public string SongFileName => Object.SongFileName;
        public int SongVolume => Object.SongVolume;

        public double ChartOffset => Object.ChartOffset.Seconds;

        public string Charter => Object.Charter;

        public string JacketFileName => Object.JacketFileName;
        public string JacketArtist => Object.JacketArtist;

        public string BackgroundFileName => Object.BackgroundFileName;
        public string BackgroundArtist => Object.BackgroundArtist;

        public double DifficultyLevel => Object.DifficultyLevel;
        public int DifficultyIndex => 1 + MathL.Clamp(Object.DifficultyIndex ?? 0, 0, 4);

        public string DifficultyName => Object.DifficultyName;
        public string DifficultyNameShort => Object.DifficultyNameShort;

        public DynValue DifficultyColor => Object.DifficultyColor is { } c ?
            NewTuple(NewNumber(255 * c.X), NewNumber(255 * c.Y), NewNumber(255 * c.Z)) :
            NewTuple(NewNumber(255), NewNumber(255), NewNumber(255));

        public double ChartDuration => Object.ChartDuration.Seconds;

        public string Tags => Object.Tags;

        public Texture GetJacketTexture()
        {
            if (m_jacketTexture is { } result) return result;

            m_jacketTexture = Texture.Empty;
            try
            {
                string chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);
                string texturePath = Path.Combine(chartsDir, Object.Set.FilePath, Object.JacketFileName);

                Texture? actualTexture = null;
                actualTexture = m_resources.LoadTexture(File.OpenRead(texturePath), Path.GetExtension(Object.JacketFileName), () =>
                {
                    m_jacketTexture = actualTexture!;
                    m_resources.Manage(actualTexture!);
                });
            }
            catch (Exception) { }

            return m_jacketTexture;
        }
    }

    class NscAudioHandle : NscLuaObjectHandle<AudioTrack>
    {
        public static implicit operator NscAudioHandle(AudioTrack audio) => new NscAudioHandle(audio);

        public NscAudioHandle(AudioTrack audio)
            : base(audio)
        {
        }
    }

    class NscChartHandle : NscLuaObjectHandle<Chart>
    {
        public static implicit operator NscChartHandle(Chart chart) => new NscChartHandle(chart);

        public NscChartHandle(Chart chart)
            : base(chart)
        {
        }
    }

    class NscGameSystemHandle : NscLuaObjectHandle<GameSystem>
    {
        public static implicit operator NscGameSystemHandle(GameSystem system) => new NscGameSystemHandle(system);

        public NscGameSystemHandle(GameSystem system)
            : base(system)
        {
        }

        public NscHighwayHandle CreateHighway() => new HighwayView(Object.ResourceLocator, Object.VisualPlayback);

        public bool DoAsyncLoad()
        {
            return true;
        }

        public bool DoAsyncFinalize()
        {
            return true;
        }

        public void Update(float delta, float total)
        {
        }

        public void ApplyToHighway(NscHighwayHandle handle)
        {
            HighwayView highway = handle;
        }

        public void ApplyToBackground(DynValue bgTable)
        {
            Table background = bgTable.Table;
        }
    }

    class NscPlaybackHandle : NscLuaObjectHandle<SlidingChartPlayback>
    {
        public static implicit operator NscPlaybackHandle(SlidingChartPlayback playback) => new NscPlaybackHandle(playback);

        public NscPlaybackHandle(SlidingChartPlayback playback)
            : base(playback)
        {
        }
    }

    class NscHighwayHandle : NscLuaObjectHandle<HighwayView>
    {
        public static implicit operator NscHighwayHandle(HighwayView highway) => new NscHighwayHandle(highway);

        public float HorizonHeight => Object.HorizonHeight;

        public NscHighwayHandle(HighwayView highway)
            : base(highway)
        {
        }

        public void SetViewport(float x, float y, float size)
        {
            Object.Viewport = ((int)x, (int)y, (int)size);
        }

        public bool DoAsyncLoad() => Object.AsyncLoad();
        public bool DoAsyncFinalize() => Object.AsyncFinalize();

        public void Update(float delta, float total)
        {
            Object.Update();
        }

        public void Render()
        {
            Object.Render();
        }
    }

    class NscResourceLoader
    {

    }

    public sealed class NscLuaLayer : Layer, IControllerInputLayer
    {
        static NscLuaLayer()
        {
            LuaScript.RegisterType<NscAudioHandle>();
            LuaScript.RegisterType<NscChartSetInfoHandle>();
            LuaScript.RegisterType<NscChartInfoHandle>();
            LuaScript.RegisterType<NscChartHandle>();
            LuaScript.RegisterType<NscGameSystemHandle>();
            LuaScript.RegisterType<NscHighwayHandle>();
        }

        private readonly ClientResourceLocator m_locator;
        private readonly ClientResourceManager m_resources;

        private readonly BasicSpriteRenderer m_spriteRenderer;

        private readonly string m_scriptFileName;
        private readonly LuaScript m_script;
        private readonly DynValue[] m_scriptArgs;

        private readonly Table m_tbl_nsc;

        private readonly Table m_tbl_nsc_audio;
        private readonly Table m_tbl_nsc_charts;
        private readonly Table m_tbl_nsc_config;
        private readonly Table m_tbl_nsc_game;
        private readonly Table m_tbl_nsc_graphics;
        private readonly Table m_tbl_nsc_input;

        private readonly Table m_tbl_nsc_input_keyboard;
        private readonly ScriptEvent m_evt_keyboard_pressed;
        private readonly ScriptEvent m_evt_keyboard_released;

        private readonly Table m_tbl_nsc_input_mouse;
        private readonly ScriptEvent m_evt_mouse_pressed;
        private readonly ScriptEvent m_evt_mouse_released;
        private readonly ScriptEvent m_evt_mouse_moved;
        private readonly ScriptEvent m_evt_mouse_scrolled;

        private readonly Table m_tbl_nsc_input_controller;
        private readonly Table m_tbl_nsc_input_controller_axisPartialTick;
        private readonly ScriptEvent m_evt_controller_pressed;
        private readonly ScriptEvent m_evt_controller_released;
        private readonly ScriptEvent m_evt_controller_axisChanged;
        private readonly ScriptEvent m_evt_controller_axisTicked;

        private readonly Table m_tbl_nsc_layer;

        private bool m_hasControllerPriority = true;

        private readonly float[] m_controllerAxisMotion = new float[2];
        private const float ControllerAxisMotionMultiplier = 3.0f;

        public override int TargetFrameRate
        {
            get
            {
                return base.TargetFrameRate;
            }
        }

        public NscLuaLayer(string scriptFileName, params DynValue[] args)
        {
            m_locator = ClientSkinService.CurrentlySelectedSkin;
            m_resources = new ClientResourceManager(m_locator);

            m_spriteRenderer = new BasicSpriteRenderer(m_locator);

            m_scriptFileName = scriptFileName;
            m_script = new LuaScript();
            m_scriptArgs = args;

            ((Table)m_script["table"])["shallowCopy"] = (Func<DynValue, DynValue>)(table =>
            {
                var result = m_script.NewTable();
                foreach (var pair in table.Table.Pairs)
                    result[pair.Key] = pair.Value;
                return NewTable(result);
            });

            m_script["KeyCode"] = typeof(KeyCode);
            m_script["MouseButton"] = typeof(MouseButton);
            m_script["ControllerInput"] = typeof(ControllerInput);

            m_script["include"] = (Func<string, DynValue>)Include_LuaFile;

            m_script["nsc"] = m_tbl_nsc = m_script.NewTable();

            m_tbl_nsc["openCurtain"] = (Action)OpenCurtain;
            m_tbl_nsc["closeCurtain"] = (Action<float, DynValue?>)((duration, callback) =>
            {
                Action? onClosed = (callback == null || callback == DynValue.Nil) ? (Action?)null : () => m_script.Call(callback!);
                if (duration <= 0)
                    CloseCurtain(onClosed);
                else CloseCurtain(duration, onClosed);
            });

            m_tbl_nsc["audio"] = m_tbl_nsc_audio = m_script.NewTable();

            m_tbl_nsc_audio["queueStaticAudioLoad"] = (Func<string, NscAudioHandle>)(audioName => ClientAs<NscClient>().StaticResources.QueueAudioLoad($"audio/{ audioName }"));
            m_tbl_nsc_audio["queueAudioLoad"] = (Func<string, NscAudioHandle>)(audioName => m_resources.QueueAudioLoad($"audio/{ audioName }"));

            m_tbl_nsc["charts"] = m_tbl_nsc_charts = m_script.NewTable();

            m_tbl_nsc_charts["setDatabaseToClean"] = (Action)(() => ClientAs<NscClient>().DatabaseWorker.SetToClean());
            m_tbl_nsc_charts["setDatabaseToIdle"] = (Action)(() => ClientAs<NscClient>().DatabaseWorker.SetToIdle());
            m_tbl_nsc_charts["setDatabaseToPopulate"] = (Action)(() => ClientAs<NscClient>().DatabaseWorker.SetToPopulate());
            m_tbl_nsc_charts["getDatabaseState"] = (Func<string>)(() => ClientAs<NscClient>().DatabaseWorker.State.ToString());

            m_tbl_nsc_charts["getChartSets"] = (Func<List<NscChartSetInfoHandle>>)(() => ClientAs<NscClient>().DatabaseWorker.ChartSets.Select(info => new NscChartSetInfoHandle(m_resources, info)).ToList());
            //m_tbl_nsc_charts["getCharts"] = (Func<List<NscChartInfoHandle>>)(() => ClientAs<NscClient>().DatabaseWorker.Charts.Select(info => (NscChartInfoHandle)info).ToList());

            //m_tbl_nsc_charts["getChartInfo"] = (Func<NscChartInfoHandle, NscChartHandle>)(chartInfo =>
            m_tbl_nsc_charts["getChartInfo"] = (Func<NscChartHandle>)(() =>
            {
                string chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);

#if false
                var setSerializer = new ChartSetSerializer();
                var chartSetInfo = setSerializer.LoadFromFile(chartsDir, "", ".theori-set");
#endif

                var chartInfo = getTempChartInfo();

                var serializer = new ChartSerializer(chartsDir, NeuroSonicGameMode.Instance);
                var chart = serializer.LoadFromFile(chartInfo);

                return chart;
            });

            //m_tbl_nsc_charts["loadChart"] = (Func<NscChartInfoHandle, NscChartHandle>)(chartInfo =>
            m_tbl_nsc_charts["loadChart"] = (Func<NscChartHandle>)(() =>
            {
                string chartsDir = Plugin.Config.GetString(NscConfigKey.StandaloneChartsDirectory);

                var chartInfo = getTempChartInfo();

                var serializer = new ChartSerializer(chartsDir, NeuroSonicGameMode.Instance);
                var chart = serializer.LoadFromFile(chartInfo);

                return chart;
            });

            m_tbl_nsc["config"] = m_tbl_nsc_config = m_script.NewTable();

            m_tbl_nsc_config["getInt"] = (Func<string, int>)(configName =>
            {
                if (Enum.TryParse(typeof(NscConfigKey), configName, out object key))
                    return Plugin.Config.GetInt((NscConfigKey)key);
                else if (Enum.TryParse(typeof(TheoriConfigKey), configName, out key))
                    return Host.Config.GetInt((TheoriConfigKey)key);
                // TODO(local): raise lua error
                return 0;
            });

            m_tbl_nsc["game"] = m_tbl_nsc_game = m_script.NewTable();

            ChartInfo getTempChartInfo()
            {
                var sets = ClientAs<NscClient>().DatabaseWorker.ChartSets;
                var chartSetInfo = sets.ElementAt(new Random().Next(sets.Count()));
                return chartSetInfo.Charts.Aggregate((a, b) => a.DifficultyLevel > b.DifficultyLevel ? a : b);
            }

            m_tbl_nsc_game["exit"] = (Action)(() => Host.Exit());
            m_tbl_nsc_game["pushDebugMenu"] = (Action)(() => Push(new NeuroSonicStandaloneStartup()));
            m_tbl_nsc_game["pushGameplay"] = (Action<NscChartInfoHandle>)(chartInfo => Push(new GameLayer(m_locator, chartInfo, AutoPlay.None)));
            m_tbl_nsc_game["newGameSystem"] = (Func<NscChartHandle, NscGameSystemHandle>)(chart =>
            {
                return new GameSystem(m_locator, chart);
            });

            m_tbl_nsc["graphics"] = m_tbl_nsc_graphics = m_script.NewTable();
            
            m_tbl_nsc_graphics["queueStaticTextureLoad"] = (Func<string, Texture>)(textureName => ClientAs<NscClient>().StaticResources.QueueTextureLoad($"textures/{ textureName }"));
            m_tbl_nsc_graphics["getStaticTexture"] = (Func<string, Texture>)(textureName => ClientAs<NscClient>().StaticResources.GetTexture($"textures/{ textureName }"));
            m_tbl_nsc_graphics["doStaticTextureLoadsAsync"] = (Func<bool>)(() => ClientAs<NscClient>().StaticResources.LoadAll());
            m_tbl_nsc_graphics["finalizeStaticTextureLoads"] = (Func<bool>)(() => ClientAs<NscClient>().StaticResources.FinalizeLoad());
            m_tbl_nsc_graphics["queueTextureLoad"] = (Func<string, Texture>)(textureName => m_resources.QueueTextureLoad($"textures/{ textureName }"));
            m_tbl_nsc_graphics["getTexture"] = (Func<string, Texture>)(textureName => m_resources.GetTexture($"textures/{ textureName }"));
            m_tbl_nsc_graphics["flush"] = (Action)(() => m_spriteRenderer.Flush());
            m_tbl_nsc_graphics["saveTransform"] = (Action)(() => m_spriteRenderer.SaveTransform());
            m_tbl_nsc_graphics["restoreTransform"] = (Action)(() => m_spriteRenderer.RestoreTransform());
            m_tbl_nsc_graphics["resetTransform"] = (Action)(() => m_spriteRenderer.ResetTransform());
            m_tbl_nsc_graphics["translate"] = (Action<float, float>)((x, y) => m_spriteRenderer.Translate(x, y));
            m_tbl_nsc_graphics["rotate"] = (Action<float>)(d => m_spriteRenderer.Rotate(d));
            m_tbl_nsc_graphics["scale"] = (Action<float, float>)((x, y) => m_spriteRenderer.Scale(x, y));
            m_tbl_nsc_graphics["shear"] = (Action<float, float>)((x, y) => m_spriteRenderer.Shear(x, y));
            m_tbl_nsc_graphics["getViewportSize"] = (Func<DynValue>)(() => DynValue.NewTuple(DynValue.NewNumber(Window.Width), DynValue.NewNumber(Window.Height)));
            m_tbl_nsc_graphics["setColor"] = (Action<float, float, float, float>)((r, g, b, a) => m_spriteRenderer.SetColor(r, g, b, a));
            m_tbl_nsc_graphics["setImageColor"] = (Action<float, float, float, float>)((r, g, b, a) => m_spriteRenderer.SetImageColor(r, g, b, a));
            m_tbl_nsc_graphics["fillRect"] = (Action<float, float, float, float>)((x, y, w, h) => m_spriteRenderer.FillRect(x, y, w, h));
            m_tbl_nsc_graphics["draw"] = (Action<Texture, float, float, float, float>)((texture, x, y, w, h) => m_spriteRenderer.Image(texture, x, y, w, h));
            m_tbl_nsc_graphics["setFontSize"] = (Action<float>)(size => m_spriteRenderer.SetFontSize(size));
            m_tbl_nsc_graphics["setTextAlign"] = (Action<Anchor>)(align => m_spriteRenderer.SetTextAlign(align));
            m_tbl_nsc_graphics["drawString"] = (Action<string, float, float>)((text, x, y) => m_spriteRenderer.Write(text, x, y));

            m_tbl_nsc_graphics["newHighway"] = (Func<NscHighwayHandle>)(() =>
            {
                var highway = new HighwayView(m_locator, null);
                m_resources.Manage(highway);
                return highway;
            });

            m_tbl_nsc["input"] = m_tbl_nsc_input = m_script.NewTable();

            m_tbl_nsc_input["setControllerPriority"] = (Action<bool>)(priority => m_hasControllerPriority = priority);

            m_tbl_nsc_input["keyboard"] = m_tbl_nsc_input_keyboard = m_script.NewTable();

            m_tbl_nsc_input_keyboard["pressed"] = m_evt_keyboard_pressed = m_script.NewEvent();
            m_tbl_nsc_input_keyboard["released"] = m_evt_keyboard_released = m_script.NewEvent();

            m_tbl_nsc_input["mouse"] = m_tbl_nsc_input_mouse = m_script.NewTable();

            m_tbl_nsc_input_mouse["pressed"] = m_evt_mouse_pressed = m_script.NewEvent();
            m_tbl_nsc_input_mouse["released"] = m_evt_mouse_released = m_script.NewEvent();
            m_tbl_nsc_input_mouse["moved"] = m_evt_mouse_moved = m_script.NewEvent();
            m_tbl_nsc_input_mouse["scrolled"] = m_evt_mouse_scrolled = m_script.NewEvent();

            m_tbl_nsc_input["controller"] = m_tbl_nsc_input_controller = m_script.NewTable();

            m_tbl_nsc_input_controller["axisPartialTick"] = m_tbl_nsc_input_controller_axisPartialTick = m_script.NewTable(); ;
            m_tbl_nsc_input_controller_axisPartialTick[ControllerInput.Laser0Axis] = 0.0f;
            m_tbl_nsc_input_controller_axisPartialTick[ControllerInput.Laser1Axis] = 0.0f;

            m_tbl_nsc_input_controller["pressed"] = m_evt_controller_pressed = m_script.NewEvent();
            m_tbl_nsc_input_controller["released"] = m_evt_controller_released = m_script.NewEvent();
            m_tbl_nsc_input_controller["axisChanged"] = m_evt_controller_axisChanged = m_script.NewEvent();
            m_tbl_nsc_input_controller["axisTicked"] = m_evt_controller_axisTicked = m_script.NewEvent();
            m_tbl_nsc_input_controller["isDown"] = (Func<ControllerInput, bool>)Input.IsButtonDown;

            m_tbl_nsc["layer"] = m_tbl_nsc_layer = m_script.NewTable();

            m_tbl_nsc_layer["construct"] = (Action)(() => { });
            m_tbl_nsc_layer["push"] = DynValue.NewCallback((context, args) =>
            {
                if (args.Count == 0) return DynValue.Nil;

                string layerPath = args.AsStringUsingMeta(context, 0, "push");
                DynValue[] rest = args.GetArray(1);

                Push(new NscLuaLayer(layerPath, rest));
                return DynValue.Nil;
            });
            m_tbl_nsc_layer["pop"] = (Action)(() => Pop());
            m_tbl_nsc_layer["setInvalidForResume"] = (Action)(() => SetInvalidForResume());
            m_tbl_nsc_layer["doAsyncLoad"] = (Func<bool>)(() => true);
            m_tbl_nsc_layer["doAsyncFinalize"] = (Func<bool>)(() => true);
            m_tbl_nsc_layer["init"] = (Action)(() => { OpenCurtain(); });
            m_tbl_nsc_layer["destroy"] = (Action)(() => { });
            m_tbl_nsc_layer["suspended"] = (Action)(() => { });
            m_tbl_nsc_layer["resumed"] = (Action)(() => { OpenCurtain(); });
            m_tbl_nsc_layer["onExiting"] = (Action)(() => { });
            m_tbl_nsc_layer["onClientSizeChanged"] = (Action<int, int, int, int>)((x, y, w, h) => { });
            m_tbl_nsc_layer["update"] = (Action<float, float>)((delta, total) => { });
            m_tbl_nsc_layer["render"] = (Action)(() => { });
        }

        private void CloseCurtain(float holdTime, Action? onClosed = null) => ClientAs<NscClient>().CloseCurtain(holdTime, onClosed);
        private void CloseCurtain(Action? onClosed = null) => ClientAs<NscClient>().CloseCurtain(onClosed);
        private void OpenCurtain() => ClientAs<NscClient>().OpenCurtain();

        private DynValue Include_LuaFile(string fileName)
        {
            return m_script.LoadFile(m_locator.OpenFileStream($"scripts/{ fileName }.lua"));
        }

        public override bool AsyncLoad()
        {
            Include_LuaFile(m_scriptFileName);
            m_script.Call(m_tbl_nsc_layer["construct"], m_scriptArgs);

            var result = m_script.Call(m_tbl_nsc_layer["doAsyncLoad"]);

            if (result == null) return true; // guard against function missing
            if (!result.CastToBool()) return false;

            if (!m_resources.LoadAll()) return false;
            return true;
        }

        public override bool AsyncFinalize()
        {
            var result = m_script.Call(m_tbl_nsc_layer["doAsyncFinalize"]);

            if (result == null) return true; // guard against function missing
            if (!result.CastToBool()) return false;

            if (!m_resources.FinalizeLoad()) return false;
            return true;
        }

        public override void Initialize()
        {
            Input.Register(this);

            m_script.Call(m_tbl_nsc_layer["init"]);
        }

        public override void Destroy()
        {
            Input.UnRegister(this);

            m_script.Call(m_tbl_nsc_layer["destroy"]);
        }

        public override void Suspended(Layer nextLayer)
        {
            base.Suspended(nextLayer);

            m_script.Call(m_tbl_nsc_layer["suspended"]);
        }

        public override void Resumed(Layer previousLayer)
        {
            base.Resumed(previousLayer);

            m_script.Call(m_tbl_nsc_layer["resumed"]);
        }

        public override bool OnExiting(Layer? source)
        {
            var result = m_script.Call(m_tbl_nsc_layer["onExiting"]);
            if (result == null) return false;

            return result.CastToBool();
        }

        public override void ClientSizeChanged(int width, int height)
        {
            base.ClientSizeChanged(width, height);

            m_script.Call(m_tbl_nsc_layer["onClientSizeChanged"], width, height);
        }

        public override bool KeyPressed(KeyInfo info)
        {
            if (m_hasControllerPriority) return false;

            m_evt_keyboard_pressed.Fire(info.KeyCode);
            return true;
        }

        public override bool KeyReleased(KeyInfo info)
        {
            if (m_hasControllerPriority) return false;

            m_evt_keyboard_released.Fire(info.KeyCode);
            return true;
        }

        public override bool MouseButtonPressed(MouseButtonInfo info)
        {
            if (m_hasControllerPriority) return false;

            m_evt_mouse_pressed.Fire(info.Button);
            return true;
        }

        public override bool MouseButtonReleased(MouseButtonInfo info)
        {
            if (m_hasControllerPriority) return false;

            m_evt_mouse_released.Fire(info.Button);
            return true;
        }

        public override bool MouseMoved(int x, int y, int dx, int dy)
        {
            if (m_hasControllerPriority) return false;

            m_evt_mouse_moved.Fire(x, y, dx, dy);
            return true;
        }

        public override bool MouseWheelScrolled(int x, int y)
        {
            if (m_hasControllerPriority) return false;

            m_evt_mouse_scrolled.Fire(x, y);
            return true;
        }

        public bool ControllerButtonPressed(ControllerInput input)
        {
            if (!m_hasControllerPriority) return false;

            m_evt_controller_pressed.Fire(input);
            return true;
        }

        public bool ControllerButtonReleased(ControllerInput input)
        {
            if (!m_hasControllerPriority) return false;

            m_evt_controller_released.Fire(input);
            return true;
        }

        public bool ControllerAxisChanged(ControllerInput input, float delta)
        {
            if (!m_hasControllerPriority) return false;

            m_controllerAxisMotion[input - ControllerInput.Laser0Axis] += delta * ControllerAxisMotionMultiplier;
            ref float value = ref m_controllerAxisMotion[input - ControllerInput.Laser0Axis];

            while (value <= -1)
            {
                m_evt_controller_axisTicked.Fire(input, -1);
                value += 1;
            }
            while (value >= 1)
            {
                m_evt_controller_axisTicked.Fire(input, 1);
                value -= 1;
            }

            m_tbl_nsc_input_controller_axisPartialTick[input] = value;
            m_evt_controller_axisChanged.Fire(input, delta);
            return true;
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            m_resources.Update();

            for (int i = 0; i < 2; i++)
            {
                ref float value = ref m_controllerAxisMotion[i];
                if (value < 0)
                    value = MathL.Min(value + delta * 2, 0);
                else value = MathL.Max(value - delta * 2, 0);
                m_tbl_nsc_input_controller_axisPartialTick[ControllerInput.Laser0Axis + i] = value;
            }

            m_script.Call(m_tbl_nsc_layer["update"], delta, total);
        }

        public override void FixedUpdate(float delta, float total)
        {
            base.FixedUpdate(delta, total);
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
        }

        public override void Render()
        {
            base.Render();

            m_spriteRenderer.BeginFrame();
            m_script.Call(m_tbl_nsc_layer["render"]);
            m_spriteRenderer.EndFrame();
        }

        public override void LateRender()
        {
            base.LateRender();
        }
    }
}
