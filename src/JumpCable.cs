using System;
using UnityEngine;
using System.Collections.Generic;

namespace JumpStarter
{
    public class JumpCable
    {
        public enum ClampType { Red, Black }

        private JumpStartSettings settings;

        private CableClamp redClamp;
        private CableClamp blackClamp;

        private List<string> clampOrder = new List<string>();
        private int hovered = 0; // 0 = red, 1 = black

        public JumpCable(JumpStartSettings settings)
        {
            this.settings = settings;
            redClamp = new CableClamp(ClampType.Red);
            blackClamp = new CableClamp(ClampType.Black);
        }

        public void OnUpdate()
        {
            // If clamps connected to vehicles, ensure distance check
            if (redClamp.IsConnected && blackClamp.IsConnected)
            {
                var posA = redClamp.ConnectedTransform.position;
                var posB = blackClamp.ConnectedTransform.position;
                float dist = Vector3.Distance(posA, posB);
                if (dist > settings.CableMaxDistance)
                {
                    // disconnect both
                    DisconnectAll();
                    if (settings.EnableSparks) Utils.SpawnSparksBetween(posA, posB);
                    ModConsole.Print("[JumpStart] Cables disconnected: vehicles moved too far apart");
                }
            }
        }

        public void OnFixedUpdate()
        {
            // nothing physics-heavy here; keep light
        }

        public string DebugString()
        {
            return $"Red: {redClamp.DebugString()}, Black: {blackClamp.DebugString()}";
        }

        public ClampType GetHoveredClamp() => hovered == 0 ? ClampType.Red : ClampType.Black;
        public void CycleHoveredClamp() { hovered = (hovered + 1) % 2; }

        public bool IsClampConnected(ClampType type)
        {
            return (type == ClampType.Red) ? redClamp.IsConnected : blackClamp.IsConnected;
        }

        public bool ConnectClampToTerminal(ClampType type, VehicleAdapter vehicle, bool terminalIsPositive)
        {
            var clamp = (type == ClampType.Red) ? redClamp : blackClamp;
            if (clamp.IsConnected) return false;
            if (vehicle == null) return false;
            // require terminal within cable reach from clamp's other end if other clamp already connected
            if (OtherClamp(type).IsConnected)
            {
                float dist = Vector3.Distance(OtherClamp(type).ConnectedTransform.position, vehicle.GameObject.transform.position);
                if (dist > settings.CableMaxDistance) return false;
            }
            clamp.Connect(vehicle, terminalIsPositive);
            return true;
        }

        public void DisconnectClamp(ClampType type)
        {
            var clamp = (type == ClampType.Red) ? redClamp : blackClamp;
            clamp.Disconnect();
        }

        public void DisconnectAll()
        {
            redClamp.Disconnect();
            blackClamp.Disconnect();
        }

        public bool IsFullyConnected()
        {
            return redClamp.IsConnected && blackClamp.IsConnected && redClamp.ConnectedVehicle != blackClamp.ConnectedVehicle;
        }

        public VehicleAdapter GetDonorVehicleForSatsuma(VehicleAdapter satsuma)
        {
            // Determine which clamp endpoints correspond to donor and satsuma
            if (!IsFullyConnected()) return null;
            if (redClamp.ConnectedVehicle == satsuma) return blackClamp.ConnectedVehicle;
            if (blackClamp.ConnectedVehicle == satsuma) return redClamp.ConnectedVehicle;
            return null;
        }

        public bool IsConnectedProperlyForJump(VehicleAdapter satsuma)
        {
            // check connection sequence: red clamps to positives, black clamps to negatives
            if (!IsFullyConnected()) return false;
            bool redPosA = redClamp.IsConnected && redClamp.ConnectedIsPositive;
            bool blackNegA = blackClamp.IsConnected && !blackClamp.ConnectedIsPositive;
            // ensure clamps connect opposite vehicles
            return redPosA && blackNegA;
        }

        private CableClamp OtherClamp(ClampType type) => type == ClampType.Red ? blackClamp : redClamp;

        public class CableClamp
        {
            public ClampType Type { get; private set; }
            public bool IsConnected => ConnectedVehicle != null;
            public VehicleAdapter ConnectedVehicle { get; private set; }
            public bool ConnectedIsPositive { get; private set; }
            public Transform ConnectedTransform { get; private set; }

            public CableClamp(ClampType t) { Type = t; }

            public void Connect(VehicleAdapter vehicle, bool terminalIsPositive)
            {
                ConnectedVehicle = vehicle;
                ConnectedIsPositive = terminalIsPositive;
                // approximate transform
                var t = terminalIsPositive ? vehicle.GetNearestTerminalTransform(true) : vehicle.GetNearestTerminalTransform(false);
                ConnectedTransform = t ?? vehicle.GameObject.transform;
            }

            public void Disconnect()
            {
                ConnectedVehicle = null; ConnectedTransform = null;
            }

            public string DebugString()
            {
                if (!IsConnected) return "(not connected)";
                return $"({ConnectedVehicle.DisplayName}, {(ConnectedIsPositive?"+":"-")})";
            }
        }
    }

    static class JumpCableExtensions
    {
        public static Transform GetNearestTerminalTransform(this VehicleAdapter adapter, bool wantPositive)
        {
            // best-effort: search nearest terminal by flag
            try
            {
                var t = adapter.GameObject.GetComponentInChildren<Transform>();
                // reuse adapter's GetNearestTerminal with its own position
                var info = adapter.GetNearestTerminal(adapter.GameObject.transform.position, 99999f);
                if (info != null && info.Value.isPositive == wantPositive) return info.Value.transform;
            }
            catch { }
            return adapter.GameObject.transform;
        }
    }
}
