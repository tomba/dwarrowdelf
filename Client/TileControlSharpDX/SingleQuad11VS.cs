using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using System.Runtime.InteropServices;

namespace Dwarrowdelf.Client.TileControl
{
	class SingleQuad11VS : Component
	{
		Device m_device;
		VertexShader m_vertexShader;

		public ShaderBytecode Bytecode { get; private set; }

		[StructLayout(LayoutKind.Sequential)]
		struct ShaderData
		{
			public Matrix WorldMatrix;
		}

		ShaderData m_shaderData;
		Buffer m_shaderDataBuffer;

		public SingleQuad11VS(Device device)
		{
			m_device = device;

			var ass = System.Reflection.Assembly.GetCallingAssembly();

			// fxc /T vs_4_0 /E VS /Fo SingleQuad11.vso SingleQuad11.vs

			using (var stream = ass.GetManifestResourceStream("Dwarrowdelf.Client.TileControl.SingleQuad11VS.hlslo"))
			{
				var bytecode = ShaderBytecode.FromStream(stream);
				this.Bytecode = bytecode;
				Create(bytecode);
			}
		}

		void Create(ShaderBytecode bytecode)
		{
			var context = m_device.ImmediateContext;

			m_vertexShader = ToDispose(new VertexShader(m_device, bytecode));
			context.VertexShader.Set(m_vertexShader);

			m_shaderDataBuffer = ToDispose(new Buffer(m_device, Utilities.SizeOf<ShaderData>(),
				ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

			//create world matrix
			Matrix w = Matrix.Identity;
			w *= Matrix.Scaling(2.0f, 2.0f, 0);
			w *= Matrix.Translation(-1.0f, -1.0f, 0);
			w.Transpose();
			m_shaderData.WorldMatrix = w;

			context.UpdateSubresource(ref m_shaderData, m_shaderDataBuffer);
			context.VertexShader.SetConstantBuffer(0, m_shaderDataBuffer);
		}
	}
}
