using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Dwarrowdelf.Client.TileControl
{
	public class TestScene : IScene
	{
		VertexShader m_vertexShader;
		PixelShader m_pixelShader;
		InputLayout m_inputLayout;
		Buffer m_vertexBuffer;
		Buffer m_constantBuffer;
		Device m_device;
		Matrix m_viewProj;

		System.Diagnostics.Stopwatch m_stopwatch;

		public TestScene()
		{
		}

		public void Attach(ISceneHost host)
		{
			var device = host.Device;
			m_device = device;

			var vsCode = ShaderBytecode.Compile(s_shaderCode, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
			m_vertexShader = new VertexShader(device, vsCode);

			var psCode = ShaderBytecode.Compile(s_shaderCode, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);
			m_pixelShader = new PixelShader(device, psCode);

			m_vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, s_vertices);

			m_inputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vsCode), new[]
			{
				new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
				new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
			});

			m_constantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			m_stopwatch = System.Diagnostics.Stopwatch.StartNew();
		}

		public void Detach()
		{
			DH.Dispose(ref m_constantBuffer);
			DH.Dispose(ref m_pixelShader);

			DH.Dispose(ref m_vertexBuffer);
			DH.Dispose(ref m_inputLayout);
			DH.Dispose(ref m_vertexShader);
		}

		public void Update(TimeSpan timeSpan)
		{
		}

		public void SetRenderSize(float width, float height)
		{
			var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
			float aspect = (float)width / height;
			var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, aspect, 0.1f, 100.0f);
			m_viewProj = Matrix.Multiply(view, proj);
		}

		public void Render()
		{
			var context = m_device.ImmediateContext;

			float time = (float)m_stopwatch.Elapsed.TotalSeconds;
			var worldViewProj = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * m_viewProj;
			worldViewProj.Transpose();
			context.UpdateSubresource(ref worldViewProj, m_constantBuffer);

			context.InputAssembler.InputLayout = m_inputLayout;
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_vertexBuffer, Utilities.SizeOf<Vector4>() * 2, 0));
			context.VertexShader.SetConstantBuffer(0, m_constantBuffer);
			context.VertexShader.Set(m_vertexShader);
			context.PixelShader.Set(m_pixelShader);

			context.Draw(36, 0);
		}

		static Vector4[] s_vertices = new[]
			{
				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front
				new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
				new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),

				new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK
				new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),

				new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top
				new Vector4(-1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),

				new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
				new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
				new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),

				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left
				new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
				new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),

				new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right
				new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
				new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
};


		static string s_shaderCode = @"
struct VS_IN
{
	float4 pos : POSITION;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

float4x4 worldViewProj;

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = mul(input.pos, worldViewProj);
	output.col = input.col;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}
";
	}
}
