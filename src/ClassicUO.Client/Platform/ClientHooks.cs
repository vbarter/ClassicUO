// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Renderer;
using SDL3;

namespace ClassicUO.Platform
{
    /// <summary>
    /// Services the host platform provides to the client.
    /// Desktop builds use <see cref="DesktopClientPlatform"/>; mobile hosts register their own.
    /// </summary>
    internal interface IClientPlatform
    {
        /// <summary>Assistant plugins (Razor, ClassicAssist, ...) need dynamic code loading.</summary>
        bool SupportsPlugins { get; }

        /// <summary>When true the client hides the UO mouse cursor (touch screens have no pointer).</summary>
        bool HideGameCursor { get; }

        /// <summary>
        /// When true the platform starts and stops SDL text input itself (showing an on-screen keyboard
        /// only when needed). When false the client keeps text input enabled for the whole session.
        /// </summary>
        bool ManagesTextInput { get; }

        /// <summary>
        /// When true the window always fills the screen (mobile). Requests to resize, maximize or
        /// make the window borderless only resize the back buffer to the actual window size.
        /// </summary>
        bool FixedWindowSize { get; }

        /// <summary>
        /// When true the client drops to a few frames per second while its window has no focus
        /// (the ReduceFPSWhenInactive option). Mobile systems suspend background apps themselves.
        /// </summary>
        bool ThrottleWhenInactive { get; }

        /// <summary>
        /// When true presenting waits for the display (vsync) and every loop iteration that is
        /// due draws a frame, so the game updates once per refresh instead of spinning between frames.
        /// </summary>
        bool SyncWithDisplay { get; }
        bool HighResolutionUI { get; }

        /// <summary>
        /// True while the app runs in the background (mobile): nothing may be drawn (no GPU work is
        /// allowed there) and the loop only needs to keep the connection alive, so it runs a few
        /// times per second instead of every frame.
        /// </summary>
        bool Suspended { get; }
    }

    /// <summary>
    /// Code that plugs into the game loop without being part of the upstream client
    /// (for example a touch input layer and its on-screen HUD).
    /// </summary>
    internal unsafe interface IClientExtension
    {
        /// <summary>Return true to consume the event so the client does not process it.</summary>
        bool HandleSdlEvent(SDL.SDL_Event* ev);

        void Update();

        /// <summary>Draws on top of the UI, below the game cursor. The batcher is not begun.</summary>
        void Draw(UltimaBatcher2D batcher);
    }

    /// <summary>
    /// Supplies character movement from something other than the right mouse button
    /// (for example an on-screen joystick).
    /// </summary>
    internal interface IMovementSource
    {
        /// <param name="screenX">Horizontal screen-space direction (right is positive).</param>
        /// <param name="screenY">Vertical screen-space direction (down is positive).</param>
        /// <param name="run">True to run instead of walk.</param>
        /// <returns>False when there is no movement request.</returns>
        bool TryGetMovement(out float screenX, out float screenY, out bool run);
    }

    internal static class ClientHooks
    {
        public static IClientPlatform Platform { get; set; } = new DesktopClientPlatform();

        public static IMovementSource MovementSource { get; set; }

        public static readonly List<IClientExtension> Extensions = new List<IClientExtension>();
    }

    internal sealed class DesktopClientPlatform : IClientPlatform
    {
        public bool SupportsPlugins => true;

        public bool HideGameCursor => false;

        public bool ManagesTextInput => false;

        public bool FixedWindowSize => false;

        public bool ThrottleWhenInactive => true;

        public bool Suspended => false;

        public bool SyncWithDisplay => false;
        public bool HighResolutionUI => false;
    }
}
