using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Cyber-only visual ride timeline. Gameplay collision rules stay in RunnerGame.
    /// Deliberately frame-rate independent. The gadget stays attached until the rider lands.
    /// </summary>
    public sealed class CyberRideMotion
    {
        public CyberGadget VisibleGadget { get; private set; }
        public float Lift { get; private set; }
        public bool Descending { get; private set; }
        public bool LandedThisFrame { get; private set; }
        public float LandingPose { get; private set; }
        public float TakeoffPose { get; private set; }

        private float verticalVelocity;
        private float descentStart;
        private float descentElapsed;
        private float descentDuration;
        private float landingClock;
        private float takeoffClock;

        public void Reset()
        {
            VisibleGadget = CyberGadget.None;
            Lift = 0f;
            Descending = false;
            LandedThisFrame = false;
            LandingPose = 0f;
            TakeoffPose = 0f;
            verticalVelocity = 0f;
            descentStart = descentElapsed = descentDuration = 0f;
            landingClock = takeoffClock = 0f;
        }

        public void Equip(CyberGadget gadget)
        {
            if (gadget == CyberGadget.None) return;
            VisibleGadget = gadget;
            Descending = false;
            takeoffClock = 0f;
            landingClock = 0f;
            LandingPose = 0f;
            TakeoffPose = 1f;
            verticalVelocity = 0f;
        }

        public void Release(bool damaged)
        {
            if (VisibleGadget == CyberGadget.None || Descending) return;
            Descending = true;
            descentStart = Lift;
            descentElapsed = 0f;
            // Longer, controlled landing for a high-altitude jetpack; board/mech settle faster.
            descentDuration = damaged ? .28f :
                VisibleGadget == CyberGadget.Jetpack ? 1.20f :
                VisibleGadget == CyberGadget.Hoverboard ? .58f : .70f;
            verticalVelocity = 0f;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public void Tick(float dt, float time)
        {
            LandedThisFrame = false;
            dt = Mathf.Max(0f, dt);
            takeoffClock += dt;
            TakeoffPose = Mathf.Clamp01(1f - takeoffClock / .65f);
            landingClock += dt;
            LandingPose = Mathf.Clamp01(1f - landingClock / .42f);

            if (VisibleGadget == CyberGadget.None)
            {
                Lift = 0f;
                return;
            }

            if (Descending)
            {
                descentElapsed += dt;
                float t = Smooth(descentElapsed / Mathf.Max(.02f, descentDuration));
                Lift = Mathf.Max(0f, Mathf.Lerp(descentStart, 0f, t));
                if (descentElapsed >= descentDuration)
                {
                    Lift = 0f;
                    VisibleGadget = CyberGadget.None;
                    Descending = false;
                    LandedThisFrame = true;
                    LandingPose = 1f;
                    landingClock = 0f;
                }
                return;
            }

            float target = VisibleGadget == CyberGadget.Jetpack ? 5.15f :
                VisibleGadget == CyberGadget.Hoverboard ? .95f : 1.24f;
            float response = VisibleGadget == CyberGadget.Jetpack ? .37f : .23f;
            Lift = Mathf.SmoothDamp(Lift, target, ref verticalVelocity,
                response, 30f, Mathf.Max(.00001f, dt));
            // Small flying bob with no jumps in altitude when equipment changes.
            // Does not change gameplay collision height.
            Lift = Mathf.Max(0f, Lift);
        }
    }
}
