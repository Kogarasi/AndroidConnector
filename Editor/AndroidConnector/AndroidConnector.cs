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

	private string			m_project_root = null;							//!< Unityプロジェクトのルートディレクトリ
	private string			m_adb_path = "C:/android-sdk-windows/platform-tools/adb.exe";		//!< adbへのパス

	private bool			m_isCompiling = false;							//!< スクリプトのコンパイル中かどうか
	private DateTime		m_LastCompilingDate = new DateTime();			//!< 最後にコンパイルをした時刻
	private DateTime		m_LastBuildDate;								//!< 最後にビルドした時刻

	private List<AdbDeviceInfo>	m_deviceList = new List<AdbDeviceInfo>();	//!< 接続しているAndroid端末


	/*! UIのカラー管理 */
	private Color			m_build_color = new Color();
	private Color			m_install_color = new Color();

	
	//----------------------------------------------//
	//! @brief ウィンドウ表示
	//! @note .....
	//----------------------------------------------//
	[MenuItem("Tools/Android Connector")]
	static void OpenWindow()
	{
		instance = EditorWindow.GetWindow< AndroidConnector >( false, "Android Connector" );
	}
	
	//----------------------------------------------//
	//! @brief 初期化
	//! @note .....
	//----------------------------------------------//
	void Awake()
	{
		// プロジェクトのルートディレクトリを取得
		m_project_root = System.IO.Directory.GetCurrentDirectory();
		//UnityEngine.Debug.Log( m_project_root );
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
		EditorGUILayout.BeginVertical();

		EditorGUILayout.LabelField( "0.Initialize" );
		EditorGUILayout.LabelField( "Select \"adb\" directory" );

		EditorGUILayout.BeginHorizontal();
			m_adb_path = EditorGUILayout.TextField( m_adb_path );
			if( GUILayout.Button( "Browse" ) )
			{

				string _temp_path = m_adb_path;
				m_adb_path = EditorUtility.OpenFilePanel( "adb.exeの場所", "C:\\android-sdk-windows\\platform-tools\\adb.exe", "exe" );

				if( m_adb_path == "" )
				{
					m_adb_path = _temp_path;
				}
				//UnityEngine.Debug.Log( m_adb_path );
			}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "1.Build" );
		EditorGUILayout.Space();
		
		if( m_isCompiling )
		{
			GUI.color = Color.red;
			EditorGUILayout.LabelField( "Unity compiling script now! and wait for build" );
			GUI.color = Color.white;
		}
		else
		{
			EditorGUILayout.LabelField( "Ready for Build!" );
		}

		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField( "Last Compile Script [ " + m_LastCompilingDate.ToLongTimeString() + " ]" );
		EditorGUILayout.LabelField( "Last Build [ " + m_LastBuildDate.ToLongTimeString() + " ]" );


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
				string _error = BuildPipeline.BuildPlayer( buildTargetScenes.ToArray(), PlayerSettings.bundleIdentifier + ".apk", BuildTarget.Android, BuildOptions.None );

				UnityEngine.Debug.Log( _error );

				m_LastBuildDate = System.IO.File.GetLastWriteTime( PlayerSettings.bundleIdentifier + ".apk" ).ToLocalTime();

				// インストールステータスを初期化
				foreach( AdbDeviceInfo device in m_deviceList )
				{
					device.isInstalled = false;
				}
			}
			else
			{
				UnityEngine.Debug.Log( "Unity Player is Compiling Script yet" );
			}
		}

		EditorGUILayout.Space();

		EditorGUILayout.LabelField( "2.Install" );
		EditorGUILayout.Space();

		GUI.backgroundColor = m_install_color;

		{
			AdbDeviceInfo[] DeviceTemp = m_deviceList.ToArray();
			foreach( AdbDeviceInfo device in DeviceTemp )
			{
				EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
						EditorGUILayout.LabelField( device.DeviceName );
						EditorGUILayout.LabelField( device.DeviceId );
					EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical();

					// install to android device
					if( GUILayout.Button( "install" ) )
					{
						installAPK( device.DeviceId, m_project_root + "\\" + PlayerSettings.bundleIdentifier + ".apk" );
					}

					if( GUILayout.Button( "execute" ) )
					{
						executeAPK( device.DeviceId );
					}

				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();

					if( device.isInstalled )
					{
						GUI.color = Color.green;
						EditorGUILayout.LabelField( "[Installed]" );
						GUI.color = Color.white;
					}
					else
					{
						GUI.color = Color.red;
						EditorGUILayout.LabelField( "[No Install]" );
						GUI.color = Color.white;
					}

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
			}
		}

		if( GUILayout.Button( "all install" ) )
		{
			AdbDeviceInfo[] DeviceTemp = m_deviceList.ToArray();
			foreach( AdbDeviceInfo device in DeviceTemp )
			{
				installAPK( device.DeviceId, m_project_root + "\\" + PlayerSettings.bundleIdentifier + ".apk" );
			}
		}

		EditorGUILayout.EndVertical();
	}

	
	//----------------------------------------------//
	//! @brief .....
	//! @note .....
	//----------------------------------------------//
	void Update()
	{
		m_LastBuildDate = System.IO.File.GetLastWriteTime( PlayerSettings.bundleIdentifier + ".apk" ).ToLocalTime();

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
		if( ProcessManager.Count == 0 )
		{
			getConnectDevices();
		}
	}

	//----------------------------------------------//
	//! @brief ADBに接続されているAndroid端末を表示
	//! @note .....
	//----------------------------------------------//
	void getConnectDevices()
	{

		ProcessManager.setProcess( m_adb_path, "devices", (o, e)=>{
			Process Sender = (Process)o;

			if( Sender.ExitCode != 0 )
			{
				UnityEngine.Debug.LogError( "adb devices error : " + Sender.ExitCode );
				return;
			}

			//m_deviceList.Clear();
			List<AdbDeviceInfo> tempList = new List<AdbDeviceInfo>();
			
			// 出力内容を取得
			string output = Sender.StandardOutput.ReadToEnd();
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

					AdbDeviceInfo _devInfo = null;
					

					// デバイスIDの取得
					string[] deviceId = line.Split( _sep_space, System.StringSplitOptions.None );

					// 前のと比較
					foreach( AdbDeviceInfo _prevDevInfo in m_deviceList )
					{
						if( _prevDevInfo.DeviceId == deviceId[0] )
						{
							_devInfo = _prevDevInfo;
							break;
						}
					}

					// 新しいデバイス
					if( _devInfo == null )
					{
						_devInfo = new AdbDeviceInfo();
						_devInfo.DeviceId = deviceId[0];
					}

					// デバイス名の取得
					if( _devInfo.DeviceName == "" )
					{
						getDeviceName( _devInfo.DeviceId, (name)=>{
							_devInfo.DeviceName = name;
						} );
					}

					//UnityEngine.Debug.Log( "ID : " + deviceId[0] );

					tempList.Add( _devInfo );

				}
			}

			m_deviceList = tempList;

			ProcessManager.unsetProcess(Sender);
		});
	}

	//----------------------------------------------//
	//! @brief 指定した端末にAPKをインストール
	//! @param	deviceId	インストール先の端末ID
	//! @param	apkPath		インストールするAPKへのパス
	//! @note	.....
	//----------------------------------------------//
	void installAPK( string deviceId, string apkPath )
	{
	
		UnityEngine.Debug.Log( "Start installing APK [ " + deviceId + " ]" );

		string cmd_option = "-s " + deviceId + " install -r " + apkPath;

		ProcessManager.setProcess( m_adb_path, cmd_option, (o,e)=>{
			Process sender = (Process)o;

			UnityEngine.Debug.Log( "End installing [ " + deviceId + " ]" );
			
			AdbDeviceInfo[] tempList = m_deviceList.ToArray();
			foreach( AdbDeviceInfo device in tempList )
			{
				if( device.DeviceId == deviceId )
				{
					device.isInstalled = true;
					break;
				}
			}

			ProcessManager.unsetProcess(sender);

		});

	}
	
	//----------------------------------------------//
	//! @brief インストールしたAPKの起動
	//! @note .....
	//----------------------------------------------//
	void executeAPK( string deviceId )
	{
		UnityEngine.Debug.Log( "Launch Activity [" + deviceId + "]" );

		string cmd_option = "-s " + deviceId + " shell am start -a android.intent.action.MAIN -n " + PlayerSettings.bundleIdentifier + "/" + __launch_activity;

		ProcessManager.setProcess( m_adb_path, cmd_option, (o, e)=>{
			Process sender = (Process)o;

			UnityEngine.Debug.Log( sender.StandardOutput.ReadToEnd() );

			ProcessManager.unsetProcess(sender);
		});

	}


	void getDeviceName( string deviceId, System.Action<string> callback )
	{

		var _callback = callback;

		string cmd_option = "-s " + deviceId + " shell getprop ro.product.model";

		ProcessManager.setProcess( m_adb_path, cmd_option, (o,e)=>{

			Process sender = (Process)o;

			_callback( sender.StandardOutput.ReadToEnd().Trim() );

			ProcessManager.unsetProcess(sender);
		});
	}
}
