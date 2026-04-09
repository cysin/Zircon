using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Client.Rendering.SDL3OpenGL
{
    // ── Native handle types ─────────────────────────────────────────────────

    internal sealed class GLTextureHandle
    {
        public uint Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public RenderTextureFormat Format { get; set; }
    }

    internal sealed class GLFramebufferHandle : IDisposable
    {
        public uint FramebufferId { get; set; }
        public uint TextureId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public void Dispose()
        {
            if (FramebufferId != 0)
            {
                uint fbo = FramebufferId;
                GL.glDeleteFramebuffers(1, ref fbo);
                FramebufferId = 0;
            }

            if (TextureId != 0)
            {
                uint tex = TextureId;
                GL.glDeleteTextures(1, ref tex);
                TextureId = 0;
            }
        }
    }

    /// <summary>
    /// Manages OpenGL texture and framebuffer resources for the SDL3+OpenGL rendering pipeline.
    /// </summary>
    internal sealed class GLManager
    {
        public const int LightWidth = 1024;
        public const int LightHeight = 768;
        public const int PoisonSize = 48;

        private GLFramebufferHandle _backBufferHandle;
        private GLFramebufferHandle _scratchTarget;
        private GLFramebufferHandle _currentTarget;

        private GLTextureHandle _colourPaletteHandle;
        private byte[] _paletteData;
        private GLTextureHandle _lightTextureHandle;
        private GLTextureHandle _poisonTextureHandle;
        private uint _whitePixelId;

        private Size _backBufferSize;
        private TextureFilterMode _textureFilterMode = TextureFilterMode.Point;

        /// <summary>
        /// Initialises back buffer / scratch targets and loads built-in textures.
        /// </summary>
        public void Initialize(Size backBufferSize, Size scratchTargetSize)
        {
            _backBufferSize = backBufferSize;

            // The back buffer is represented as FBO 0 (the default framebuffer)
            _backBufferHandle = new GLFramebufferHandle
            {
                FramebufferId = 0,
                TextureId = 0,
                Width = backBufferSize.Width,
                Height = backBufferSize.Height
            };

            _scratchTarget = CreateFramebuffer(scratchTargetSize);
            _currentTarget = _backBufferHandle;

            GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, _backBufferHandle.FramebufferId);

            LoadBuiltInTextures();
        }

        // ── Texture creation / release ──────────────────────────────────────

        public RenderTexture CreateTexture(Size size, RenderTextureFormat format, RenderTextureUsage usage, RenderTexturePool pool)
        {
            GL.glGenTextures(1, out uint texId);
            GL.glBindTexture(GL.GL_TEXTURE_2D, texId);

            SetDefaultTextureParameters();

            switch (format)
            {
                case RenderTextureFormat.A8R8G8B8:
                    GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, (int)GL.GL_RGBA8,
                        size.Width, size.Height, 0,
                        GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, IntPtr.Zero);
                    break;

                case RenderTextureFormat.Dxt1:
                {
                    int blocksX = Math.Max(1, (size.Width + 3) / 4);
                    int blocksY = Math.Max(1, (size.Height + 3) / 4);
                    int imageSize = blocksX * blocksY * 8;
                    GL.glCompressedTexImage2D(GL.GL_TEXTURE_2D, 0,
                        GL.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT,
                        size.Width, size.Height, 0, imageSize, IntPtr.Zero);
                    break;
                }

                case RenderTextureFormat.Dxt5:
                {
                    int blocksX = Math.Max(1, (size.Width + 3) / 4);
                    int blocksY = Math.Max(1, (size.Height + 3) / 4);
                    int imageSize = blocksX * blocksY * 16;
                    GL.glCompressedTexImage2D(GL.GL_TEXTURE_2D, 0,
                        GL.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT,
                        size.Width, size.Height, 0, imageSize, IntPtr.Zero);
                    break;
                }
            }

            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

            var handle = new GLTextureHandle
            {
                Id = texId,
                Width = size.Width,
                Height = size.Height,
                Format = format
            };

            return RenderTexture.From(handle);
        }

        public void ReleaseTexture(RenderTexture texture)
        {
            if (texture.NativeHandle is GLTextureHandle handle && handle.Id != 0)
            {
                uint id = handle.Id;
                GL.glDeleteTextures(1, ref id);
                handle.Id = 0;
            }
        }

        // ── Texture locking ─────────────────────────────────────────────────

        public TextureLock LockTexture(RenderTexture texture, TextureLockMode mode)
        {
            if (texture.NativeHandle is not GLTextureHandle handle)
                throw new InvalidOperationException("Expected a GLTextureHandle.");

            if (handle.Format == RenderTextureFormat.Dxt1 || handle.Format == RenderTextureFormat.Dxt5)
                return CreateCompressedWriteLock(handle);

            // A8R8G8B8 path
            int rowSize = handle.Width * 4;
            int totalSize = rowSize * handle.Height;
            byte[] buffer = new byte[totalSize];
            GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr ptr = pinned.AddrOfPinnedObject();

            if (mode == TextureLockMode.ReadOnly)
            {
                // Download from GPU
                GL.glBindTexture(GL.GL_TEXTURE_2D, handle.Id);
                // glGetTexImage is not available in GL ES, but we target desktop GL 3.3.
                // Load it dynamically.
                var glGetTexImage = Marshal.GetDelegateForFunctionPointer<d_glGetTexImage>(
                    SDL3Native.SDL_GL_GetProcAddress("glGetTexImage"));

                if (glGetTexImage != null)
                    glGetTexImage(GL.GL_TEXTURE_2D, 0, GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, ptr);

                GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

                return TextureLock.From(ptr, rowSize, () => pinned.Free());
            }

            // Write / Discard path: caller fills the buffer, then we upload on dispose.
            return TextureLock.From(ptr, rowSize, () =>
            {
                try
                {
                    GL.glBindTexture(GL.GL_TEXTURE_2D, handle.Id);
                    GL.glTexSubImage2D(GL.GL_TEXTURE_2D, 0, 0, 0,
                        handle.Width, handle.Height,
                        GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, ptr);
                    GL.glBindTexture(GL.GL_TEXTURE_2D, 0);
                }
                finally
                {
                    pinned.Free();
                }
            });
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void d_glGetTexImage(uint target, int level, uint format, uint type, IntPtr pixels);

        private TextureLock CreateCompressedWriteLock(GLTextureHandle handle)
        {
            int blockSize = handle.Format == RenderTextureFormat.Dxt1 ? 8 : 16;
            int blocksX = Math.Max(1, (handle.Width + 3) / 4);
            int blocksY = Math.Max(1, (handle.Height + 3) / 4);
            int tightRowSize = blocksX * blockSize;
            int dataSize = tightRowSize * blocksY;

            byte[] buffer = new byte[dataSize];
            GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr ptr = pinned.AddrOfPinnedObject();

            uint internalFormat = handle.Format == RenderTextureFormat.Dxt1
                ? GL.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
                : GL.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;

            return TextureLock.From(ptr, tightRowSize, () =>
            {
                try
                {
                    GL.glBindTexture(GL.GL_TEXTURE_2D, handle.Id);
                    GL.glCompressedTexImage2D(GL.GL_TEXTURE_2D, 0, internalFormat,
                        handle.Width, handle.Height, 0, dataSize, ptr);
                    GL.glBindTexture(GL.GL_TEXTURE_2D, 0);
                }
                finally
                {
                    pinned.Free();
                }
            });
        }

        // ── Render target (FBO) management ──────────────────────────────────

        public RenderTargetResource CreateRenderTarget(Size size)
        {
            GLFramebufferHandle fbo = CreateFramebuffer(size);
            RenderTexture tex = RenderTexture.From(fbo);
            RenderSurface surface = RenderSurface.From(fbo);
            return RenderTargetResource.From(tex, surface);
        }

        public void ReleaseRenderTarget(RenderTargetResource renderTarget)
        {
            if (renderTarget.Surface.NativeHandle is GLFramebufferHandle fbo)
                fbo.Dispose();
        }

        public RenderSurface GetCurrentSurface()
        {
            if (_currentTarget == null)
                throw new InvalidOperationException("No current render surface.");

            return RenderSurface.From(_currentTarget);
        }

        public void SetSurface(RenderSurface surface)
        {
            if (!surface.IsValid)
                throw new ArgumentException("A valid surface handle is required.", nameof(surface));

            if (surface.NativeHandle is GLFramebufferHandle fbo)
            {
                _currentTarget = fbo;
                GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, fbo.FramebufferId);
                GL.glViewport(0, 0, fbo.Width, fbo.Height);
            }
            else
                throw new ArgumentException("Surface handle must wrap a GLFramebufferHandle.", nameof(surface));
        }

        public GLFramebufferHandle GetCurrentTargetHandle()
        {
            return _currentTarget;
        }

        public RenderSurface GetScratchSurface()
        {
            if (_scratchTarget == null)
                throw new InvalidOperationException("Scratch surface not available.");

            return RenderSurface.From(_scratchTarget);
        }

        public RenderTexture GetScratchTexture()
        {
            if (_scratchTarget == null)
                throw new InvalidOperationException("Scratch texture not available.");

            return RenderTexture.From(_scratchTarget);
        }

        public void SetBackBuffer()
        {
            _currentTarget = _backBufferHandle;
            GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, 0);
            GL.glViewport(0, 0, _backBufferHandle.Width, _backBufferHandle.Height);
        }

        public Size GetBackBufferSize()
        {
            return _backBufferSize;
        }

        public void ResizeBackBuffer(Size newSize, Size scratchTargetSize)
        {
            _backBufferSize = newSize;
            _backBufferHandle.Width = newSize.Width;
            _backBufferHandle.Height = newSize.Height;

            if (ReferenceEquals(_currentTarget, _scratchTarget))
                _currentTarget = _backBufferHandle;

            // Recreate scratch target at the new size
            _scratchTarget?.Dispose();
            _scratchTarget = CreateFramebuffer(scratchTargetSize);

            if (ReferenceEquals(_currentTarget, _backBufferHandle))
                SetBackBuffer();
        }

        // ── Color fill ──────────────────────────────────────────────────────

        public void ColorFill(RenderSurface surface, Rectangle rect, Color color)
        {
            if (!surface.IsValid)
                return;

            if (surface.NativeHandle is not GLFramebufferHandle fbo)
                return;

            // Save current FBO
            GLFramebufferHandle previous = _currentTarget;

            GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, fbo.FramebufferId);
            GL.glViewport(0, 0, fbo.Width, fbo.Height);

            GL.glEnable(GL.GL_SCISSOR_TEST);
            // OpenGL scissor is bottom-left origin, flip Y
            int scissorY = fbo.Height - rect.Bottom;
            GL.glScissor(rect.X, scissorY, rect.Width, rect.Height);

            GL.glClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            GL.glClear(GL.GL_COLOR_BUFFER_BIT);

            GL.glDisable(GL.GL_SCISSOR_TEST);

            // Restore previous FBO
            if (previous != null)
            {
                GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, previous.FramebufferId);
                GL.glViewport(0, 0, previous.Width, previous.Height);
            }
        }

        // ── Built-in textures ───────────────────────────────────────────────

        public RenderTexture GetColourPaletteTexture()
        {
            if (_colourPaletteHandle == null)
                throw new InvalidOperationException("Colour palette texture has not been initialized.");

            return RenderTexture.From(_colourPaletteHandle);
        }

        public byte[] GetColourPaletteData()
        {
            if (_paletteData == null)
                throw new InvalidOperationException("Colour palette data has not been initialized.");

            return _paletteData;
        }

        public RenderTexture GetLightTexture()
        {
            if (_lightTextureHandle == null)
                throw new InvalidOperationException("Light texture has not been initialized.");

            return RenderTexture.From(_lightTextureHandle);
        }

        public RenderTexture GetPoisonTexture()
        {
            if (_poisonTextureHandle == null)
                throw new InvalidOperationException("Poison texture has not been initialized.");

            return RenderTexture.From(_poisonTextureHandle);
        }

        // ── Texture filter ──────────────────────────────────────────────────

        public TextureFilterMode GetTextureFilterMode()
        {
            return _textureFilterMode;
        }

        public void SetTextureFilterMode(TextureFilterMode mode)
        {
            _textureFilterMode = mode;
        }

        // ── Cleanup ─────────────────────────────────────────────────────────

        public void Shutdown()
        {
            if (_colourPaletteHandle != null && _colourPaletteHandle.Id != 0)
            {
                uint id = _colourPaletteHandle.Id;
                GL.glDeleteTextures(1, ref id);
                _colourPaletteHandle.Id = 0;
            }

            if (_lightTextureHandle != null && _lightTextureHandle.Id != 0)
            {
                uint id = _lightTextureHandle.Id;
                GL.glDeleteTextures(1, ref id);
                _lightTextureHandle.Id = 0;
            }

            if (_poisonTextureHandle != null && _poisonTextureHandle.Id != 0)
            {
                uint id = _poisonTextureHandle.Id;
                GL.glDeleteTextures(1, ref id);
                _poisonTextureHandle.Id = 0;
            }

            _scratchTarget?.Dispose();
            _scratchTarget = null;

            // Back buffer handle is FBO 0 -- nothing to delete.
            _backBufferHandle = null;
            _currentTarget = null;
        }

        // ── Private helpers ─────────────────────────────────────────────────

        private GLFramebufferHandle CreateFramebuffer(Size size)
        {
            size = new Size(Math.Max(1, size.Width), Math.Max(1, size.Height));

            GL.glGenTextures(1, out uint texId);
            GL.glBindTexture(GL.GL_TEXTURE_2D, texId);
            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, (int)GL.GL_RGBA8,
                size.Width, size.Height, 0,
                GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, IntPtr.Zero);

            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, (int)GL.GL_LINEAR);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, (int)GL.GL_LINEAR);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, (int)GL.GL_CLAMP_TO_EDGE);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, (int)GL.GL_CLAMP_TO_EDGE);
            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

            GL.glGenFramebuffers(1, out uint fboId);
            GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, fboId);
            GL.glFramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT0, GL.GL_TEXTURE_2D, texId, 0);

            uint status = GL.glCheckFramebufferStatus(GL.GL_FRAMEBUFFER);
            if (status != GL.GL_FRAMEBUFFER_COMPLETE)
                Console.WriteLine($"[GLManager] Framebuffer incomplete: 0x{status:X}");
            // FBO created successfully

            GL.glBindFramebuffer(GL.GL_FRAMEBUFFER, 0);

            return new GLFramebufferHandle
            {
                FramebufferId = fboId,
                TextureId = texId,
                Width = size.Width,
                Height = size.Height
            };
        }

        private void SetDefaultTextureParameters()
        {
            int filter = _textureFilterMode == TextureFilterMode.Linear ? (int)GL.GL_LINEAR : (int)GL.GL_NEAREST;
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, filter);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, filter);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, (int)GL.GL_CLAMP_TO_EDGE);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, (int)GL.GL_CLAMP_TO_EDGE);
        }

        public uint WhitePixelId => _whitePixelId;

        private void LoadBuiltInTextures()
        {
            CreateWhitePixel();
            LoadPaletteTexture();
            LoadLightTexture();
            LoadPoisonTexture();
        }

        private void CreateWhitePixel()
        {
            GL.glGenTextures(1, out _whitePixelId);
            GL.glBindTexture(GL.GL_TEXTURE_2D, _whitePixelId);
            byte[] white = { 255, 255, 255, 255 };
            GCHandle pin = GCHandle.Alloc(white, GCHandleType.Pinned);
            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, (int)GL.GL_RGBA8, 1, 1, 0,
                GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, pin.AddrOfPinnedObject());
            pin.Free();
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, (int)GL.GL_NEAREST);
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, (int)GL.GL_NEAREST);
            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);
        }

        private void LoadPaletteTexture()
        {
            string path = Path.Combine(".", "Data", "Pallete.png");
            if (!File.Exists(path))
                return;

            // Use a platform-agnostic approach: decode PNG manually via a small embedded decoder
            // or fall back to System.Drawing on Windows.  For cross-platform, we upload raw BGRA
            // data.  As a pragmatic first step, we try System.Drawing.Common (available via NuGet on
            // all platforms) guarded by a try/catch so the pipeline still initialises without it.
            try
            {
                using var bmp = new System.Drawing.Bitmap(path);
                int w = bmp.Width;
                int h = bmp.Height;
                var rect = new Rectangle(0, 0, w, h);
                var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                _paletteData = new byte[w * h * 4];
                Marshal.Copy(data.Scan0, _paletteData, 0, _paletteData.Length);
                bmp.UnlockBits(data);

                _colourPaletteHandle = UploadBgraTexture(w, h, _paletteData);
            }
            catch
            {
                // Palette will be unavailable -- callers should handle this gracefully.
            }
        }

        private void LoadLightTexture()
        {
            byte[] lightData = LightGeneratorGL.CreateLightData(LightWidth, LightHeight);
            _lightTextureHandle = UploadBgraTexture(LightWidth, LightHeight, lightData);
        }

        private void LoadPoisonTexture()
        {
            byte[] poisonData = LightGeneratorGL.CreatePoisonData(PoisonSize, PoisonSize);
            _poisonTextureHandle = UploadBgraTexture(PoisonSize, PoisonSize, poisonData);
        }

        private GLTextureHandle UploadBgraTexture(int width, int height, byte[] data)
        {
            GL.glGenTextures(1, out uint texId);
            GL.glBindTexture(GL.GL_TEXTURE_2D, texId);
            SetDefaultTextureParameters();

            GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, (int)GL.GL_RGBA8,
                    width, height, 0,
                    GL.GL_BGRA, GL.GL_UNSIGNED_BYTE, pinned.AddrOfPinnedObject());
            }
            finally
            {
                pinned.Free();
            }

            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

            return new GLTextureHandle
            {
                Id = texId,
                Width = width,
                Height = height,
                Format = RenderTextureFormat.A8R8G8B8
            };
        }

        /// <summary>
        /// Resolves a <see cref="RenderTexture"/> to the underlying OpenGL texture ID.
        /// Supports both <see cref="GLTextureHandle"/> (regular textures) and
        /// <see cref="GLFramebufferHandle"/> (render targets whose color attachment is a texture).
        /// </summary>
        public static uint ResolveTextureId(RenderTexture texture)
        {
            if (texture.NativeHandle is GLTextureHandle th)
                return th.Id;

            if (texture.NativeHandle is GLFramebufferHandle fh)
                return fh.TextureId;

            return 0;
        }

        /// <summary>
        /// Resolves a <see cref="RenderTexture"/> to its pixel dimensions.
        /// </summary>
        public static Size ResolveTextureSize(RenderTexture texture)
        {
            if (texture.NativeHandle is GLTextureHandle th)
                return new Size(th.Width, th.Height);

            if (texture.NativeHandle is GLFramebufferHandle fh)
                return new Size(fh.Width, fh.Height);

            return Size.Empty;
        }
    }
}
