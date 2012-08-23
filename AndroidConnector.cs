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

	/*! 固定データ */
	private const string __adb_device_ignore_line = "List of devices attached";
	private const string __launch_activity = "com.unity3d.player.UnityPlayerProxyActivity";

	static AndroidConnector instance = null;

	private string			m_project_root = null;						//!< Unityプロジェクトのルートディレクトリ
	private List<string>	m_deviceList = new List<string>();			//!< 接続しているAndroid端末

	private bool			m_isCompiling = false;						//!< スクリプトのコンパイル中かどうか
	private DateTime		m_LastCompilingDate = new DateTime();		//!< 最後にコンパイルをした時刻
	private DateTime		m_LastBuildDate = new DateTime();			//!< 最後にビルドした時刻

	/*! UIのカラー管理 */
	private Color			m_build_color = new Color();
	private Color			m_install_color = new Color();

	
	//----------------------------------------------//
	//! @brief ウィンドウ表示
	//! @note .....
	//----------------------------------------------//
	[MenuItem("Tools/Android Connection")]
	static void OpenWindow()
	{
		instance = EditorWindow.GetWindow< AndroidConnector >( false, "Connection Android" );

	}
	
	//----------------------------------------------//
	//! @brief 初期化
	//! @note .....
	//----------------------------------------------//
	void Awake()
	{
		// プロジェクトのルートディレクトリを取得
		m_project_root = System.IO.Directory.GetCurrentDirectory();
		UnityEngine.Debug.Log( m_project_root );
	}

	void OnDestroy()
	{
		// UnityEngine.Debug.Log( "Connection Android Windows is Destroyed" );
	}
	
	//----------------------------------------------//
	//! @brief GUI表示
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

				// ビルド対象のシーンを抽出
				List<string> buildTargetScenes = new List<string>();

				foreach( var scene in EditorBuildSettings.scenes )
				{
					if( scene.enabled )
					{
						buildTargetScenes.Add( scene.path );
					}
				}
			
				// ビルド開始
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
	//! @brief ADBに接続されているAndroid端末を表示
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
			// 出力内容を取得
			string output = _Process.StandardOutput.ReadToEnd();
			output = output.Replace( "\r\n", "\n" );

			// 各行に分割
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

					// デバイスIDの取得
					string[] deviceId = line.Split( _sep_space, System.StringSplitOptions.None );

					//UnityEngine.Debug.Log( "ID : " + deviceId[0] );

					m_deviceList.Add( deviceId[0] );

				}
			}

		}
	}

	//----------------------------------------------//
	//! @brief 指定した端末にAPKをインストール
	//! @param	deviceId	インストール先の端末ID
	//! @param	apkPath		インストールするAPKへのパス
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
	//! @brief インストールしたAPKの起動
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
