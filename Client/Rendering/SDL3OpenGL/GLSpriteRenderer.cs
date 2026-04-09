using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Client.Rendering.SDL3OpenGL
{
    /// <summary>
    /// Batched quad renderer using OpenGL 3.3.
    /// Each draw call uploads 4 vertices as a triangle strip (immediate-mode style).
    /// Shader effects (outline, grayscale, drop shadow) are supported via separate programs.
    /// </summary>
    internal sealed class GLSpriteRenderer : IDisposable
    {
        // ── Vertex layout: float2 pos, float2 tex, float4 color = 32 bytes ─

        private const int VertexSize = 32;
        private const int MaxVertices = 4;

        private uint _vao;
        private uint _vbo;

        // Shader programs
        private GLShaderProgram _spriteProgram;
        private GLShaderProgram _grayscaleProgram;
        private GLShaderProgram _outlineProgram;
        private GLShaderProgram _dropShadowProgram;

        // Uniform locations (sprite)
        private int _uMatrix;
        private int _uTexture;

        // Uniform locations (outline)
        private int _uOutlineMatrix;
        private int _uOutlineTexture;
        private int _uOutlineColor;
        private int _uOutlineTextureSize;
        private int _uOutlineThickness;
        private int _uOutlineSourceUV;

        // Uniform locations (grayscale)
        private int _uGrayscaleMatrix;
        private int _uGrayscaleTexture;

        // Uniform locations (drop shadow)
        private int _uDropShadowMatrix;
        private int _uDropShadowImgMin;
        private int _uDropShadowImgMax;
        private int _uDropShadowShadowSize;
        private int _uDropShadowMaxAlpha;
        private int _uDropShadowViewportHeight;

        private bool _disposed;

        public bool SupportsOutlineShader => _outlineProgram != null;

        public void Initialize()
        {
            CreateBuffers();
            LoadShaders();
        }

        // ── Draw operations ─────────────────────────────────────────────────

        /// <summary>
        /// Draws a textured quad with the default sprite shader.
        /// </summary>
        public uint WhitePixelId { get; set; }

        public void Draw(uint textureId, int texWidth, int texHeight,
                         RectangleF destination, RectangleF? source,
                         Color color, Matrix3x2 transform,
                         BlendMode blendMode, float opacity, float blendRate,
                         Size viewportSize, bool flipV = false)
        {
            if (_spriteProgram == null)
                return;

            ApplyBlendMode(blendMode, blendRate);

            _spriteProgram.Use();
            SetProjectionMatrix(_spriteProgram, _uMatrix, transform, viewportSize);

            GL.glActiveTexture(0x84C0); // GL_TEXTURE0
            GL.glBindTexture(GL.GL_TEXTURE_2D, textureId == 0 ? WhitePixelId : textureId);
            _spriteProgram.SetUniform(_uTexture, 0);

            UploadQuad(destination, source, texWidth, texHeight, color, opacity, 0f, false, flipV);
            GL.glDrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);
        }

        /// <summary>
        /// Draws a textured quad with the outline shader.
        /// </summary>
        public void DrawOutlined(uint textureId, int texWidth, int texHeight,
                                  RectangleF destination, RectangleF? source,
                                  Color color, Matrix3x2 transform,
                                  BlendMode blendMode, float opacity, float blendRate,
                                  Color outlineColor, float outlineThickness,
                                  Size viewportSize)
        {
            GLShaderProgram program = _outlineProgram ?? _spriteProgram;
            if (program == null)
                return;

            ApplyBlendMode(blendMode, blendRate);
            program.Use();

            int matLoc = program == _outlineProgram ? _uOutlineMatrix : _uMatrix;
            int texLoc = program == _outlineProgram ? _uOutlineTexture : _uTexture;

            SetProjectionMatrix(program, matLoc, transform, viewportSize);

            GL.glActiveTexture(0x84C0);
            GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
            program.SetUniform(texLoc, 0);

            if (program == _outlineProgram)
            {
                program.SetUniform(_uOutlineColor,
                    outlineColor.R / 255f, outlineColor.G / 255f, outlineColor.B / 255f, 1f);
                program.SetUniform(_uOutlineTextureSize, (float)texWidth, (float)texHeight);
                program.SetUniform(_uOutlineThickness, outlineThickness);

                float u1 = 0f, v1 = 0f, u2 = 1f, v2 = 1f;
                if (source.HasValue)
                {
                    u1 = source.Value.Left / texWidth;
                    v1 = source.Value.Top / texHeight;
                    u2 = source.Value.Right / texWidth;
                    v2 = source.Value.Bottom / texHeight;
                }
                program.SetUniform(_uOutlineSourceUV, u1, v1, u2, v2);
            }

            UploadQuad(destination, source, texWidth, texHeight, color, opacity, outlineThickness, true);
            GL.glDrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);
        }

        /// <summary>
        /// Draws a textured quad with the grayscale shader.
        /// </summary>
        public void DrawGrayscale(uint textureId, int texWidth, int texHeight,
                                   RectangleF destination, RectangleF? source,
                                   Color color, Matrix3x2 transform,
                                   BlendMode blendMode, float opacity, float blendRate,
                                   Size viewportSize)
        {
            GLShaderProgram program = _grayscaleProgram ?? _spriteProgram;
            if (program == null)
                return;

            ApplyBlendMode(blendMode, blendRate);
            program.Use();

            int matLoc = program == _grayscaleProgram ? _uGrayscaleMatrix : _uMatrix;
            int texLoc = program == _grayscaleProgram ? _uGrayscaleTexture : _uTexture;

            SetProjectionMatrix(program, matLoc, transform, viewportSize);

            GL.glActiveTexture(0x84C0);
            GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
            program.SetUniform(texLoc, 0);

            UploadQuad(destination, source, texWidth, texHeight, color, opacity, 0f, false);
            GL.glDrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);
        }

        /// <summary>
        /// Draws a drop shadow effect quad.
        /// </summary>
        public void DrawDropShadow(uint textureId, int texWidth, int texHeight,
                                    RectangleF destination, RectangleF shadowBounds,
                                    RectangleF? source,
                                    Color color, Matrix3x2 transform,
                                    BlendMode blendMode, float opacity, float blendRate,
                                    Color shadowColor, float shadowWidth, float shadowMaxOpacity,
                                    Size viewportSize)
        {
            GLShaderProgram program = _dropShadowProgram ?? _spriteProgram;
            if (program == null)
                return;

            ApplyBlendMode(blendMode, blendRate);
            program.Use();

            int matLoc = program == _dropShadowProgram ? _uDropShadowMatrix : _uMatrix;
            SetProjectionMatrix(program, matLoc, transform, viewportSize);

            if (program == _dropShadowProgram)
            {
                program.SetUniform(_uDropShadowImgMin, shadowBounds.Left, shadowBounds.Top);
                program.SetUniform(_uDropShadowImgMax, shadowBounds.Right, shadowBounds.Bottom);
                program.SetUniform(_uDropShadowShadowSize, shadowWidth);
                program.SetUniform(_uDropShadowMaxAlpha, shadowMaxOpacity);
                program.SetUniform(_uDropShadowViewportHeight, (float)viewportSize.Height);
            }

            // Drop shadow does not sample a texture -- it uses gl_FragCoord.
            // But the vertex shader still needs the quad geometry.
            UploadQuad(destination, source, texWidth, texHeight, color, opacity, shadowWidth, false);
            GL.glDrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);
        }

        /// <summary>
        /// Draws lines using the sprite shader with a 1x1 white pixel approach.
        /// </summary>
        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float opacity, float lineWidth, Size viewportSize)
        {
            // Use GL line drawing for simplicity
            if (_spriteProgram == null)
                return;

            _spriteProgram.Use();
            SetProjectionMatrix(_spriteProgram, _uMatrix, Matrix3x2.Identity, viewportSize);

            GL.glLineWidth(lineWidth);

            // Upload 2 vertices as a line
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float a = (color.A / 255f) * opacity;

            float[] vertices = new float[]
            {
                x1, y1, 0f, 0f, r, g, b, a,
                x2, y2, 0f, 0f, r, g, b, a
            };

            GL.glBindVertexArray(_vao);
            GL.glBindBuffer(GL.GL_ARRAY_BUFFER, _vbo);

            GCHandle pinned = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            try
            {
                GL.glBufferSubData(GL.GL_ARRAY_BUFFER, IntPtr.Zero,
                    (IntPtr)(vertices.Length * sizeof(float)),
                    pinned.AddrOfPinnedObject());
            }
            finally
            {
                pinned.Free();
            }

            // Bind a blank texture (texture unit 0 with no texture bound will produce white in most drivers)
            GL.glActiveTexture(0x84C0);
            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);
            _spriteProgram.SetUniform(_uTexture, 0);

            GL.glDrawArrays(0x0001, 0, 2); // GL_LINES = 0x0001
        }

        // ── Blend mode mapping ──────────────────────────────────────────────

        public void ApplyBlendMode(BlendMode mode, float blendRate)
        {
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendEquation(GL.GL_FUNC_ADD);
            GL.glBlendColor(blendRate, blendRate, blendRate, blendRate);

            switch (mode)
            {
                case BlendMode.NORMAL:
                case BlendMode.LIGHT:
                case BlendMode.LIGHTINV:
                case BlendMode.INVNORMAL:
                case BlendMode.INVLIGHTINV:
                case BlendMode.INVCOLOR:
                case BlendMode.INVBACKGROUND:
                    // Screen blend: SrcRGB = InvDstColor, DstRGB = One
                    GL.glBlendFuncSeparate(
                        GL.GL_ONE_MINUS_DST_COLOR, GL.GL_ONE,
                        GL.GL_ONE_MINUS_DST_ALPHA, GL.GL_ONE);
                    break;

                case BlendMode.INVLIGHT:
                    // BlendFactor, InverseSourceColor
                    GL.glBlendFuncSeparate(
                        GL.GL_CONSTANT_COLOR, GL.GL_ONE_MINUS_SRC_COLOR,
                        GL.GL_CONSTANT_COLOR, GL.GL_ONE_MINUS_SRC_ALPHA);
                    break;

                case BlendMode.COLORFY:
                    // SourceAlpha, One
                    GL.glBlendFuncSeparate(
                        GL.GL_SRC_ALPHA, GL.GL_ONE,
                        GL.GL_SRC_ALPHA, GL.GL_ONE);
                    break;

                case BlendMode.MASK:
                    // Zero, InverseSourceAlpha
                    GL.glBlendFuncSeparate(
                        GL.GL_ZERO, GL.GL_ONE_MINUS_SRC_ALPHA,
                        GL.GL_ZERO, GL.GL_ONE_MINUS_SRC_ALPHA);
                    break;

                case BlendMode.EFFECTMASK:
                    // DestAlpha, One
                    GL.glBlendFuncSeparate(
                        GL.GL_DST_ALPHA, GL.GL_ONE,
                        GL.GL_DST_ALPHA, GL.GL_ONE);
                    break;

                case BlendMode.HIGHLIGHT:
                    // BlendFactor, One
                    GL.glBlendFuncSeparate(
                        GL.GL_CONSTANT_COLOR, GL.GL_ONE,
                        GL.GL_CONSTANT_COLOR, GL.GL_ONE);
                    break;

                case BlendMode.LIGHTMAP:
                    // Zero, SourceColor (alpha: Zero, SourceAlpha)
                    GL.glBlendFuncSeparate(
                        GL.GL_ZERO, GL.GL_SRC_COLOR,
                        GL.GL_ZERO, GL.GL_SRC_ALPHA);
                    break;

                case BlendMode.NONE:
                default:
                    // Standard alpha blending
                    GL.glBlendFuncSeparate(
                        GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA,
                        GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
                    break;
            }
        }

        /// <summary>
        /// Sets standard alpha blending (non-special blend mode rendering).
        /// </summary>
        public void SetDefaultBlend()
        {
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendEquation(GL.GL_FUNC_ADD);
            GL.glBlendFuncSeparate(
                GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA,
                GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
        }

        public void FlushSprite()
        {
            // In an immediate-mode-style renderer each Draw already issues glDrawArrays,
            // so there is nothing to flush.  This is here for interface parity.
        }

        // ── Lifecycle ───────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _spriteProgram?.Dispose();
            _grayscaleProgram?.Dispose();
            _outlineProgram?.Dispose();
            _dropShadowProgram?.Dispose();

            if (_vbo != 0) { GL.glDeleteBuffers(1, ref _vbo); _vbo = 0; }
            if (_vao != 0) { GL.glDeleteVertexArrays(1, ref _vao); _vao = 0; }
        }

        // ── Private helpers ─────────────────────────────────────────────────

        private void CreateBuffers()
        {
            GL.glGenVertexArrays(1, out _vao);
            GL.glBindVertexArray(_vao);

            GL.glGenBuffers(1, out _vbo);
            GL.glBindBuffer(GL.GL_ARRAY_BUFFER, _vbo);

            // Allocate space for 4 vertices (triangle strip quad)
            GL.glBufferData(GL.GL_ARRAY_BUFFER, (IntPtr)(VertexSize * MaxVertices), IntPtr.Zero, GL.GL_DYNAMIC_DRAW);

            // Position: location 0, float2, offset 0
            GL.glEnableVertexAttribArray(0);
            GL.glVertexAttribPointer(0, 2, GL.GL_FLOAT, false, VertexSize, IntPtr.Zero);

            // TexCoord: location 1, float2, offset 8
            GL.glEnableVertexAttribArray(1);
            GL.glVertexAttribPointer(1, 2, GL.GL_FLOAT, false, VertexSize, (IntPtr)8);

            // Color: location 2, float4, offset 16
            GL.glEnableVertexAttribArray(2);
            GL.glVertexAttribPointer(2, 4, GL.GL_FLOAT, false, VertexSize, (IntPtr)16);

            GL.glBindVertexArray(0);
        }

        private void LoadShaders()
        {
            string shaderDir = FindShaderDirectory();

            if (shaderDir == null)
                return;

            string vertPath = Path.Combine(shaderDir, "Sprite.vert");
            string fragPath = Path.Combine(shaderDir, "Sprite.frag");
            string outlinePath = Path.Combine(shaderDir, "Outline.frag");
            string grayscalePath = Path.Combine(shaderDir, "Grayscale.frag");
            string dropShadowPath = Path.Combine(shaderDir, "DropShadow.frag");

            _spriteProgram = GLShaderProgram.FromFiles(vertPath, fragPath);
            if (_spriteProgram != null)
            {
                _uMatrix = _spriteProgram.GetUniformLocation("uMatrix");
                _uTexture = _spriteProgram.GetUniformLocation("uTexture");
            }

            _outlineProgram = GLShaderProgram.FromFiles(vertPath, outlinePath);
            if (_outlineProgram != null)
            {
                _uOutlineMatrix = _outlineProgram.GetUniformLocation("uMatrix");
                _uOutlineTexture = _outlineProgram.GetUniformLocation("uTexture");
                _uOutlineColor = _outlineProgram.GetUniformLocation("uOutlineColor");
                _uOutlineTextureSize = _outlineProgram.GetUniformLocation("uTextureSize");
                _uOutlineThickness = _outlineProgram.GetUniformLocation("uOutlineThickness");
                _uOutlineSourceUV = _outlineProgram.GetUniformLocation("uSourceUV");
            }

            _grayscaleProgram = GLShaderProgram.FromFiles(vertPath, grayscalePath);
            if (_grayscaleProgram != null)
            {
                _uGrayscaleMatrix = _grayscaleProgram.GetUniformLocation("uMatrix");
                _uGrayscaleTexture = _grayscaleProgram.GetUniformLocation("uTexture");
            }

            _dropShadowProgram = GLShaderProgram.FromFiles(vertPath, dropShadowPath);
            if (_dropShadowProgram != null)
            {
                _uDropShadowMatrix = _dropShadowProgram.GetUniformLocation("uMatrix");
                _uDropShadowImgMin = _dropShadowProgram.GetUniformLocation("uImgMin");
                _uDropShadowImgMax = _dropShadowProgram.GetUniformLocation("uImgMax");
                _uDropShadowShadowSize = _dropShadowProgram.GetUniformLocation("uShadowSize");
                _uDropShadowMaxAlpha = _dropShadowProgram.GetUniformLocation("uMaxAlpha");
                _uDropShadowViewportHeight = _dropShadowProgram.GetUniformLocation("uViewportHeight");
            }
        }

        private static string FindShaderDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Rendering", "SDL3OpenGL", "Shaders"),
                Path.Combine(baseDir, "Shaders"),
                Path.Combine(baseDir, "..", "Client", "Rendering", "SDL3OpenGL", "Shaders"),
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }

            return null;
        }

        private unsafe void SetProjectionMatrix(GLShaderProgram program, int uniformLocation, Matrix3x2 transform, Size viewportSize)
        {
            float width = viewportSize.Width;
            float height = viewportSize.Height;

            // Orthographic projection: top-left (0,0) to bottom-right (w,h)
            // OpenGL NDC: X maps to -1..1, Y maps to -1..1.
            // X: (x / W) * 2 - 1
            // Y: 1 - (y / H) * 2  (flip Y so top-left is origin)

            Matrix4x4 projection = Matrix4x4.Identity;
            projection.M11 = 2f / width;
            projection.M22 = -2f / height;
            projection.M41 = -1f;
            projection.M42 = 1f;

            // Extend the 3x2 transform to 4x4
            Matrix4x4 world = Matrix4x4.Identity;
            world.M11 = transform.M11;
            world.M12 = transform.M12;
            world.M21 = transform.M21;
            world.M22 = transform.M22;
            world.M41 = transform.M31;
            world.M42 = transform.M32;

            Matrix4x4 final = world * projection;

            // Match D3D11: transpose the matrix for the shader.
            // The GLSL shader uses vec4 * mat4 (row-vector convention) matching HLSL's mul(vec4, Matrix).
            final = Matrix4x4.Transpose(final);

            program.SetUniformMatrix4(uniformLocation, final);
        }

        private unsafe void UploadQuad(RectangleF destination, RectangleF? source,
                                        int texWidth, int texHeight,
                                        Color color, float opacity,
                                        float geometryExpand, bool expandUvs,
                                        bool flipV = false)
        {
            float left = destination.Left;
            float right = destination.Right;
            float top = destination.Top;
            float bottom = destination.Bottom;

            float u1 = 0f, v1 = 0f, u2 = 1f, v2 = 1f;
            if (source.HasValue)
            {
                u1 = source.Value.Left / (float)texWidth;
                v1 = source.Value.Top / (float)texHeight;
                u2 = source.Value.Right / (float)texWidth;
                v2 = source.Value.Bottom / (float)texHeight;
            }

            // FBO textures in OpenGL are Y-flipped: V=0 is bottom, V=1 is top.
            // Flip V coordinates to sample correctly.
            if (flipV)
            {
                float tmp = v1;
                v1 = 1f - v2;
                v2 = 1f - tmp;
            }

            if (geometryExpand > 0f)
            {
                left -= geometryExpand;
                right += geometryExpand;
                top -= geometryExpand;
                bottom += geometryExpand;

                if (expandUvs && texWidth > 0 && texHeight > 0)
                {
                    float uPad = geometryExpand / texWidth;
                    float vPad = geometryExpand / texHeight;
                    u1 -= uPad;
                    v1 -= vPad;
                    u2 += uPad;
                    v2 += vPad;
                }
            }

            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float a = (color.A / 255f) * opacity;

            // Triangle strip: TL, TR, BL, BR
            float[] vertices = new float[]
            {
                left,  top,    u1, v1, r, g, b, a,
                right, top,    u2, v1, r, g, b, a,
                left,  bottom, u1, v2, r, g, b, a,
                right, bottom, u2, v2, r, g, b, a,
            };

            GL.glBindVertexArray(_vao);
            GL.glBindBuffer(GL.GL_ARRAY_BUFFER, _vbo);

            GCHandle pinned = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            try
            {
                GL.glBufferSubData(GL.GL_ARRAY_BUFFER, IntPtr.Zero,
                    (IntPtr)(vertices.Length * sizeof(float)),
                    pinned.AddrOfPinnedObject());
            }
            finally
            {
                pinned.Free();
            }
        }
    }
}
