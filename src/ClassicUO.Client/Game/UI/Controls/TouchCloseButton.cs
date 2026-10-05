using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>A touch-sized close affordance that uses the existing cancel/reply path.</summary>
    internal sealed class TouchCloseButton : Control
    {
        private readonly Gump _owner;
        private bool _pressed;
        private bool _touchActive, _touchInside;

        public TouchCloseButton(Gump owner)
        {
            _owner = owner;
            AcceptMouseInput = true;
            CanMove = false;
            WantUpdateSize = false;
            SetTooltip("Close");
        }

        public override ClickPriority Priority => ClickPriority.High;

        public void BeginTouch() => _pressed = _touchActive = _touchInside = true;

        public void MoveTouch(Point point) => _touchInside = new Rectangle(ScreenCoordinateX, ScreenCoordinateY, Width, Height).Contains(point);

        public bool EndTouch(Point point, bool cancelled)
        {
            bool close = _pressed && !cancelled && !IsDisposed && IsVisible && _owner.CanCloseWithRightClick
                && new Rectangle(ScreenCoordinateX, ScreenCoordinateY, Width, Height).Contains(point);
            _pressed = false;
            _touchActive = false;
            if (close) _owner.InvokeMouseCloseGumpWithRClick();
            return close;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left) _pressed = true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            bool close = _pressed && button == MouseButtonType.Left && x >= 0 && y >= 0 && x < Width && y < Height;
            _pressed = false;
            if (close) _owner.InvokeMouseCloseGumpWithRClick();
        }

        public override bool AddToRenderLists(RenderLists lists, int x, int y, ref float depthRef)
        {
            if (!IsVisible || IsDisposed) return false;
            float depth = depthRef;
            lists.AddGumpNoAtlas(batcher =>
            {
                int inset = Width / 8;
                var rect = new Rectangle(x + inset, y + inset, Width - inset * 2, Height - inset * 2);
                Color gold = _pressed && (_touchActive ? _touchInside : MouseIsOver) ? new Color(255, 215, 125) : new Color(177, 139, 75);
                batcher.Draw(RoundUiRenderer.Disc(batcher.GraphicsDevice, new Color(28, 25, 22)), rect, Vector3.UnitZ, depth);
                var center = new Vector2(x + Width / 2f, y + Height / 2f);
                RoundUiRenderer.Ring(batcher, center, rect.Width / 2f - 1, gold, 2, depth);
                float arm = Width * 0.16f;
                var line = SolidColorTextureCache.GetTexture(gold);
                batcher.DrawLine(line, center - new Vector2(arm, arm), center + new Vector2(arm, arm), Vector3.UnitZ, 2, depth);
                batcher.DrawLine(line, center + new Vector2(-arm, arm), center + new Vector2(arm, -arm), Vector3.UnitZ, 2, depth);
                return true;
            });
            return true;
        }
    }
}
