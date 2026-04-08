using System;

namespace Client.Rendering
{
    public sealed class RenderingPipelineContext
    {
        public RenderingPipelineContext(object renderTarget)
        {
            RenderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
        }

        public object RenderTarget { get; }
    }
}
