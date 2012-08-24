using System;
using System.Diagnostics;
using System.Collections.Generic;

public class ProcessManager
{
	private static List<Process>		m_ProcessList = new List<Process>();		//!< バックグラウンドで実行しているプロセス

	public static void setProcess( string program, string parameter, System.Action<object, EventArgs> callback )
	{
		Process _proc;

		ProcessStartInfo _info = new ProcessStartInfo( program, parameter );
		_info.CreateNoWindow = true;
		_info.RedirectStandardOutput = true;
		_info.UseShellExecute = false;

		_proc = Process.Start( _info );
		_proc.EnableRaisingEvents = true;
		_proc.Exited += new EventHandler( callback );

		m_ProcessList.Add( _proc );
	}

	public static void unsetProcess( Process process )
	{

		if( process.HasExited == false )
		{
			process.Kill();
		}

		if( m_ProcessList.IndexOf( process ) != 1 )
		{
			m_ProcessList.Remove( process );
		}
	}

	public static int Count{ get{ return m_ProcessList.Count; } }
}
