using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace JumpStarter
{
    public struct TerminalInfo
    {
        public bool isPositive;
        public string description;
        public Transform transform;
    }

    public class VehicleAdapter
    {
        public GameObject GameObject { get; private set; }
        public string DisplayName => GameObject != null ? GameObject.name : "Unknown";
        private JumpStartSettings settings;

        // cached terminals
        private TerminalInfo? positiveTerminal = null;
        private TerminalInfo? negativeTerminal = null;

        // Simulated fallback battery values when real ones cannot be found
        private float simulatedCharge = 0.5f; // 50%

        public VehicleAdapter(GameObject go, JumpStartSettings settings)
        {
            this.GameObject = go;
            this.settings = settings;
            DiscoverTerminals();
        }

        public bool NameContains(string fragment)
        {
            return GameObject != null && GameObject.name.ToUpperInvariant().Contains(fragment.ToUpperInvariant());
        }

        private void DiscoverTerminals()
        {
            if (GameObject == null) return;
            try
            {
                // Heuristic: look for child objects named "Battery", "battery", or "bat" and find terminal transforms beneath
                Transform battery = GameObject.transform.Find("Battery") ?? GameObject.transform.Find("battery") ?? GameObject.transform.Find("BATTERY");
                if (battery != null)
                {
                    var pos = battery.Find("positive") ?? battery.Find("pos") ?? battery.Find("+" ) ?? battery.Find("posTerminal");
                    var neg = battery.Find("negative") ?? battery.Find("neg") ?? battery.Find("-") ?? battery.Find("negTerminal");

                    if (pos != null)
                    {
                        positiveTerminal = new TerminalInfo { isPositive = true, description = DisplayName + " + Terminal", transform = pos };
                    }
                    if (neg != null)
                    {
                        negativeTerminal = new TerminalInfo { isPositive = false, description = DisplayName + " - Terminal", transform = neg };
                    }
                }

                // If not found, attempt to find any child with "terminal" in name
                if (positiveTerminal == null || negativeTerminal == null)
                {
                    foreach (Transform t in GameObject.GetComponentsInChildren<Transform>())
                    {
                        string n = t.name.ToLowerInvariant();
                        if (positiveTerminal == null && (n.Contains("pos") || n.Contains("positive") || n.Contains("terminal+")))
                        {
                            positiveTerminal = new TerminalInfo { isPositive = true, description = DisplayName + " + Terminal", transform = t };
                        }
                        if (negativeTerminal == null && (n.Contains("neg") || n.Contains("negative") || n.Contains("terminal-")))
                        {
                            negativeTerminal = new TerminalInfo { isPositive = false, description = DisplayName + " - Terminal", transform = t };
                        }
                    }
                }

                // fallback: use battery object's transform as both terminals slightly offset
                if (positiveTerminal == null || negativeTerminal == null)
                {
                    Transform any = GameObject.transform.Find("Battery") ?? GameObject.transform.Find("BATTERY");
                    if (any != null)
                    {
                        if (positiveTerminal == null) positiveTerminal = new TerminalInfo { isPositive = true, description = DisplayName + " + (fallback)", transform = any };
                        if (negativeTerminal == null) negativeTerminal = new TerminalInfo { isPositive = false, description = DisplayName + " - (fallback)", transform = any };
                    }
                }

            }
            catch (Exception) { }
        }

        public TerminalInfo? GetNearestTerminal(Vector3 point, float radius)
        {
            try
            {
                TerminalInfo? best = null;
                float bestDist = float.MaxValue;
                if (positiveTerminal != null)
                {
                    float d = Vector3.Distance(point, positiveTerminal.Value.transform.position);
                    if (d <= radius && d < bestDist) { bestDist = d; best = positiveTerminal; }
                }
                if (negativeTerminal != null)
                {
                    float d = Vector3.Distance(point, negativeTerminal.Value.transform.position);
                    if (d <= radius && d < bestDist) { bestDist = d; best = negativeTerminal; }
                }
                return best;
            }
            catch { return null; }
        }

        // Battery-related adaptation (best-effort)
        public float GetBatteryCharge()
        {
            // Try to read PlayMaker variable heuristically
            try
            {
                var fsm = GameObject.GetComponent<PlayMakerFSM>();
                if (fsm != null)
                {
                    var v = fsm.FsmVariables.GetFsmFloat("BatteryCharge") ?? fsm.FsmVariables.GetFsmFloat("BatteryVoltage");
                    if (v != null) return v.Value;
                }
            }
            catch { }
            // fallback
            return simulatedCharge;
        }

        public float GetAvailableChargeForTransfer()
        {
            // Only a fraction of battery is useable
            return Mathf.Max(0f, GetBatteryCharge() - 0.05f);
        }

        public bool HasSufficientBattery(float threshold)
        {
            return GetBatteryCharge() >= threshold;
        }

        public bool IsEngineRunning()
        {
            try
            {
                var rb = GameObject.GetComponent<Rigidbody>();
                // heuristic: if RPM or engine sound exists -- fallback to Rigidbody moving
                return rb != null && rb.velocity.magnitude > 0.1f;
            }
            catch { return false; }
        }

        public float GetChargeDeficit()
        {
            float current = GetBatteryCharge();
            return Mathf.Max(0f, 1f - current);
        }

        public void ApplyChargeDelta(float delta)
        {
            try
            {
                // try write to PlayMaker if variable exists
                var fsm = GameObject.GetComponent<PlayMakerFSM>();
                if (fsm != null)
                {
                    var v = fsm.FsmVariables.GetFsmFloat("BatteryCharge") ?? fsm.FsmVariables.GetFsmFloat("BatteryVoltage");
                    if (v != null) { v.Value = Mathf.Clamp01(v.Value + delta); return; }
                }
            }
            catch { }

            // fallback
            simulatedCharge = Mathf.Clamp01(simulatedCharge + delta);
        }

        public float GetDisplayCharge()
        {
            return Mathf.Clamp01(GetBatteryCharge());
        }
    }
}
