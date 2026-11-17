// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Emanuele Sparvoli
//
// HDT plugin: really hide the overlay when Hearthstone loses focus.
//
// Stock HDT doesn't hide the overlay when Hearthstone goes to the background.
// It sends it to the bottom of the z-order (OverlayZState.Behind). Under Wine
// the overlay is an override-redirect X window, which the compositor always
// draws above managed windows, so "send to back" does nothing and the overlay
// floats over every other app. This plugin sets Config.HideOverlay in memory
// while Hearthstone is in the background, which drives HDT's own Hidden path
// (opacity 0, click-through), and clears it when Hearthstone regains focus.
//
// HDT saves config.xml from many places (including at shutdown, before plugins
// unload), so a temporary HideOverlay=true can reach disk. A marker file records
// that the flag is ours; OnLoad and OnUnload clear it and save again.
//
// C# 5: compiled with the .NET Framework csc.exe inside the Wine prefix.

using System;
using System.IO;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;

[assembly: System.Reflection.AssemblyTitle("WineOverlayHide")]
[assembly: System.Reflection.AssemblyDescription("HDT plugin that hides the overlay while Hearthstone is in the background under Wine/Proton")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace WineOverlayHide
{
    public class WineOverlayHidePlugin : IPlugin
    {
        private bool _weHid;

        private static string MarkerPath
        {
            get { return Path.Combine(Path.Combine(Config.AppDataPath, "Plugins"), "WineOverlayHide.hidden"); }
        }

        public string Name { get { return "Wine Overlay Hide"; } }
        public string Description { get { return "Hides the overlay while Hearthstone is in the background. Fixes the overlay staying on top of other apps under Wine/Proton."; } }
        public string ButtonText { get { return "No settings"; } }
        public string Author { get { return "Emanuele"; } }
        public Version Version { get { return new Version(1, 0, 0); } }
        public MenuItem MenuItem { get { return null; } }

        public void OnLoad()
        {
            // A previous session saved HideOverlay=true on our behalf: undo it.
            if (File.Exists(MarkerPath))
            {
                Config.Instance.HideOverlay = false;
                SaveAndClearMarker("recovered stale hide from previous session");
            }
        }

        public void OnUnload()
        {
            if (_weHid)
            {
                Config.Instance.HideOverlay = false;
                _weHid = false;
                SaveAndClearMarker("restored overlay on unload");
            }
        }

        public void OnButtonPress()
        {
        }

        public void OnUpdate()
        {
            // Someone else (hotkey, options checkbox) cleared the flag while we
            // held it. Forget our claim; the check below re-applies the hide if
            // Hearthstone is still in the background.
            if (_weHid && !Config.Instance.HideOverlay)
            {
                _weHid = false;
                DeleteMarker();
            }

            bool hsRunning = User32.GetHearthstoneWindow() != IntPtr.Zero;
            bool hideInBackground = Core.Game.IsInMenu
                ? Config.Instance.HideMenuOverlayInBackground
                : Config.Instance.HideInBackground;
            bool wantHidden = hsRunning && hideInBackground && !User32.IsHearthstoneInForeground();

            if (wantHidden)
            {
                if (!_weHid && !Config.Instance.HideOverlay)
                {
                    WriteMarker();
                    Config.Instance.HideOverlay = true;
                    _weHid = true;
                }
            }
            else if (_weHid)
            {
                Config.Instance.HideOverlay = false;
                _weHid = false;
                DeleteMarker();
            }
        }

        private static void SaveAndClearMarker(string why)
        {
            try
            {
                Config.Save();
                DeleteMarker();
                Log.Info("WineOverlayHide: " + why);
            }
            catch (Exception ex)
            {
                Log.Error("WineOverlayHide: config save failed, keeping marker: " + ex);
            }
        }

        private static void WriteMarker()
        {
            try { File.WriteAllText(MarkerPath, DateTime.Now.ToString("o")); }
            catch (Exception ex) { Log.Error("WineOverlayHide: cannot write marker: " + ex); }
        }

        private static void DeleteMarker()
        {
            try { if (File.Exists(MarkerPath)) File.Delete(MarkerPath); }
            catch (Exception ex) { Log.Error("WineOverlayHide: cannot delete marker: " + ex); }
        }
    }
}
