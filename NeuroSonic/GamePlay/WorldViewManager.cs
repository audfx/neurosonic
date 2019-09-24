using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using theori;
using theori.Graphics;

namespace NeuroSonic.GamePlay
{
    [Flags]
    public enum WorldViewParams
    {
        Nothing = 0,

        Roll = 1 << 1,
        EffectRoll = 1 << 2,

        Zoom = 1 << 3,
        Pitch = 1 << 4,

        Offset = 1 << 5,
        EffectOffset = 1 << 6,

        Everything = Roll | EffectRoll | Zoom | Pitch | Offset | EffectOffset,
    }

    public sealed class WorldViewManager
    {
        private const float CAMERA_FOV = 60;

        private const float HIGHWAY_WIDTH = 1.0f;
        private const float HIGHWAY_LENGTH = 11.0f;
        private const float HIGHWAY_ADD = 1.1f;

        #region Private Data

        private Vector3 m_cameraPosition = Vector3.Zero;

        #endregion

        #region Input Interface

        /// <summary>
        /// A unit of Roll is 1.
        /// </summary>
        public float Roll { get; set; }
        /// <summary>
        /// A unit of EffectRoll is 1.
        /// </summary>
        public float EffectRoll { get; set; }
        public float Zoom { get; set; }
        public float Pitch { get; set; }
        public float Offset { get; set; }
        public float EffectOffset { get; set; }

        public float CameraVerticalPanning { get; set; } = 0.1f;
        public Vector3 CameraShake { get; set; }

        #endregion

        #region Output Interface

        public float CameraFOV => CAMERA_FOV;
        public Vector3 CameraPosition => m_cameraPosition + CameraShake;
        public Quaternion CameraOrientation
        {
            get
            {
                Transform world = GetWorldTransform(WorldViewParams.Nothing);
                Vector3 direction = Vector3.Normalize(world.Matrix.Translation);
                float rotation = MathL.ToRadians(CameraFOV / 2 - CameraFOV * CameraVerticalPanning)
                               + MathL.Atan(direction.Y, -direction.Z);
                return Quaternion.CreateFromYawPitchRoll(0, rotation, 0);
            }
        }

        public (float Near, float Far) GetClippingPlanes(Transform world, Vector3 cameraPosition, Quaternion cameraOrientation)
        {
            Vector3[] clipPoints = new Vector3[4] { new Vector3(-1, 0, HIGHWAY_ADD), new Vector3(1, 0, HIGHWAY_ADD), new Vector3(-1, 0, -HIGHWAY_LENGTH), new Vector3(1, 0, -HIGHWAY_LENGTH) };

            float minClipDist = float.MaxValue;
            float maxClipDist = float.MinValue;

            Vector3 cameraForward = Vector3.Transform(new Vector3(0, 0, -1), cameraOrientation);
            for (int i = 0; i < 4; i++)
            {
                float clipDist = SignedDistance(Vector3.Transform(clipPoints[i], world.Matrix) - cameraPosition, cameraForward);

                minClipDist = Math.Min(minClipDist, clipDist);
                maxClipDist = Math.Max(maxClipDist, clipDist);
            }

            float clipNear = Math.Max(0.01f, minClipDist);
            float clipFar = maxClipDist;

            // TODO(local): see if the default epsilon is enough? There's no easy way to check clip planes manually right now
            if (clipNear.ApproxEq(clipFar))
                clipFar = clipNear + 0.001f;

            return (clipNear, clipFar);

            Vector3 V3Project(Vector3 a, Vector3 b) => b * (Vector3.Dot(a, b) / Vector3.Dot(b, b));
            float SignedDistance(Vector3 point, Vector3 ray)
            {
                Vector3 projected = V3Project(point, ray);
                return MathL.Sign(Vector3.Dot(ray, projected)) * projected.Length();
            }
        }

        public Transform GetWorldTransform(WorldViewParams p = WorldViewParams.Everything)
        {
            const float ANCHOR_ROT = 2.5f;
            const float ANCHOR_Y = -0.7925f;
            const float CONTNR_Z = -0.975f;

            float roll = (p.HasFlag(WorldViewParams.Roll) ? Roll : 0) +
                         (p.HasFlag(WorldViewParams.EffectRoll) ? EffectRoll : 0);

            float pitch = p.HasFlag(WorldViewParams.Pitch) ? Pitch : 0;

            float offset = (p.HasFlag(WorldViewParams.Offset) ? Offset : 0) +
                           (p.HasFlag(WorldViewParams.EffectOffset) ? EffectOffset : 0);

            var origin = Transform.RotationZ(roll * 360);
            var anchor = Transform.RotationX(ANCHOR_ROT)
                       * Transform.Translation(offset, ANCHOR_Y, 0);
            var contnr = Transform.Translation(0, 0, 0)
                       * Transform.RotationX(pitch)
                       * Transform.Translation(0, 0, CONTNR_Z);

            Transform transform = contnr * anchor * origin;

            if (p.HasFlag(WorldViewParams.Zoom))
            {
                var direction = transform.Matrix.Translation;
                float distance = direction.Length();
                direction = Vector3.Normalize(direction);

                transform *= Transform.Translation(direction * distance * Zoom);
            }

            return transform;
        }

        #endregion
    }
}
