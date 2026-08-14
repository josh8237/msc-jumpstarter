using System;
using UnityEngine;
using MSCLoader;

namespace JumpStarter
{
    public class JumpStartMod : Mod
    {
        public override string ID => "jumpstarter";
        public override string Name => "MSC JumpStarter";
        public override string Author => "josh8237";
        public override string Version => "0.1.0";
        public override string Description => "Adds realistic jumper-cable jump-starting using MSCLoader.";

        internal static JumpStartSettings Settings;
        internal static JumpStartManager Manager;

        public override void OnLoad()
        {
            try
            {
                ModConsole.Print("[JumpStart] Loading JumpStarter mod...");

                Settings = new JumpStartSettings();
                // SetupSettings may not exist on older MSCLoader; call if available.
                try
                {
                    SetupSettings(Settings);
                }
                catch (Exception)
                {
                    ModConsole.Print("[JumpStart] SetupSettings unavailable on this MSCLoader build. Settings UI will not be available.");
                }

                Manager = new JumpStartManager(Settings);

                // Register update hook
                ModConsole.Print("[JumpStart] Loaded. Press F7 to toggle debug/test mode.");

            }
            catch (Exception ex)
            {
                ModConsole.Print("[JumpStart] Failed to load: " + ex.Message, ModConsole.Error);
            }
        }

        public override void Update()
        {
            try
            {
                Manager?.OnUpdate();

                // debug toggle
                if (Input.GetKeyDown(KeyCode.F7))
                {
                    Settings.DebugMode = !Settings.DebugMode;
                    ModConsole.Print("[JumpStart] Debug mode: " + Settings.DebugMode);
                }
            }
            catch (Exception ex)
            {
                ModConsole.Print("[JumpStart] Update error: " + ex.Message, ModConsole.Error);
            }
        }

        public override void FixedUpdate()
        {
            try
            {
                Manager?.OnFixedUpdate();
            }
            catch (Exception ex)
            {
                ModConsole.Print("[JumpStart] FixedUpdate error: " + ex.Message, ModConsole.Error);
            }
        }

        public override void OnGUI()
        {
            try
            {
                Manager?.OnGUI();
            }
            catch { }
        }

        public override void OnSave()
        {
            // save any persistent state if needed; intentionally minimal
            ModConsole.Print("[JumpStart] OnSave called");
        }
    }
}
