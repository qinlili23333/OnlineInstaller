﻿
Imports System.IO.Compression
Imports System.Reflection
Imports System.Text.Json
Imports System.Drawing
Imports System.Net
Imports System.IO
Imports System.Windows.Resources

Class MainWindow
    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        DoInstallAsync()
    End Sub
    Class InstallJSONFormat
        Public Property Name As String
        Public Property InstallLocation As String
        Public Property RunExe As String
        Public Property PerformOnlineInstall As Boolean
        Public Property OnlinePackage As String
        Public Property HybridInstall As Boolean
    End Class
    Private Async Sub DoInstallAsync()
        Dim asm As Assembly = Assembly.GetExecutingAssembly()
        'MsgBox(String.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames().ToArray()))
        Dim InstallJSON
        Try
            Dim ConfigFile = asm.GetManifestResourceStream("OnlineInstaller.InstallConfig")
            InstallJSON = Await JsonSerializer.DeserializeAsync(Of InstallJSONFormat)(ConfigFile)
        Catch
            MsgBox("读取安装信息失败！可能是安装包已损坏",, "安装失败")
            End
        End Try
        InstallName.Content = InstallJSON.Name
        Dim archive As ZipArchive
        Dim archive2 As ZipArchive
        If InstallJSON.PerformOnlineInstall = True Then
            InstallStatus.Content = "正在下载..."
            Dim dlt As Task = Task.Run(Sub()
                                           Dim fileReq As HttpWebRequest = HttpWebRequest.Create(InstallJSON.OnlinePackage)
                                           Dim fileResp As HttpWebResponse = fileReq.GetResponse()
                                           archive = New ZipArchive(fileResp.GetResponseStream())
                                       End Sub)
            Await dlt.WaitAsync(New TimeSpan(10, 0, 0, 0))
            If InstallJSON.HybridInstall = True Then
                archive2 = New ZipArchive(asm.GetManifestResourceStream("OnlineInstaller.InstallFile"), ZipArchiveMode.Read)
            End If
        Else
            archive = New ZipArchive(asm.GetManifestResourceStream("OnlineInstaller.InstallFile"), ZipArchiveMode.Read)
        End If
        InstallStatus.Content = "正在安装..."
        Dim ExtractPath As String
        If InstallJSON.InstallLocation = "Here" Then
            ExtractPath = System.Environment.CurrentDirectory
        Else
            ExtractPath = InstallJSON.InstallLocation
        End If
        Dim t As Task = Task.Run(Sub()
                                     archive.ExtractToDirectory(ExtractPath, True)
                                     If InstallJSON.HybridInstall = True Then
                                         archive2.ExtractToDirectory(ExtractPath, True)
                                     End If
                                 End Sub)
        t.GetAwaiter().OnCompleted(Sub()
                                       If InstallJSON.RunExe = "False" Then
                                           InstallStatus.Content = "安装成功!"
                                           Task.Run(Async Sub()
                                                        Await Task.Delay(1000)
                                                        End
                                                    End Sub)
                                       Else
                                           If File.Exists(InstallJSON.RunExe) Then
                                               InstallStatus.Content = "安装成功!即将启动..."
                                               Task.Run(Async Sub()
                                                            Await Task.Delay(1000)
                                                            Process.Start(InstallJSON.RunExe)
                                                            Await Task.Delay(1000)
                                                            End
                                                        End Sub)
                                           Else
                                               InstallStatus.Content = "安装可能失败了" + Environment.NewLine + "可能存在网络不稳定或流量劫持" + Environment.NewLine + "或安装器版本和服务器版本不符"
                                               PBar.Foreground = New SolidColorBrush(
Media.ColorConverter.ConvertFromString("#FFFF0000"))
                                               PBar.IsIndeterminate = False
                                               PBar.Value = 100
                                               Task.Run(Async Sub()
                                                            Await Task.Delay(3000)
                                                            End
                                                        End Sub)
                                           End If
                                       End If
                                   End Sub)
    End Sub
End Class