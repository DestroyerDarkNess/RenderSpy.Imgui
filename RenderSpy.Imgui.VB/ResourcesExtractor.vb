



Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports RenderSpy.Globals

Public Class ResourcesExtractor

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Shared Function FreeLibrary(ByVal hModule As IntPtr) As Boolean
    End Function

    <DllImport("kernel32")>
    Public Shared Function LoadLibrary(ByVal lpFileName As String) As IntPtr
    End Function

    Public Shared UsesModernHook As Boolean = False

    Public Shared Function LoadAllRuntimes() As Boolean
        Try

            Dim handle As IntPtr = WinApi.GetModuleHandle("dinput8.dll")

            If Not handle = IntPtr.Zero Then
                FreeLibrary(handle)

                If System.IO.File.Exists("dinput8.dll") = False Then

                    Try
                        System.IO.File.WriteAllBytes("dinput8.dll", My.Resources.dinput8)
                        MessageBox.Show("dinput8 Pathched!! please restart the game.")
                        System.Diagnostics.Process.GetCurrentProcess().Kill()
                    Catch : End Try

                Else
                    LoadLibrary("dinput8.dll")
                    UsesModernHook = True
                End If
            End If


            Dim CimguiPath As String = Path.Combine(Path.GetTempPath(), "cimgui.dll")

            Try
                If File.Exists(CimguiPath) Then File.Delete(CimguiPath)
            Catch ex As Exception : End Try


            System.IO.File.WriteAllBytes(CimguiPath, My.Resources.cimgui)

            LoadLibrary(CimguiPath)

            Return True
        Catch ex As Exception
                Return False
            End Try
    End Function

End Class
