using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    /// <summary>Cached round UI shapes and a stencil clip; map layers render only once.</summary>
    public static class RoundUiRenderer
    {
        private static GraphicsDevice _device;
        private static readonly Dictionary<Color, Texture2D> _discs = new();
        private static readonly BlendState NoColor = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.None,
            ColorWriteChannels1 = ColorWriteChannels.None,
            ColorWriteChannels2 = ColorWriteChannels.None,
            ColorWriteChannels3 = ColorWriteChannels.None
        };
        private static readonly DepthStencilState WriteMask = new DepthStencilState
        {
            DepthBufferEnable = false, StencilEnable = true,
            StencilFunction = CompareFunction.Always, ReferenceStencil = 1,
            StencilPass = StencilOperation.Replace
        };
        private static readonly DepthStencilState ReadMask = new DepthStencilState
        {
            DepthBufferEnable = false, StencilEnable = true,
            StencilFunction = CompareFunction.Equal, ReferenceStencil = 1,
            StencilPass = StencilOperation.Keep, StencilWriteMask = 0
        };

        public static Texture2D Disc(GraphicsDevice device, Color color)
        {
            if (_device != device)
            {
                foreach (Texture2D texture in _discs.Values) texture.Dispose();
                _discs.Clear();
                _device = device;
            }
            if (_discs.TryGetValue(color, out Texture2D cached) && !cached.IsDisposed) return cached;
            const int size = 512;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size / 2f, dy = y + 0.5f - size / 2f;
                    float coverage = MathHelper.Clamp(size / 2f - 1f - MathF.Sqrt(dx * dx + dy * dy), 0f, 1f);
                    pixels[y * size + x] = color * coverage;
                }
            var result = new Texture2D(device, size, size);
            result.SetData(pixels);
            _discs[color] = result;
            return result;
        }

        public static void BeginClip(UltimaBatcher2D batcher, Rectangle bounds, float depth)
        {
            batcher.SetStencil(null); // Flush queued UI before clearing its stencil buffer.
            batcher.GraphicsDevice.Clear(ClearOptions.Stencil, Color.Transparent, 0f, 0);
            batcher.SetBlendState(NoColor);
            batcher.SetStencil(WriteMask);
            batcher.Draw(Disc(batcher.GraphicsDevice, Color.White), bounds, Vector3.UnitZ, depth);
            batcher.SetStencil(ReadMask);
            batcher.SetBlendState(null);
        }

        public static void EndClip(UltimaBatcher2D batcher) => batcher.SetStencil(null);

        public static void Ring(UltimaBatcher2D batcher, Vector2 center, float radius, Color color, float stroke, float depth)
        {
            Texture2D line = SolidColorTextureCache.GetTexture(color);
            const int segments = 128;
            Vector2 previous = center + new Vector2(radius, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * MathHelper.TwoPi / segments;
                Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                batcher.DrawLine(line, previous, next, Vector3.UnitZ, stroke, depth);
                previous = next;
            }
        }
    }
}
