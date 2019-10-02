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

namespace NeuroSonic
{
    class NscAudioHandle
    {
        public static implicit operator NscAudioHandle(AudioTrack audio) => new NscAudioHandle(audio);
        public static implicit operator AudioTrack(NscAudioHandle handle) => handle.m_handle;

        private readonly AudioTrack m_handle;

        public NscAudioHandle(AudioTrack target)
        {
            m_handle = target;
        }
    }
    
    class NscHighwayHandle
    {
        public static implicit operator NscHighwayHandle(HighwayView highway) => new NscHighwayHandle(highway);
        public static implicit operator HighwayView(NscHighwayHandle handle) => handle.m_highway;

        private readonly HighwayView m_highway;

        public float HorizonHeight => m_highway.HorizonHeight;

        public NscHighwayHandle(HighwayView highway)
        {
            m_highway = highway;
        }

        public void SetViewport(float x, float y, float size)
        {
            m_highway.Viewport = ((int)x, (int)y, (int)size);
        }

        public bool DoAsyncLoad() => m_highway.AsyncLoad();
        public bool DoAsyncFinalize() => m_highway.AsyncFinalize();

        public void Update(float delta, float total)
        {
            m_highway.Update();
        }

        public void Render()
        {
            m_highway.Render();
        }
    }

    public sealed class NscLuaLayer : Layer, IControllerInputLayer
    {
        static NscLuaLayer()
        {
            LuaScript.RegisterType<NscAudioHandle>();
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
        private const float ControllerAxisMotionMultiplier = 1.0f;

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

            m_tbl_nsc_audio["loadStaticAudioAsync"] = (Func<string, NscAudioHandle>)(audioName => ClientAs<NscClient>().StaticResources.QueueAudioLoad($"audio/{ audioName }"));
            m_tbl_nsc_audio["loadAudioAsync"] = (Func<string, NscAudioHandle>)(audioName => m_resources.QueueAudioLoad($"audio/{ audioName }"));

            m_tbl_nsc["charts"] = m_tbl_nsc_charts = m_script.NewTable();

            m_tbl_nsc_charts["newHighway"] = (Func<NscHighwayHandle>)(() =>
            {
                var highway = new HighwayView(m_locator, null);
                m_resources.Manage(highway);
                return highway;
            });

            m_tbl_nsc["game"] = m_tbl_nsc_game = m_script.NewTable();

            m_tbl_nsc_game["exit"] = (Action)(() => Host.Exit());
            m_tbl_nsc_game["pushDebugMenu"] = (Action)(() => Push(new NeuroSonicStandaloneStartup()));
            m_tbl_nsc_game["newPlayback"] = (Func<SlidingChartPlayback, HighwayView>)(playback =>
            {
                var highway = new HighwayView(m_locator, playback);
                return highway;
            });
            m_tbl_nsc_game["newHighway"] = (Func<NscHighwayHandle>)(() =>
            {
                var highway = new HighwayView(m_locator, null);
                m_resources.Manage(highway);
                return highway;
            });

            m_tbl_nsc["graphics"] = m_tbl_nsc_graphics = m_script.NewTable();
            
            m_tbl_nsc_graphics["loadStaticTextureAsync"] = (Func<string, Texture>)(textureName => ClientAs<NscClient>().StaticResources.QueueTextureLoad($"textures/{ textureName }"));
            m_tbl_nsc_graphics["getStaticTexture"] = (Func<string, Texture>)(textureName => ClientAs<NscClient>().StaticResources.GetTexture($"textures/{ textureName }"));
            m_tbl_nsc_graphics["doStaticTextureLoadsAsync"] = (Func<bool>)(() => ClientAs<NscClient>().StaticResources.LoadAll());
            m_tbl_nsc_graphics["finalizeStaticTextureLoads"] = (Func<bool>)(() => ClientAs<NscClient>().StaticResources.FinalizeLoad());
            m_tbl_nsc_graphics["loadTextureAsync"] = (Func<string, Texture>)(textureName => m_resources.QueueTextureLoad($"textures/{ textureName }"));
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
