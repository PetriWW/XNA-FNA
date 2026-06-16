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
        int totalVtxCount = drawData.TotalVtxCount;
        int totalIdxCount = drawData.TotalIdxCount;

        if (totalVtxCount == 0 || totalIdxCount == 0) return;

        int vtxBytes = totalVtxCount * DrawVertDeclaration.Size;
        int idxBytes = totalIdxCount * sizeof(ushort);

        if (vtxBytes > _vertexBufferSize || _vertexBuffer == null)
        {
            _vertexBuffer?.Dispose();
            _vertexBufferSize = (int)(vtxBytes * 1.5f);
            _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize / DrawVertDeclaration.Size, BufferUsage.WriteOnly);
            _vertexData = new byte[_vertexBufferSize];
        }

        if (idxBytes > _indexBufferSize || _indexBuffer == null)
        {
            _indexBuffer?.Dispose();
            _indexBufferSize = (int)(idxBytes * 1.5f);
            _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize / sizeof(ushort), BufferUsage.WriteOnly);
            _indexData = new byte[_indexBufferSize];
        }

        int currentVtxOffset = 0;
        int currentIdxOffset = 0;

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];
            int cmdVtxBytes = cmdList.VtxBuffer.Size * DrawVertDeclaration.Size;
            int cmdIdxBytes = cmdList.IdxBuffer.Size * sizeof(ushort);

            Marshal.Copy(cmdList.VtxBuffer.Data, _vertexData, currentVtxOffset, cmdVtxBytes);
            Marshal.Copy(cmdList.IdxBuffer.Data, _indexData, currentIdxOffset, cmdIdxBytes);

            currentVtxOffset += cmdVtxBytes;
            currentIdxOffset += cmdIdxBytes;
        }

        _vertexBuffer.SetData(_vertexData, 0, vtxBytes);
        _indexBuffer.SetData(_indexData, 0, idxBytes);

        SetupRenderState(drawData);

        int globalVtxOffset = 0;
        int globalIdxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];

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

                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        globalVtxOffset,
                        0,
                        cmdList.VtxBuffer.Size,
                        globalIdxOffset + (int)pcmd.IdxOffset,
                        (int)(pcmd.ElemCount / 3)
                    );
                }
            }

            globalVtxOffset += cmdList.VtxBuffer.Size;
            globalIdxOffset += cmdList.IdxBuffer.Size;
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
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
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
