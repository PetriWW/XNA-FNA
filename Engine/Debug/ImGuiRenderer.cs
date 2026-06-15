using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;

namespace MyGame.Engine.Debug;

public class ImGuiRenderer
{
    private Game _game;
    private GraphicsDevice _graphicsDevice;
    private BasicEffect _effect = null!;
    private RasterizerState _rasterizerState;
    private byte[] _vertexData = null!;
    private VertexBuffer _vertexBuffer = null!;
    private int _vertexBufferSize;
    private byte[] _indexData = null!;
    private IndexBuffer _indexBuffer = null!;

    // ARCHITECTURE FIX: Restored the missing Index Buffer Size tracker
    private int _indexBufferSize;

    private Dictionary<IntPtr, Texture2D> _loadedTextures;
    private int _textureId;
    private IntPtr? _fontTextureId;
    private int _scrollWheelValue;

    public ImGuiRenderer(Game game)
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _graphicsDevice = game.GraphicsDevice;
        _loadedTextures = new Dictionary<IntPtr, Texture2D>();
        _rasterizerState = new RasterizerState()
        {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
        };
        SetupInput();
    }

    public virtual unsafe void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);
        var pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);
        var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        tex2d.SetData(pixels);
        if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);
        _fontTextureId = BindTexture(tex2d);
        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData();
    }

    public IntPtr BindTexture(Texture2D texture)
    {
        var id = new IntPtr(_textureId++);
        _loadedTextures.Add(id, texture);
        return id;
    }

    public void UnbindTexture(IntPtr textureId)
    {
        _loadedTextures.Remove(textureId);
    }

    public void BeforeLayout(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        ImGui.GetIO().DeltaTime = dt > 0f ? dt : (1f / 60f);

        UpdateInput();
        ImGui.NewFrame();
    }

    public void AfterLayout()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    protected virtual void SetupInput()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
    }

    protected virtual void UpdateInput()
    {
        var io = ImGui.GetIO();

        int width = _graphicsDevice.PresentationParameters.BackBufferWidth;
        int height = _graphicsDevice.PresentationParameters.BackBufferHeight;
        io.DisplaySize = new System.Numerics.Vector2(Math.Max(1, width), Math.Max(1, height));
        io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);

        if (!_game.IsActive) return;

        var mouse = Mouse.GetState();
        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);

        if (mouse.ScrollWheelValue != _scrollWheelValue)
        {
            float scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
            io.AddMouseWheelEvent(0, scrollDelta / 120f);
            _scrollWheelValue = mouse.ScrollWheelValue;
        }
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0) return;

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];
            int vtxSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vtxSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vtxSize);
                _vertexBuffer?.Dispose();
                _vertexData = new byte[newSize];
                _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, newSize / DrawVertDeclaration.Size, BufferUsage.None);
                _vertexBufferSize = newSize;
            }

            int idxSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (idxSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, idxSize);
                _indexBuffer?.Dispose();
                _indexData = new byte[newSize];
                _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, newSize / sizeof(ushort), BufferUsage.None);
                _indexBufferSize = newSize;
            }
        }

        SetupRenderState(drawData);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            Marshal.Copy(cmdList.VtxBuffer.Data, _vertexData, 0, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>());
            Marshal.Copy(cmdList.IdxBuffer.Data, _indexData, 0, cmdList.IdxBuffer.Size * sizeof(ushort));

            _vertexBuffer.SetData(_vertexData, 0, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>());
            _indexBuffer.SetData(_indexData, 0, cmdList.IdxBuffer.Size * sizeof(ushort));

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                var pcmd = cmdList.CmdBuffer[cmdi];
                if (!_loadedTextures.ContainsKey(pcmd.TextureId)) continue;

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    (int)pcmd.ClipRect.X, (int)pcmd.ClipRect.Y,
                    (int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                    (int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                _effect.Texture = _loadedTextures[pcmd.TextureId];
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cmdList.VtxBuffer.Size, (int)pcmd.IdxOffset, (int)(pcmd.ElemCount / 3));
                }
            }
        }

        _graphicsDevice.ScissorRectangle = _graphicsDevice.Viewport.Bounds;
    }

    private void SetupRenderState(ImDrawDataPtr drawData)
    {
        if (_effect == null)
        {
            _effect = new BasicEffect(_graphicsDevice)
            {
                World = Matrix.Identity,
                View = Matrix.Identity,
                TextureEnabled = true,
                VertexColorEnabled = true
            };
        }

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0f, -1f, 1f);

        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
    }

    public static class DrawVertDeclaration
    {
        public static readonly int Size = 20;
        public static readonly VertexDeclaration Declaration = new VertexDeclaration(
            Size,
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );
    }
}
