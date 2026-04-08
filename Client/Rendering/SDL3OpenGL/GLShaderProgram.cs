using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Client.Rendering.SDL3OpenGL
{
    /// <summary>
    /// Compiles, links and manages a GLSL shader program (vertex + fragment).
    /// </summary>
    internal sealed class GLShaderProgram : IDisposable
    {
        public uint ProgramId { get; private set; }

        private bool _disposed;

        private GLShaderProgram(uint programId)
        {
            ProgramId = programId;
        }

        /// <summary>
        /// Creates a shader program from vertex and fragment shader source files.
        /// Returns null if either file is missing or compilation/linking fails.
        /// </summary>
        public static GLShaderProgram FromFiles(string vertexPath, string fragmentPath)
        {
            if (!File.Exists(vertexPath) || !File.Exists(fragmentPath))
                return null;

            string vertexSource = File.ReadAllText(vertexPath);
            string fragmentSource = File.ReadAllText(fragmentPath);

            return FromSource(vertexSource, fragmentSource);
        }

        /// <summary>
        /// Creates a shader program from vertex and fragment shader source strings.
        /// Returns null if compilation or linking fails.
        /// </summary>
        public static GLShaderProgram FromSource(string vertexSource, string fragmentSource)
        {
            uint vertexShader = CompileShader(GL.GL_VERTEX_SHADER, vertexSource);
            if (vertexShader == 0)
                return null;

            uint fragmentShader = CompileShader(GL.GL_FRAGMENT_SHADER, fragmentSource);
            if (fragmentShader == 0)
            {
                GL.glDeleteShader(vertexShader);
                return null;
            }

            uint program = GL.glCreateProgram();
            GL.glAttachShader(program, vertexShader);
            GL.glAttachShader(program, fragmentShader);
            GL.glLinkProgram(program);

            GL.glGetProgramiv(program, GL.GL_LINK_STATUS, out int linkStatus);
            if (linkStatus == 0)
            {
                string log = GetProgramInfoLog(program);
                Console.WriteLine($"[GLShaderProgram] Link error: {log}");
                GL.glDeleteProgram(program);
                GL.glDeleteShader(vertexShader);
                GL.glDeleteShader(fragmentShader);
                return null;
            }

            // Shaders can be detached/deleted after linking
            GL.glDeleteShader(vertexShader);
            GL.glDeleteShader(fragmentShader);

            return new GLShaderProgram(program);
        }

        public void Use()
        {
            GL.glUseProgram(ProgramId);
        }

        public int GetUniformLocation(string name)
        {
            return GL.glGetUniformLocation(ProgramId, name);
        }

        public void SetUniform(int location, int value)
        {
            GL.glUniform1i(location, value);
        }

        public void SetUniform(int location, float value)
        {
            GL.glUniform1f(location, value);
        }

        public void SetUniform(int location, float x, float y)
        {
            GL.glUniform2f(location, x, y);
        }

        public void SetUniform(int location, float x, float y, float z)
        {
            GL.glUniform3f(location, x, y, z);
        }

        public void SetUniform(int location, float x, float y, float z, float w)
        {
            GL.glUniform4f(location, x, y, z, w);
        }

        public unsafe void SetUniformMatrix4(int location, Matrix4x4 matrix)
        {
            float* ptr = (float*)&matrix;
            GL.glUniformMatrix4fv(location, 1, false, ptr);
        }

        // ── Private helpers ─────────────────────────────────────────────────

        private static uint CompileShader(uint type, string source)
        {
            uint shader = GL.glCreateShader(type);
            string[] sources = new[] { source };
            GL.glShaderSource(shader, 1, sources, null);
            GL.glCompileShader(shader);

            GL.glGetShaderiv(shader, GL.GL_COMPILE_STATUS, out int compileStatus);
            if (compileStatus == 0)
            {
                string log = GetShaderInfoLog(shader);
                string typeName = type == GL.GL_VERTEX_SHADER ? "vertex" : "fragment";
                Console.WriteLine($"[GLShaderProgram] {typeName} shader compile error: {log}");
                GL.glDeleteShader(shader);
                return 0;
            }

            return shader;
        }

        private static string GetShaderInfoLog(uint shader)
        {
            GL.glGetShaderiv(shader, GL.GL_INFO_LOG_LENGTH, out int length);
            if (length <= 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                GL.glGetShaderInfoLog(shader, length, out _, buffer);
                return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string GetProgramInfoLog(uint program)
        {
            GL.glGetProgramiv(program, GL.GL_INFO_LOG_LENGTH, out int length);
            if (length <= 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal(length);
            try
            {
                GL.glGetProgramInfoLog(program, length, out _, buffer);
                return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (ProgramId != 0)
            {
                GL.glDeleteProgram(ProgramId);
                ProgramId = 0;
            }
        }
    }
}
