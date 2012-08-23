/*
 * Copyright (C) 2012 Kogarasi
 * Twitter	: @kogarasi_cross
 * E-mail	: kogarasi.cross@gmail.com
 * github	: https://github.com/Kogarasi/
 */

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class AndroidConnector : EditorWindow
{

	/*! �Œ�f�[�^ */
	private const string __adb_device_ignore_line = "List of devices attached";
	private const string __launch_activity = "com.unity3d.player.UnityPlayerProxyActivity";

	static AndroidConnector instance = null;

	private string			m_project_root = null;						//!< Unity�v���W�F�N�g�̃��[�g�f�B���N�g��
	private List<string>	m_deviceList = new List<string>();			//!< �ڑ����Ă���Android�[��

	private bool			m_isCompiling = false;						//!< �X�N���v�g�̃R���p�C�������ǂ���
	private DateTime		m_LastCompilingDate = new DateTime();		//!< �Ō�ɃR���p�C������������
	private DateTime		m_LastBuildDate = new DateTime();			//!< �Ō�Ƀr���h��������

	/*! UI�̃J���[�Ǘ� */
	private Color			m_build_color = new Color();
	private Color			m_install_color = new Color();

	
	//----------------------------------------------//
	//! @brief �E�B���h�E�\��
	//! @note .....
	//----------------------------------------------//
	[MenuItem("Tools/Android Connection")]
	static void OpenWindow()
	{
		instance = EditorWindow.GetWindow< AndroidConnector >( false, "Connection Android" );

	}
	
	//----------------------------------------------//
	//! @brief ������
	//! @note .....
	//----------------------------------------------//
	void Awake()
	{
		// �v���W�F�N�g�̃��[�g�f�B���N�g�����擾
		m_project_root = System.IO.Directory.GetCurrentDirectory();
		UnityEngine.Debug.Log( m_project_root );
	}

	void OnDestroy()
	{
		// UnityEngine.Debug.Log( "Connection Android Windows is Destroyed" );
	}
	
	//----------------------------------------------//
	//! @brief GUI�\��
	//! @note .....
	//----------------------------------------------//
	void OnGUI()
	{

		GUILayout.Label( "1.Build" );
		GUILayout.Space( 10 );

		
		if( m_isCompiling )
		{
			GUILayout.Label( "unity compiling script now! and wait for build" );
		}
		else
		{
			GUILayout.Label( "ready for build!" );
		}

		GUI.backgroundColor = m_build_color;
		if( GUILayout.Button( "Build APK" ) )
		{

			// Check Compiling Script
			if( EditorApplication.isCompiling == false )
			{

				// �r���h�Ώۂ̃V�[���𒊏o
				List<string> buildTargetScenes = new List<string>();

				foreach( var scene in EditorBuildSettings.scenes )
				{
					if( scene.enabled )
					{
						buildTargetScenes.Add( scene.path );
					}
				}
			
				// �r���h�J�n
				EditorUserBuildSettings.SetBuildLocation( BuildTarget.Android, m_project_root );
				BuildPipeline.BuildPlayer( buildTargetScenes.ToArray(), PlayerSettings.bundleIdentifier + ".apk", BuildTarget.Android, BuildOptions.None );

				m_LastBuildDate = DateTime.Now;
			}
			else
			{
				UnityEngine.Debug.Log( "Unity Player is Compiling Script yet" );
			}
		}


		GUILayout.Space( 10 );

		GUILayout.Label( "2.Install" );
		GUILayout.Space( 10 );

		GUI.backgroundColor = m_install_color;
		foreach( string deviceId in m_deviceList )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label( deviceId );

			GUILayout.BeginVertical();

				// install to android device
				if( GUILayout.Button( "install" ) )
				{
					installAPK( deviceId, m_project_root + "\\" + PlayerSettings.bundleIdentifier + ".apk" );
				}

				if( GUILayout.Button( "execute" ) )
				{
					executeAPK( deviceId );
				}

			GUILayout.EndVertical();

			GUILayout.EndHorizontal();
			GUILayout.Space( 10 );
		}

		if( GUILayout.Button( "all install" ) )
		{
			foreach( string deviceId in m_deviceList )
			{
				installAPK( deviceId, m_project_root + "\\" + PlayerSettings.bundleIdentifier + ".apk" );
			}
		}
	}

	
	//----------------------------------------------//
	//! @brief .....
	//! @note .....
	//----------------------------------------------//
	void Update()
	{
		if( EditorApplication.isCompiling )
		{
			m_build_color = new Color( 1.0f, 0, 0, 1.0f );

			m_isCompiling = true;
		}
		else
		{
			m_build_color = new Color( 0, 1.0f, 0, 1.0f );
		}

		if( m_isCompiling && EditorApplication.isCompiling == false )
		{
			m_isCompiling = false;
			m_LastCompilingDate = DateTime.Now;
		}

		string apk_path = m_project_root + "\\" + PlayerSettings.bundleIdentifier + ".apk";
		if( System.IO.File.Exists( apk_path ) && m_LastBuildDate > m_LastCompilingDate )
		{
			m_install_color = new Color( 0, 1, 0, 1 );
		}
		else
		{
			m_install_color = new Color( 1, 0, 0, 1 );
		}

		// Refresh Device List
		getConnectDevices();
	}

	//----------------------------------------------//
	//! @brief ADB�ɐڑ�����Ă���Android�[����\��
	//! @note .....
	//----------------------------------------------//
	void getConnectDevices()
	{

		m_deviceList.Clear();

		ProcessStartInfo _info = new ProcessStartInfo( "adb", "devices" );
		_info.CreateNoWindow = true;
		_info.RedirectStandardOutput = true;
		_info.UseShellExecute = false;

		Process _Process = new Process();
		_Process.StartInfo = _info;
		_Process.Start();

		_Process.WaitForExit();

		if( _Process.ExitCode != 0 )
		{
			UnityEngine.Debug.Log( "ERROR : " + _Process.ExitCode );
		}
		else
		{
			// �o�͓��e���擾
			string output = _Process.StandardOutput.ReadToEnd();
			output = output.Replace( "\r\n", "\n" );

			// �e�s�ɕ���
			string[] _sep_newLine = {"\n"};
			string[] lines = output.Split( _sep_newLine, System.StringSplitOptions.None );

			foreach( string line in lines )
			{
				if( line.IndexOf( __adb_device_ignore_line ) != -1 )
				{
					// UnityEngine.Debug.Log( "Ignore first line" );
					continue;
				}
				else if( line.IndexOf( "device" ) != -1 )
				{
					string[] _sep_space = {"\t"};

					// �f�o�C�XID�̎擾
					string[] deviceId = line.Split( _sep_space, System.StringSplitOptions.None );

					//UnityEngine.Debug.Log( "ID : " + deviceId[0] );

					m_deviceList.Add( deviceId[0] );

				}
			}

		}
	}

	//----------------------------------------------//
	//! @brief �w�肵���[����APK���C���X�g�[��
	//! @param	deviceId	�C���X�g�[����̒[��ID
	//! @param	apkPath		�C���X�g�[������APK�ւ̃p�X
	//! @note	.....
	//----------------------------------------------//
	void installAPK( string deviceId, string apkPath )
	{

		UnityEngine.Debug.LogWarning( "Start installing APK to " + deviceId );

		ProcessStartInfo _info = new ProcessStartInfo( "adb", "-s " + deviceId + " install -r " + apkPath );
		_info.CreateNoWindow = true;
		_info.RedirectStandardOutput = true;
		_info.UseShellExecute = false;

		Process _Process = new Process();
		_Process.StartInfo = _info;
		_Process.Start();

		_Process.WaitForExit();

		UnityEngine.Debug.LogWarning( "End installing" );
	}
	
	//----------------------------------------------//
	//! @brief �C���X�g�[������APK�̋N��
	//! @note .....
	//----------------------------------------------//
	void executeAPK( string deviceId )
	{
		UnityEngine.Debug.LogWarning( "Start installing APK to " + deviceId );


		string cmd_option = "-s " + deviceId + " shell am start -a android.intent.action.MAIN -n " + PlayerSettings.bundleIdentifier + "/" + __launch_activity;

		//UnityEngine.Debug.Log( cmd_option );

		ProcessStartInfo _info = new ProcessStartInfo( "adb", cmd_option );
		_info.CreateNoWindow = true;
		_info.RedirectStandardOutput = true;
		_info.UseShellExecute = false;

		Process _Process = new Process();
		_Process.StartInfo = _info;
		_Process.Start();

		_Process.WaitForExit();

		UnityEngine.Debug.Log( _Process.StandardOutput.ReadToEnd() );

		UnityEngine.Debug.LogWarning( "End installing" );
	}
}
