using System;
using System.Threading.Tasks;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    public class DeferredSprite : Sprite
    {
        public Func<Texture> ResolveTexture { get; set; }
        public bool Async { get; set; } = true;
        
        private bool pending = false;
    
        protected override void ApplyDrawNode(DrawNode node)
        {
            if (ResolveTexture != null && Texture == null && !pending)
            {
                pending = true;
                if (Async)
                    Task.Factory.StartNew(ResolveTexture)
                        .ContinueWith(texture => Scheduler.Add(() => Texture = texture.Result));
                else
                    Texture = ResolveTexture();
            }
            base.ApplyDrawNode(node);
        }
    }
}