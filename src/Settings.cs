using MSCLoader;

namespace JumpStarter
{
    public class JumpStartSettings : ModSettings
    {
        public Checkbox enableJumping = new Checkbox("Enable jump-start system", true);
        public Checkbox enableSparks = new Checkbox("Enable sparks on errors", true);
        public FloatSlider cableMaxDistance = new FloatSlider("Cable maximum distance", 5f, 2f, 10f);
        public FloatSlider interactionDistance = new FloatSlider("Interaction distance", 3f, 1f, 6f);
        public FloatSlider terminalHitRadius = new FloatSlider("Terminal hit radius", 0.5f, 0.1f, 1.5f);
        public FloatSlider donorMinCharge = new FloatSlider("Donor minimum charge", 0.25f, 0f, 1f);
        public FloatSlider maxTransferPerSecond = new FloatSlider("Max transfer per second", 0.2f, 0.01f, 1f);
        public Checkbox requireDonorRunning = new Checkbox("Require donor engine running", true);
        public Checkbox enableChargingHum = new Checkbox("Enable charging hum (placeholder)", true);

        // runtime debug flag not persisted
        public bool DebugMode = false;

        // Properties for ease of use
        public float CableMaxDistance => cableMaxDistance.Value;
        public float InteractionDistance => interactionDistance.Value;
        public float TerminalHitRadius => terminalHitRadius.Value;
        public float DonorMinCharge => donorMinCharge.Value;
        public float MaxTransferPerSecond => maxTransferPerSecond.Value;
        public bool EnableSparks => enableSparks.Value;
        public bool RequireDonorRunning => requireDonorRunning.Value;
        public bool EnableChargingHum => enableChargingHum.Value;
    }
}
