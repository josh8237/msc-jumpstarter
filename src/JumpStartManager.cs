using System;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;

namespace JumpStarter
{
    public class JumpStartManager
    {
        private JumpStartSettings settings;
        private JumpCable cable;
        private List<VehicleAdapter> trackedVehicles = new List<VehicleAdapter>();
        private Camera playerCamera;
        private float lastScan = 0f;
        private const float scanInterval = 5f;

        public bool ShowInteractionLabel = false;
        public string InteractionLabel = string.Empty;

        public JumpStartManager(JumpStartSettings settings)
        {
            this.settings = settings;
            cable = new JumpCable(settings);
            playerCamera = Camera.main;
            SafeLog("JumpStartManager initialized");
        }

        public void OnUpdate()
        {
            try
            {
                if (Time.time - lastScan > scanInterval)
                {
                    lastScan = Time.time;
                    DiscoverVehicles();
                }

                UpdateInteraction();

                // handle simple input actions for connect/disconnect
                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryInteract();
                }

                // Allow player to attempt starting Satsuma if connected
                if (Input.GetKeyDown(KeyCode.R))
                {
                    AttemptStartSatsuma();
                }

                cable.OnUpdate();
            }
            catch (Exception ex)
            {
                SafeLog("OnUpdate error: " + ex.Message);
            }
        }

        public void OnFixedUpdate()
        {
            // physics-related checks (disconnect if too far)
            cable.OnFixedUpdate();
        }

        public void OnGUI()
        {
            if (ShowInteractionLabel && !string.IsNullOrEmpty(InteractionLabel))
            {
                var style = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                Rect r = new Rect(Screen.width / 2f - 200, Screen.height - 120, 400, 40);
                GUI.Label(r, InteractionLabel, style);
            }

            if (settings.DebugMode)
            {
                GUILayout.BeginArea(new Rect(10, 10, 400, 400));
                GUILayout.Label("[JumpStart] Debug Info");
                GUILayout.Label("Tracked vehicles: " + trackedVehicles.Count);
                GUILayout.Label("Cable: " + cable.DebugString());
                GUILayout.EndArea();
            }
        }

        private void DiscoverVehicles()
        {
            trackedVehicles.Clear();
            // Heuristic: look for common vehicle names used in MSC
            string[] vehicleNames = new[] { "SATSUMA", "FERNDALE", "HAYOSIKO", "KEKMET" };
            foreach (var root in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                try
                {
                    string nm = root.gameObject.name.ToUpperInvariant();
                    foreach (var v in vehicleNames)
                    {
                        if (nm.Contains(v))
                        {
                            var va = new VehicleAdapter(root.gameObject, settings);
                            trackedVehicles.Add(va);
                            SafeLog("Detected vehicle: " + root.gameObject.name);
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        private void UpdateInteraction()
        {
            ShowInteractionLabel = false;
            InteractionLabel = string.Empty;

            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, settings.InteractionDistance))
            {
                // Check if hit a known vehicle terminal
                foreach (var v in trackedVehicles)
                {
                    var term = v.GetNearestTerminal(hit.point, settings.TerminalHitRadius);
                    if (term != null)
                    {
                        // show label depending on clamp availability
                        var clamp = cable.GetHoveredClamp();
                        string clampName = clamp == JumpCable.ClampType.Red ? "CONNECT RED CLAMP" : "CONNECT BLACK CLAMP";
                        if (cable.IsClampConnected(clamp))
                            clampName = "DISCONNECT CLAMP";

                        InteractionLabel = clampName + "\n" + "Target: " + term.Value.description;
                        ShowInteractionLabel = true;
                        return;
                    }
                }
            }
        }

        private void TryInteract()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, settings.InteractionDistance))
            {
                foreach (var v in trackedVehicles)
                {
                    var term = v.GetNearestTerminal(hit.point, settings.TerminalHitRadius);
                    if (term != null)
                    {
                        // Connect or disconnect based on clamp state
                        var hoveredClamp = cable.GetHoveredClamp();
                        if (!cable.IsClampConnected(hoveredClamp))
                        {
                            bool ok = cable.ConnectClampToTerminal(hoveredClamp, v, term.Value.isPositive);
                            if (ok)
                            {
                                SafeLog(hoveredClamp + " connected to " + (term.Value.isPositive ? "positive" : "negative"));
                            }
                            else
                            {
                                SafeLog("Failed to connect " + hoveredClamp);
                            }
                        }
                        else
                        {
                            cable.DisconnectClamp(hoveredClamp);
                            SafeLog(hoveredClamp + " disconnected");
                        }
                        return;
                    }
                }
            }

            // If player not looking at terminal, cycle hovered clamp
            cable.CycleHoveredClamp();
            SafeLog("Hovered clamp: " + cable.GetHoveredClamp());
        }

        private void AttemptStartSatsuma()
        {
            // Find Satsuma
            VehicleAdapter satsuma = null;
            foreach (var v in trackedVehicles)
            {
                if (v.NameContains("SATSUMA")) { satsuma = v; break; }
            }
            if (satsuma == null)
            {
                SafeLog("No Satsuma found");
                return;
            }

            if (!cable.IsFullyConnected())
            {
                SafeLog("Cables are not fully connected");
                return;
            }

            // Validate donor
            var donor = cable.GetDonorVehicleForSatsuma(satsuma);
            if (donor == null)
            {
                SafeLog("No valid donor attached");
                return;
            }

            if (!donor.HasSufficientBattery(settings.DonorMinCharge))
            {
                SafeLog("Donor battery too low");
                // optional sparks/effects
                if (settings.EnableSparks) Utils.SpawnSparksNear(donor.GameObject.transform.position);
                return;
            }

            // If donor needs to run, ensure it's running
            if (settings.RequireDonorRunning && !donor.IsEngineRunning())
            {
                SafeLog("Donor engine not running. Start donor vehicle first.");
                return;
            }

            // Begin transfer/charging
            float required = satsuma.GetChargeDeficit();
            float donorAvailable = donor.GetAvailableChargeForTransfer();

            float transferAmount = Mathf.Min(donorAvailable, Mathf.Min(required, settings.MaxTransferPerSecond * Time.deltaTime));

            if (transferAmount <= 0f)
            {
                SafeLog("No available charge to transfer");
                return;
            }

            // apply transfer
            donor.ApplyChargeDelta(-transferAmount);
            satsuma.ApplyChargeDelta(transferAmount);

            SafeLog($"Transferred {transferAmount:0.00} charge from {donor.DisplayName} to Satsuma");

            // Optional audio/hum
            if (settings.EnableChargingHum)
            {
                // placeholder: play a short audio if available
            }

            // After transfer, allow player to try starting via game starter
            // We do not forcibly start the engine; instead we log and let player use game start controls.
        }

        private void SafeLog(string message)
        {
            if (settings.DebugMode) ModConsole.Print("[JumpStart] " + message);
        }
    }
}
