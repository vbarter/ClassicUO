// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Mouse
    {
        public const int MOUSE_DELAY_DOUBLE_CLICK = 350;

        /* Log a button press event at the given time. */
        public static void ButtonPress(MouseButtonType type)
        {
            CancelDoubleClick = false;

            switch (type)
            {
                case MouseButtonType.Left:
                    LButtonPressed = true;
                    LClickPosition = Position;

                    break;

                case MouseButtonType.Middle:
                    MButtonPressed = true;
                    MClickPosition = Position;

                    break;

                case MouseButtonType.Right:
                    RButtonPressed = true;
                    RClickPosition = Position;

                    break;

                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    XButtonPressed = true;

                    break;
            }

            SDL.SDL_CaptureMouse(true);
        }

        /* Log a button release event at the given time */
        public static void ButtonRelease(MouseButtonType type)
        {
            switch (type)
            {
                case MouseButtonType.Left:
                    LButtonPressed = false;

                    break;

                case MouseButtonType.Middle:
                    MButtonPressed = false;

                    break;

                case MouseButtonType.Right:
                    RButtonPressed = false;

                    break;

                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    XButtonPressed = false;

                    break;
            }

            if (!(LButtonPressed || RButtonPressed || MButtonPressed))
            {
                SDL.SDL_CaptureMouse(false);
            }
        }

        public static Point Position;

        public static Point LClickPosition;

        public static Point RClickPosition;

        public static Point MClickPosition;

        public static uint LastLeftButtonClickTime { get; set; }

        public static uint LastMidButtonClickTime { get; set; }

        public static uint LastRightButtonClickTime { get; set; }

        public static bool CancelDoubleClick { get; set; }

        public static bool LButtonPressed { get; set; }

        public static bool RButtonPressed { get; set; }

        public static bool MButtonPressed { get; set; }

        public static bool XButtonPressed { get; set; }

        public static bool IsDragging { get; set; }

        public static Point LDragOffset => LButtonPressed ? Position - LClickPosition : Point.Zero;

        public static Point RDragOffset => RButtonPressed ? Position - RClickPosition : Point.Zero;

        public static Point MDragOffset => MButtonPressed ? Position - MClickPosition : Point.Zero;

        public static bool MouseInWindow { get; set; }

        /// <summary>
        /// When set, <see cref="Update"/> uses this window position instead of querying the SDL mouse.
        /// Lets non-mouse input (touch) drive the pointer. Same units as SDL mouse coordinates.
        /// </summary>
        public static Point? InjectedWindowPosition { get; set; }

        public static void Update()
        {
            float x, y;

            if (InjectedWindowPosition.HasValue)
            {
                x = InjectedWindowPosition.Value.X;
                y = InjectedWindowPosition.Value.Y;
            }
            else if (!MouseInWindow)
            {
                SDL.SDL_GetGlobalMouseState(out x, out y);
                SDL.SDL_GetWindowPosition(Client.Game.Window.Handle, out int winX, out int winY);
                x -= winX;
                y -= winY;
            }
            else
            {
                SDL.SDL_GetMouseState(out x, out y);
            }

            Position = WindowToGame(
                (int)x,
                (int)y,
                Client.Game.GraphicManager.PreferredBackBufferWidth,
                Client.Game.GraphicManager.PreferredBackBufferHeight,
                Client.Game.Window.ClientBounds.Width,
                Client.Game.Window.ClientBounds.Height,
                Client.Game.DpiScale
            );

            IsDragging = LButtonPressed || RButtonPressed || MButtonPressed;
        }

        /// <summary>
        /// Scales window coordinates for the faux-backbuffer and DPI settings.
        /// </summary>
        internal static Point WindowToGame(int windowX, int windowY, int backBufferWidth, int backBufferHeight, int clientWidth, int clientHeight, float dpiScale)
        {
            // NOTE: integer division is intentional, it matches the historical behaviour
            return new Point(
                (int) ((double) windowX * (backBufferWidth / clientWidth) / dpiScale),
                (int) ((double) windowY * (backBufferHeight / clientHeight) / dpiScale)
            );
        }
    }
}