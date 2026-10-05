// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Xml;
using ClassicUO.Platform;
using ClassicUO.Input;
using static SDL3.SDL;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Gump : Control
    {
        private TouchCloseButton _touchClose;
        public Gump(World world, uint local, uint server)
        {
            World = world;
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public World World { get; }

        public bool CanBeSaved => GumpType != Gumps.GumpType.None;

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }

        public uint MasterGumpSerial { get; set; }


        public override void Update()
        {
            if (InvalidateContents)
            {
                UpdateContents();
                InvalidateContents = false;
            }

            if (ActivePage == 0)
            {
                ActivePage = 1;
            }

            // The synthetic chrome must not keep a paged server window at its old size.
            if (_touchClose != null) _touchClose.IsVisible = false;
            base.Update();

            UpdateTouchClose();
        }

        private void UpdateTouchClose()
        {
            bool eligible = ClientHooks.Platform.FixedWindowSize && World.InGame && CanCloseWithRightClick
                && Width >= 80 && Height >= 70 && !(this is WorldViewportGump)
                && GumpType != GumpType.Buff && GumpType != GumpType.HealthBar
                && GumpType != GumpType.CounterBar && GumpType != GumpType.InfoBar
                && GumpType != GumpType.MacroButton && GumpType != GumpType.AbilityButton
                && GumpType != GumpType.SpellButton && GumpType != GumpType.SkillButton
                && GumpType != GumpType.RacialButton && GumpType != GumpType.NameOverHeadHandler;
            if (!eligible)
            {
                if (_touchClose != null) _touchClose.IsVisible = false;
                return;
            }
            if (_touchClose == null || _touchClose.IsDisposed || _touchClose.Parent != this)
            {
                _touchClose = new TouchCloseButton(this);
                Add(_touchClose);
            }
            var game = Client.Game;
            var size = Mouse.WindowToGame(44, 44, game.GraphicManager.PreferredBackBufferWidth,
                game.GraphicManager.PreferredBackBufferHeight, game.Window.ClientBounds.Width,
                game.Window.ClientBounds.Height, game.DpiScale);
            _touchClose.Width = Math.Max(44, size.X);
            _touchClose.Height = Math.Max(44, size.Y);
            Rectangle safeBounds = game.ClientBounds;
            if (SDL_GetWindowSafeArea(game.Window.Handle, out SDL_Rect safe))
            {
                Point topLeft = Mouse.WindowToGame(safe.x, safe.y, game.GraphicManager.PreferredBackBufferWidth,
                    game.GraphicManager.PreferredBackBufferHeight, game.Window.ClientBounds.Width,
                    game.Window.ClientBounds.Height, game.DpiScale);
                Point bottomRight = Mouse.WindowToGame(safe.x + safe.w, safe.y + safe.h, game.GraphicManager.PreferredBackBufferWidth,
                    game.GraphicManager.PreferredBackBufferHeight, game.Window.ClientBounds.Width,
                    game.Window.ClientBounds.Height, game.DpiScale);
                safeBounds = new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
            }
            // Remain inside the root hit bounds, in a corner outside the circular map.
            _touchClose.X = Math.Clamp(Width - _touchClose.Width, safeBounds.Left - X, Math.Max(safeBounds.Left - X, safeBounds.Right - X - _touchClose.Width));
            _touchClose.Y = Math.Clamp(0, safeBounds.Top - Y, Math.Max(safeBounds.Top - Y, safeBounds.Bottom - Y - _touchClose.Height));
            _touchClose.IsVisible = true;
        }

        protected bool ContainsTouchClose(int x, int y) => _touchClose != null && _touchClose.IsVisible
            && !_touchClose.IsDisposed && _touchClose.Bounds.Contains(x, y);

        public override void Dispose()
        {
            Item it = World.Items.Get(LocalSerial);

            if (it != null && it.Opened)
            {
                it.Opened = false;
            }

            base.Dispose();
        }


        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int) GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());
        }

        public void SetInScreen()
        {
            Rectangle windowBounds = Client.Game.ClientBounds;
            Rectangle bounds = Bounds;
            bounds.X += windowBounds.X;
            bounds.Y += windowBounds.Y;

            if (windowBounds.Intersects(bounds))
            {
                return;
            }

            X = 0;
            Y = 0;
        }

        public virtual void Restore(XmlElement xml)
        {
        }

        public void RequestUpdateContents()
        {
            InvalidateContents = true;
        }

        protected virtual void UpdateContents()
        {
        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width - (Width >> 2);
            int halfHeight = Height - (Height >> 2);

            if (X < -halfWidth)
            {
                position.X = -halfWidth;
            }

            if (Y < -halfHeight)
            {
                position.Y = -halfHeight;
            }

            if (X > Client.Game.ClientBounds.Width - (Width - halfWidth))
            {
                position.X = Client.Game.ClientBounds.Width - (Width - halfWidth);
            }

            if (Y > Client.Game.ClientBounds.Height - (Height - halfHeight))
            {
                position.Y = Client.Game.ClientBounds.Height - (Height - halfHeight);
            }

            Location = position;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            return IsVisible && base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0)
            {
                List<uint> switches = new List<uint>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case StbTextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump
                (
                    LocalSerial,
                    // Seems like MasterGump serial does not work as expected.
                    /*MasterGumpSerial != 0 ? MasterGumpSerial :*/ ServerSerial,
                    buttonID,
                    switches.ToArray(),
                    entries.ToArray()
                );

                if (CanMove)
                {
                    UIManager.SavePosition(ServerSerial, Location);
                }
                else
                {
                    UIManager.RemovePosition(ServerSerial);
                }

                Dispose();
            }
        }

        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
            {
                return;
            }

            if (ServerSerial != 0)
            {
                OnButtonClick(0);
            }

            base.CloseWithRightClick();
        }

        public override void ChangePage(int pageIndex)
        {
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
        }
    }
}
