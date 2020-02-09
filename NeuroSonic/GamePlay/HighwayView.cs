#nullable disable

using System;
using System.Collections.Generic;
using System.Numerics;

using theori;
using theori.Charting;
using theori.Charting.Playback;
using theori.Graphics;
using theori.Graphics.OpenGL;
using theori.Resources;

using NeuroSonic.Charting;

namespace NeuroSonic.GamePlay
{
    using EntityMap = Dictionary<Entity, ObjectRenderable3D>;

    public class HighwayView : Disposable, IAsyncLoadable
    {
        class KeyBeamInfo
        {
            public float Alpha;
            public Vector3 Color;
            public bool Held;
        }

        class GlowInfo
        {
            public Entity Object;
            public float Glow;
            public int GlowState;
        }

        private const float LENGTH_BASE = 11;
        private const float LENGTH_ADD = 1.1f;

        private float m_pitch, m_zoom; // "top", "bottom"
        public float CritScreenY = 0.1f;

        public readonly BasicCamera Camera;
        public Transform DefaultTransform { get; private set; }
        public Transform DefaultZoomedTransform { get; private set; }
        public Transform WorldTransform { get; private set; }
        public Transform CritLineTransform { get; private set; }

        private readonly ClientResourceManager m_resources;

        private ObjectRenderable3DStaticResources m_obj3dResources;

        private Texture highwayTexture, keyBeamTexture;
        private Texture entryTexture, exitTexture;

        private Material basicMaterial;
        private Material highwayMaterial;
        private Material laserMaterial, laserEntryMaterial;

        private readonly Drawable3D[] m_highwayDrawables = new Drawable3D[6];
        private readonly Dictionary<HybridLabel, Drawable3D> m_keyBeamDrawables = new Dictionary<HybridLabel, Drawable3D>();
        private Drawable3D m_lVolEntryDrawable, m_lVolExitDrawable;
        private Drawable3D m_rVolEntryDrawable, m_rVolExitDrawable;

        private Vector3 m_lVolColor, m_rVolColor;

        private readonly Dictionary<HybridLabel, EntityMap> m_renderables = new Dictionary<HybridLabel, EntityMap>();
        private readonly Dictionary<HybridLabel, KeyBeamInfo> m_keyBeamInfos = new Dictionary<HybridLabel, KeyBeamInfo>();
        private readonly Dictionary<HybridLabel, GlowInfo> m_glowInfos = new Dictionary<HybridLabel, GlowInfo>();
        private readonly Dictionary<HybridLabel, bool> m_streamsActive = new Dictionary<HybridLabel, bool>();

        public SlidingChartPlayback Playback { get; }

        public float LaserRoll { get; private set; }
        public float CriticalHeight => (1 - CritScreenY) * Camera.ViewportHeight;

        public float HorizonHeight { get; private set; }

        const float SizeScaleHeight = 1.2f; // 0.95f
        //public (int X, int Y, int Size) Viewport { get; set; } = ((Window.Width - Window.Height * 2) / 2, -Window.Height / 2, Window.Height * 2);
        public (int X, int Y, int Size) Viewport { get; set; } = ((int)(Window.Width - Window.Height * SizeScaleHeight) / 2, 0, (int)(Window.Height * SizeScaleHeight));
        //public (int X, int Y, int Size) Viewport { get; set; } = Window.Height > Window.Width ? (0, (Window.Height - Window.Width) / 2, Window.Width) : ((int)(Window.Width - Window.Height * 0.95f) / 2, 0, (int)(Window.Height * 0.95f));

        public float TargetLaserRoll { get; set; }
        public float TargetBaseRoll { get; set; }
        public float TargetEffectRoll { get; set; }

        public float TargetPitch { get; set; }
        public float TargetZoom { get; set; }
        public float TargetOffset { get; set; }
        public float TargetEffectOffset { get; set; }

        private readonly float[] m_splits = new float[5];

        public float Split0 { get => m_splits[0]; set => m_splits[0] = value; }
        public float Split1 { get => m_splits[1]; set => m_splits[1] = value; }
        public float Split2 { get => m_splits[2]; set => m_splits[2] = value; }
        public float Split3 { get => m_splits[3]; set => m_splits[3] = value; }
        public float Split4 { get => m_splits[4]; set => m_splits[4] = value; }

        public bool LasersFillHighway { get; set; } = false;

        public Vector3 CameraOffset { get; set; }

        time_t SlamDurationTime(Entity e) => Playback.Chart.ControlPoints.MostRecent(e.Position).MeasureDuration / 24.0;

        public HighwayView(ClientResourceLocator locator, SlidingChartPlayback playback)
        {
            Playback = playback;

            m_resources = new ClientResourceManager(locator);

            m_lVolColor = Color.HSVtoRGB(new Vector3(NscConfig.Laser0Color / 360.0f, 1, 1));
            m_rVolColor = Color.HSVtoRGB(new Vector3(NscConfig.Laser1Color / 360.0f, 1, 1));

            Camera = new BasicCamera();
            Camera.SetPerspectiveFoV(2 * 60, 1.0f, 0.01f, 1000);

            for (int i = 0; i < 8; i++)
            {
                m_renderables[i] = new EntityMap();
                m_glowInfos[i] = new GlowInfo();
                m_streamsActive[i] = true;

                if (i < 6)
                {
                    m_keyBeamInfos[i] = new KeyBeamInfo();
                    m_keyBeamDrawables[i] = null;
                }
            }
        }

        public bool AsyncLoad()
        {
            m_resources.QueueTextureLoad("textures/game/bt_chip");
            m_resources.QueueTextureLoad("textures/game/bt_chip_sample");
            m_resources.QueueTextureLoad("textures/game/bt_hold");
            m_resources.QueueTextureLoad("textures/game/bt_hold_entry");
            m_resources.QueueTextureLoad("textures/game/bt_hold_exit");

            m_resources.QueueTextureLoad("textures/game/fx_chip");
            m_resources.QueueTextureLoad("textures/game/fx_chip_sample");
            m_resources.QueueTextureLoad("textures/game/fx_hold");
            m_resources.QueueTextureLoad("textures/game/fx_hold_entry");
            m_resources.QueueTextureLoad("textures/game/fx_hold_exit");

            m_resources.QueueTextureLoad("textures/game/laser");

            highwayTexture = m_resources.QueueTextureLoad("textures/game/highway");
            keyBeamTexture = m_resources.QueueTextureLoad("textures/game/key_beam");
            entryTexture = m_resources.QueueTextureLoad("textures/game/laser_entry");
            exitTexture = m_resources.QueueTextureLoad("textures/game/laser_exit");

            basicMaterial = m_resources.QueueMaterialLoad("materials/basic");
            m_resources.QueueMaterialLoad("materials/chip");
            m_resources.QueueMaterialLoad("materials/hold");
            highwayMaterial = m_resources.QueueMaterialLoad("materials/highway");
            laserMaterial = m_resources.QueueMaterialLoad("materials/laser");
            laserEntryMaterial = m_resources.QueueMaterialLoad("materials/laser_entry");

            if (!m_resources.LoadAll())
                return false;

            return true;
        }

        public bool AsyncFinalize()
        {
            if (!m_resources.FinalizeLoad())
                return false;

            m_obj3dResources = new ObjectRenderable3DStaticResources();

            var highwayParams = new MaterialParams();
            highwayParams["LeftColor"] = m_lVolColor;
            highwayParams["RightColor"] = m_rVolColor;
            highwayParams["Hidden"] = 0.0f;

            laserMaterial.BlendMode = BlendMode.Additive;
            laserEntryMaterial.BlendMode = BlendMode.Additive;

            var keyBeamMesh = Mesh.CreatePlane(Vector3.UnitX, Vector3.UnitZ, 1, LENGTH_BASE + LENGTH_ADD, Anchor.BottomCenter);
            m_resources.Manage(keyBeamMesh);

            for (int i = 0; i < 6; i++)
            {
                m_highwayDrawables[i] = new Drawable3D()
                {
                    Texture = highwayTexture,
                    Material = highwayMaterial,
                    Mesh = Mesh.CreatePlane(Vector3.UnitX, Vector3.UnitZ, 1.0f / 6, LENGTH_BASE + LENGTH_ADD, Anchor.BottomCenter, new Rect(i / 6.0f, 0, 1.0f / 6, 1)),
                    Params = highwayParams,
                };
                m_resources.Manage(m_highwayDrawables[i].Mesh);
            }

            for (int i = 0; i < 8; i++)
            {
                m_keyBeamDrawables[i] = new Drawable3D()
                {
                    Texture = keyBeamTexture,
                    Mesh = keyBeamMesh,
                    Material = basicMaterial,
                };
            }

            MaterialParams CreateVolumeParams(int lane)
            {
                var volParams = new MaterialParams();
                volParams["LaserColor"] = lane == 0 ? m_lVolColor : m_rVolColor;
                volParams["HiliteColor"] = new Vector3(1, 1, 0);
                return volParams;
            }

            void CreateVolDrawables(int lane, ref Drawable3D entryDrawable, ref Drawable3D exitDrawable)
            {
                entryDrawable = new Drawable3D()
                {
                    Texture = entryTexture,
                    Mesh = Mesh.CreatePlane(Vector3.UnitX, Vector3.UnitZ, 2 / 6.0f, 1.0f, Anchor.TopCenter),
                    Material = laserEntryMaterial,
                    Params = CreateVolumeParams(lane),
                };
                m_resources.Manage(entryDrawable.Mesh);

                exitDrawable = new Drawable3D()
                {
                    Texture = exitTexture,
                    Mesh = Mesh.CreatePlane(Vector3.UnitX, Vector3.UnitZ, 2 / 6.0f, 1.0f, Anchor.BottomCenter),
                    Material = laserMaterial,
                    Params = CreateVolumeParams(lane),
                };
                m_resources.Manage(exitDrawable.Mesh);
            }

            CreateVolDrawables(0, ref m_lVolEntryDrawable, ref m_lVolExitDrawable);
            CreateVolDrawables(1, ref m_rVolEntryDrawable, ref m_rVolExitDrawable);

            return true;
        }

        protected override void DisposeManaged()
        {
            m_resources.Dispose();
            m_obj3dResources.Dispose();

            foreach (var (_, r) in m_renderables)
            {
                foreach (var obj3d in r.Values)
                    obj3d.Dispose();
                r.Clear();
            }
            m_renderables.Clear();
        }

        public void Reset()
        {
            foreach (var (_, r) in m_renderables)
            {
                foreach (var obj3d in r.Values)
                    obj3d.Dispose();
                r.Clear();
            }
        }

        public void RenderableObjectAppear(Entity obj)
        {
            if (!m_renderables.ContainsKey(obj.Lane)) return;

            if (obj is ButtonEntity bobj)
            {
                if (obj.IsInstant)
                    m_renderables[obj.Lane][obj] = new ButtonChipRenderState3D(bobj, m_resources, m_obj3dResources);
                else m_renderables[obj.Lane][obj] = new ButtonHoldRenderState3D(bobj, m_resources, m_obj3dResources);
            }
            else if (obj is AnalogEntity aobj)
            {
                var color = obj.Lane == 6 ? m_lVolColor : m_rVolColor;
                if (obj.IsInstant)
                    m_renderables[obj.Lane][obj] = new SlamRenderState3D(aobj, color, m_resources);
                else m_renderables[obj.Lane][obj] = new LaserRenderState3D(aobj, color, m_resources);
            }
        }

        public void RenderableObjectDisappear(Entity obj)
        {
            if (!m_renderables.ContainsKey(obj.Lane)) return;

            var lane = m_renderables[obj.Lane];
            if (!lane.ContainsKey(obj)) return;

            m_renderables[obj.Lane][obj].Dispose();
            m_renderables[obj.Lane].Remove(obj);
        }

        public void BeginKeyBeam(HybridLabel lane, Vector3 color)
        {
            m_keyBeamInfos[(int)lane].Alpha = 1.0f;
            m_keyBeamInfos[(int)lane].Color = color;
            m_keyBeamInfos[(int)lane].Held = true;
        }

        public void EndKeyBeam(HybridLabel lane)
        {
            m_keyBeamInfos[(int)lane].Held = false;
        }

        public void SetStreamActive(int stream, bool active)
        {
            m_streamsActive[stream] = active;
        }

        public void SetObjectGlow(Entity targetObject, float glow, int glowState)
        {
            m_glowInfos[targetObject.Lane].Object = targetObject;
            m_glowInfos[targetObject.Lane].Glow = glow;
            m_glowInfos[targetObject.Lane].GlowState = glowState;
        }

        public Vector2 Project(Transform worldTransform, Vector3 worldPosition)
        {
            var p = Camera.ProjectNormalized(worldTransform, worldPosition) * new Vector2(Viewport.Size * 2);
            return new Vector2(Viewport.X - Viewport.Size / 2 + p.X, Viewport.Y - Viewport.Size / 2 + p.Y);
        }

        public void Update()
        {
            for (int i = 0; i < 6; i++)
            {
                const float KEY_BEAM_SPEED = 15.0f;

                var info = m_keyBeamInfos[i];
                if (info.Held) continue;

                info.Alpha = Math.Max(0, info.Alpha - Time.Delta * KEY_BEAM_SPEED);
            }

            Camera.ViewportWidth = Window.Width;
            Camera.ViewportHeight = Window.Height;

            LaserRoll = TargetLaserRoll;
            m_pitch = TargetPitch;
            m_zoom = TargetZoom;
            
            Transform GetAtRoll(float roll, float xOffset)
            {
                const float ANCHOR_ROT = 2.5f;
                const float ANCHOR_Y = -0.7925f;
                const float CONTNR_Z = -0.51f;

                var origin = Transform.RotationZ(roll);
                var anchor = Transform.RotationX(ANCHOR_ROT)
                           * Transform.Translation(xOffset, ANCHOR_Y, 0);
                var contnr = Transform.Translation(0, 0, 0)
                           * Transform.RotationX(m_pitch)
                           * Transform.Translation(0, 0, CONTNR_Z);

                return contnr * anchor * origin;
            }

            var worldNormal = GetAtRoll((TargetBaseRoll + TargetEffectRoll) * 360 + LaserRoll, TargetOffset + TargetEffectOffset);
            var worldNoRoll = GetAtRoll(0, 0);
            var worldCritLine = GetAtRoll(TargetBaseRoll * 360 + LaserRoll, TargetOffset + TargetEffectOffset);

            static Vector3 ZoomDirection(Transform t, out float dist)
            {
                var dir = ((Matrix4x4)t).Translation;
                dist = dir.Length();
                return Vector3.Normalize(dir);
            }

            var zoomDir = ZoomDirection(worldNormal, out float highwayDist);
            var zoomTransform = Transform.Translation(zoomDir * m_zoom * highwayDist);

            DefaultTransform = worldNoRoll;
            DefaultZoomedTransform = worldNoRoll * Transform.Translation(ZoomDirection(worldNoRoll, out float zoomedDist) * m_zoom * zoomedDist);
            WorldTransform = worldNormal * zoomTransform;
            CritLineTransform = worldCritLine;

            var critDir = Vector3.Normalize(((Matrix4x4)worldNoRoll).Translation);
            float rotToCrit = MathL.Atan(critDir.Y, -critDir.Z);

            float cameraRot = Camera.FieldOfView * 0.3405f;
            float cameraPitch = rotToCrit + MathL.ToRadians(cameraRot);

            Camera.Position = CameraOffset;
            Camera.Rotation = Quaternion.CreateFromYawPitchRoll(0, cameraPitch, 0);

            HorizonHeight = Project(Transform.Identity, Camera.Position + new Vector3(0, 0, -1)).Y / Window.Height;

            static Vector3 V3Project(Vector3 a, Vector3 b) => b * (Vector3.Dot(a, b) / Vector3.Dot(b, b));
            static float SignedDistance(Vector3 point, Vector3 ray)
            {
                Vector3 projected = V3Project(point, ray);
                return MathL.Sign(Vector3.Dot(ray, projected)) * projected.Length();
            }

            float minClipDist = float.MaxValue;
            float maxClipDist = float.MinValue;

            Vector3 cameraForward = Vector3.Transform(new Vector3(0, 0, -1), Camera.Rotation);
            for (int i = 0; i < 4; i++)
            {
                float clipDist = SignedDistance(Vector3.Transform(m_clipPoints[i], WorldTransform.Matrix) - Camera.Position, cameraForward);

                minClipDist = Math.Min(minClipDist, clipDist);
                maxClipDist = Math.Max(maxClipDist, clipDist);
            }

            float clipNear = Math.Max(0.01f, minClipDist);
            float clipFar = maxClipDist;

            // TODO(local): see if the default epsilon is enough? There's no easy way to check clip planes manually right now
            if (clipNear.ApproxEq(clipFar))
                clipFar = clipNear + 0.001f;

            Camera.NearDistance = clipNear;
            Camera.FarDistance = clipFar;
        }

        private readonly Vector3[] m_clipPoints = new Vector3[4] { new Vector3(-1, 0, LENGTH_ADD), new Vector3(1, 0, LENGTH_ADD), new Vector3(-1, 0, -LENGTH_BASE), new Vector3(1, 0, -LENGTH_BASE) };

        public void Render()
        {
            var renderState = new RenderState
            {
                //Viewport = (Viewport.X, -Window.Height + Viewport.Y + Viewport.Size, Viewport.Size, Viewport.Size),
                Viewport = (Viewport.X - Viewport.Size / 2, -Window.Height + (Viewport.Y - Viewport.Size / 2) + Viewport.Size * 2, Viewport.Size * 2, Viewport.Size * 2),
                ProjectionMatrix = Camera.ProjectionMatrix,
                CameraMatrix = Camera.ViewMatrix,
            };

            static float GetLanePosition(int lane) => -5 / 12.0f + lane / 6.0f;
            float GetLaneOffset(int lane)
            {
                float center = m_splits[2] / 12;

                float result = 0;

                if (lane <= 2) result -= center;
                if (lane <= 1) result -= m_splits[1] / 6;
                if (lane == 0) result -= m_splits[0] / 6;

                if (lane >= 3) result += center;
                if (lane >= 4) result += m_splits[3] / 6;
                if (lane == 5) result += m_splits[4] / 6;

                return result;
            }

            float highwayWidthScale = 0;
            for (int i = 0; i < m_splits.Length; i++)
                highwayWidthScale += m_splits[i];
            highwayWidthScale = highwayWidthScale / 6.0f + 1;

            using var queue = new RenderQueue(renderState);

            for (int i = 0; i < 6; i++)
            {
                float offset = GetLanePosition(i) + GetLaneOffset(i);
                m_highwayDrawables[i].DrawToQueue(queue, Transform.Translation(offset, 0, LENGTH_ADD) * WorldTransform);
            }

            for (int i = 0; i < 6; i++)
            {
                if (i < 4)
                    RenderKeyBeam(i + 1);
                else
                {
                    RenderKeyBeam(1 + (i - 4) * 2);
                    RenderKeyBeam(2 + (i - 4) * 2);
                }

                void RenderKeyBeam(int lane)
                {
                    float offset = GetLanePosition(lane) + GetLaneOffset(lane);

                    var keyBeamInfo = m_keyBeamInfos[i];
                    var keyBeamDrawable = m_keyBeamDrawables[i];

                    Transform t = Transform.Scale(1.0f / 6, 1, 1) * Transform.Translation(offset, 0, LENGTH_ADD) * WorldTransform;
                    keyBeamDrawable!.Params["Color"] = new Vector4(keyBeamInfo.Color, keyBeamInfo.Alpha * (i < 4 ? 0.8f : 0.2f));
                    keyBeamDrawable.DrawToQueue(queue, t);
                }
            }

            void RenderButtonStream(int i, bool chip)
            {
                foreach (var objr in m_renderables[i].Values)
                {
                    var objrBtn = (IButtonRenderState3D)objr;
                    if (chip != objr.Object.IsInstant) continue;

                    if (i < 4)
                    {
                        objrBtn.SplitDrawMode = 0;
                        DrawObject(objr, i + 1);
                    }
                    else
                    {
                        int lane = 1 + 2 * (i - 4);

                        objrBtn.SplitDrawMode = 1;
                        DrawObject(objr, lane, 1.0f / 12);

                        objrBtn.SplitDrawMode = 2;
                        DrawObject(objr, lane + 1, -1.0f / 12);
                    }

                    void DrawObject(ObjectRenderable3D o, int lane, float xOffset = 0)
                    {
                        float z = LENGTH_BASE * Playback.GetRelativeDistance(o.Object.AbsolutePosition);
                        float zDur = 1;

                        // TODO(local): [CONFIG] Allow user to change the scaling of chips, or use a different texture
                        // TODO(local): change default scaling to take up exactly N degrees of the field of view at all times
                        Transform tDiff = Transform.Identity;
                        if (o.Object.IsInstant)
                        {
                            float distScaling = z / LENGTH_BASE;
                            float widthMult = 1.0f;

                            if ((int)o.Object.Lane < 4)
                            {
                                int fxLaneCheck = 4 + (int)o.Object.Lane / 2;
                                if (o.Object.Chart[fxLaneCheck].TryGetAt(o.Object.Position, out var overlap) && overlap.IsInstant)
                                    widthMult = 0.8f;
                            }

                            tDiff = Transform.Scale(widthMult, 1, 1);
                            zDur = 1 + distScaling;
                        }
                        else
                        {
                            tDiff = Transform.Scale(1, 1, zDur);
                            zDur = LENGTH_BASE * Playback.GetRelativeDistanceFromTime(o.Object.AbsolutePosition, o.Object.AbsoluteEndPosition);
                        }

                        if (o is GlowingRenderState3D glowObj)
                        {
                            if (m_glowInfos[o.Object.Lane].Object == o.Object)
                            {
                                glowObj.Glow = m_glowInfos[o.Object.Lane].Glow;
                                glowObj.GlowState = m_glowInfos[o.Object.Lane].GlowState;
                            }
                            else
                            {
                                glowObj.Glow = 0.0f;
                                glowObj.GlowState = 1;
                            }
                        }

                        float xOffs = GetLanePosition(lane) + GetLaneOffset(lane) + xOffset;
                        Transform t = tDiff * Transform.Translation(xOffs, 0, -z) * WorldTransform;
                        o.Render(queue, t, zDur);
                    }
                }
            }

            void RenderAnalogStream(int i)
            {
                const float HISCALE = 0.1f;

                float horScale = LasersFillHighway ? highwayWidthScale : 1;
                foreach (var objr in m_renderables[i + 6].Values)
                {
                    var analog = (AnalogEntity)objr.Object;
                    var glowObj = (GlowingRenderState3D)objr;

                    if (m_glowInfos[analog.Lane].Object == analog.Head)
                    {
                        glowObj.Glow = m_glowInfos[analog.Lane].Glow;
                        glowObj.GlowState = m_glowInfos[analog.Lane].GlowState;
                    }
                    else
                    {
                        glowObj.Glow = m_streamsActive[analog.Lane] ? 0.0f : -0.5f;
                        glowObj.GlowState = m_streamsActive[analog.Lane] ? 1 : 0;
                    }

                    time_t position = analog.AbsolutePosition;
                    if (analog.PreviousConnected != null && analog.Previous.IsInstant)
                        position += SlamDurationTime(analog);

                    time_t endPosition;
                    if (analog.IsInstant)
                        endPosition = position + SlamDurationTime(analog);
                    else endPosition = objr.Object.AbsoluteEndPosition;

                    float z = LENGTH_BASE * Playback.GetRelativeDistance(position);
                    float zDur = LENGTH_BASE * Playback.GetRelativeDistanceFromTime(position, endPosition);

                    Transform scale = Transform.Scale(horScale, 1, 1 + HISCALE);
                    Transform t = Transform.Translation(0, 0, -z) * scale * WorldTransform;
                    objr.Render(queue, t, zDur);

                    if (objr.Object.PreviousConnected == null)
                    {
                        float laneSpace = 5 / 6.0f;
                        if (analog.RangeExtended) laneSpace *= 2;

                        time_t entryPosition = objr.Object.AbsolutePosition;
                        float zEntryAbs = Playback.GetRelativeDistance(entryPosition);
                        float zEntry = LENGTH_BASE * zEntryAbs;

                        Transform tEntry = Transform.Translation((((AnalogEntity)objr.Object).InitialValue - 0.5f) * laneSpace, 0, -zEntry) * scale * WorldTransform;
                        (i == 0 ? m_lVolEntryDrawable : m_rVolEntryDrawable).DrawToQueue(queue, tEntry);
                    }

                    if (objr.Object.NextConnected == null && objr.Object.IsInstant)
                    {
                        float laneSpace = 5 / 6.0f;
                        if (analog.RangeExtended) laneSpace *= 2;

                        time_t exitPosition = objr.Object.AbsoluteEndPosition;
                        if (objr.Object.IsInstant)
                            exitPosition += SlamDurationTime(objr.Object);

                        float zExitAbs = Playback.GetRelativeDistance(exitPosition);
                        float zExit = LENGTH_BASE * zExitAbs;

                        Transform tExit = Transform.Translation((((AnalogEntity)objr.Object).FinalValue - 0.5f) * laneSpace, 0, -zExit) * scale * WorldTransform;
                        (i == 0 ? m_lVolExitDrawable : m_rVolExitDrawable).DrawToQueue(queue, tExit);
                    }
                }
            }

            for (int i = 0; i < 2; i++) RenderButtonStream(i + 4, false);
            for (int i = 0; i < 4; i++) RenderButtonStream(i, false);

            for (int i = 0; i < 2; i++) RenderAnalogStream(i);

            for (int i = 0; i < 2; i++) RenderButtonStream(i + 4, true);
            for (int i = 0; i < 4; i++) RenderButtonStream(i, true);
        }
    }
}
