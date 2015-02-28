using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NamedPipesRcp;

namespace DemoWindowsService
{
	public partial class Service1 : ServiceBase
	{
		private PipeServer _pipeServer;

		public Service1()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				var ps = GetPipeSecurity();

				var serializer = new DataContractPipeMessageSerializer();
				_pipeServer = new PipeServer("demoPipe", serializer, ps);
				_pipeServer.RegisterHandler<string, string>("TestMessage", OnTestMessage);			
				_pipeServer.Start();
			}
			catch (Exception e)
			{
				throw new Exception(e.Message + Environment.NewLine + e.StackTrace, e);
			}
		}

		private PipeSecurity GetPipeSecurity()
		{
			var ps = new PipeSecurity();
			var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
			ps.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
			
			var currentUser = WindowsIdentity.GetCurrent();
			if (currentUser.User.AccountDomainSid != null)
			{
				sid = new SecurityIdentifier(WellKnownSidType.AccountDomainUsersSid, currentUser.User.AccountDomainSid);
				ps.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
			}

			var myPipeServerIdentity = currentUser.Owner;
			ps.AddAccessRule(new PipeAccessRule(myPipeServerIdentity, PipeAccessRights.FullControl, AccessControlType.Allow));
			return ps;
		}

		private string OnTestMessage(string request)
		{
			return "Response " + request;
		}

		protected override void OnStop()
		{
			if (_pipeServer != null)
				_pipeServer.Stop();
		}
	}
}
