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
			m_vertexShader = ToDispose(new VertexShader(m_device, bytecode));
		}

		public void Update()
		{
			var context = m_device.ImmediateContext;

			context.VertexShader.Set(m_vertexShader);
		}
	}
}
