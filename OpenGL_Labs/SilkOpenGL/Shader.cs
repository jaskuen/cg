using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;

namespace SilkOpenGL;

public class Shader : IDisposable
{
    private uint _handle;
    private GL _gl;

    // ─── поля, которые можно задать заранее ───
    private readonly string _vertexPath;
    private readonly string _fragmentPath;
    private bool _isCompiled;

    public uint ProgramId => _handle;

    public Shader(string vertexPath, string fragmentPath)
    {
        _vertexPath = vertexPath ?? throw new ArgumentNullException(nameof(vertexPath));
        _fragmentPath = fragmentPath ?? throw new ArgumentNullException(nameof(fragmentPath));
        _isCompiled = false;
    }

    // Вызывается в момент запуска GL
    public void Compile(GL gl)
    {
        if (_isCompiled) return;
        if (gl == null) throw new ArgumentNullException(nameof(gl));

        _gl = gl;

        uint vertex = LoadShader(ShaderType.VertexShader, _vertexPath);
        uint fragment = LoadShader(ShaderType.FragmentShader, _fragmentPath);

        _handle = _gl.CreateProgram();
        _gl.AttachShader(_handle, vertex);
        _gl.AttachShader(_handle, fragment);
        _gl.LinkProgram(_handle);

        _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new Exception($"Program failed to link: {_gl.GetProgramInfoLog(_handle)}");
        }

        _gl.DetachShader(_handle, vertex);
        _gl.DetachShader(_handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        _isCompiled = true;
    }

    public void Use()
    {
        if (!_isCompiled) throw new InvalidOperationException("Shader not compiled yet");
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.UseProgram(_handle);
    }

    //Uniforms are properties that applies to the entire geometry
    public void SetUniform(string name, int value)
    {
        //Setting a uniform on a shader using a name.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) //If GetUniformLocation returns -1 the uniform is not found.
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, uint value)
    {
        //Setting a uniform on a shader using a name.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) //If GetUniformLocation returns -1 the uniform is not found.
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform1(location, value);
    }

    public void SetUniform(string name, Vector3 value)
    {
        //Setting a uniform on a shader using a name.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1) //If GetUniformLocation returns -1 the uniform is not found.
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform3(location, value);
    }

    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        //A new overload has been created for setting a uniform so we can use the transform in our shader.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.UniformMatrix4(location, 1, false, (float*)&value);
    }

    public unsafe void SetUniform(string name, Color color)
    {
        //A new overload has been created for setting a uniform so we can use the transform in our shader.
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform4(location, color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public void SetUniform(string name, float value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }

        _gl.Uniform1(location, value);
    }

    public bool TrySetUniform(string name, int value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            return false;
        }

        _gl.Uniform1(location, value);
        return true;
    }

    public bool TrySetUniform(string name, float value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            return false;
        }

        _gl.Uniform1(location, value);
        return true;
    }

    public bool TrySetUniform(string name, Vector3 value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            return false;
        }

        _gl.Uniform3(location, value);
        return true;
    }

    public unsafe bool TrySetUniform(string name, Matrix4x4 value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            return false;
        }

        _gl.UniformMatrix4(location, 1, false, (float*)&value);
        return true;
    }

    public bool TrySetUniform(string name, uint value)
    {
        int location = _gl.GetUniformLocation(_handle, name);
        if (location == -1)
        {
            return false;
        }

        _gl.Uniform1(location, value);
        return true;
    }

    public void Dispose()
    {
        if (_isCompiled && _gl != null)
        {
            _gl.DeleteProgram(_handle);
        }
    }

    private uint LoadShader(ShaderType type, string path)
    {
        string src = File.ReadAllText(path);
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, src);
        _gl.CompileShader(handle);

        string infoLog = _gl.GetShaderInfoLog(handle);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling {type}: {infoLog}");
        }

        return handle;
    }
}