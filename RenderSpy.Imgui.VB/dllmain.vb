Imports System.Threading
Imports System.Windows.Forms
Imports ImGuiNET
Imports RenderSpy.Globals
Imports RenderSpy.Inputs
Imports RenderSpy.Interfaces

<AttributeUsage(AttributeTargets.Method)>
Public Class InjectionEntryPoint
    Inherits Attribute

    Public Property CreateThread As Boolean = True
    Public Property BuildTarget As String = ".dll"
    Public Property MergeLibs As Boolean = False
End Class

Public Class dllmain

#Region " Declare "

    Private Shared Logger As Boolean = False
    Public Shared GameHandle As IntPtr = IntPtr.Zero
    Public Shared KeyMenu As Integer = &H2D 'Or 45 =  VK_INSERT

#End Region

#Region " Dll EntryPoint "

    <InjectionEntryPoint(CreateThread:=True, MergeLibs:=True)>
    Public Shared Sub EntryPoint()

        Try

            While (GameHandle.ToInt32() = 0) : GameHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle : End While


            If Logger = True Then RenderSpy.Globals.WinApi.AllocConsole()

            ResourcesExtractor.LoadAllRuntimes()

            Dim GraphicsT As RenderSpy.Graphics.GraphicsType = RenderSpy.Graphics.Detector.GetCurrentGraphicsType()
            Dim CurrentHook As RenderSpy.Interfaces.IHook = Nothing

            LogConsole("Current Graphics: " & GraphicsT.ToString() & " LIB: " + RenderSpy.Graphics.Detector.GetLibByEnum(GraphicsT))

            Select Case GraphicsT
                Case Graphics.GraphicsType.d3d9
                    ImguiHooker.D3dVersion = GraphicsT

                    Dim PresentHook_9 As Graphics.d3d9.Present = New Graphics.d3d9.Present()
                    PresentHook_9.Install()
                    CurrentHook = PresentHook_9

                    AddHandler PresentHook_9.PresentEvent, Function(ByVal device As IntPtr, ByVal sourceRect As IntPtr, ByVal destRect As IntPtr, ByVal hDestWindowOverride As IntPtr, ByVal dirtyRegion As IntPtr)

                                                               If ImguiHooker.ImguiHook_Ini(device, GameHandle) = True AndAlso ShowImGui_UI = True Then
                                                                   ImguiHooker.ImguiHook_RenderBegin(ShowImGui_UI)

                                                                   UI()

                                                                   ImguiHooker.ImguiHook_RenderEnd()
                                                               End If

                                                               Return PresentHook_9.Present_orig(device, sourceRect, destRect, hDestWindowOverride, dirtyRegion)
                                                           End Function

                Case Else
                    Throw New Exception("The game does not render in Directx 9 mode")
            End Select

            Dim InputHook As IHook = Nothing

            If ResourcesExtractor.UsesModernHook = True Then

                Dim GetWindowLongPtr_Hook As GetWindowLongPtr = New GetWindowLongPtr()
                GetWindowLongPtr_Hook.WindowHandle = GameHandle
                GetWindowLongPtr_Hook.Install()
                InputHook = GetWindowLongPtr_Hook
                AddHandler GetWindowLongPtr_Hook.WindowProc, Function(ByVal hWnd As IntPtr, ByVal msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr)
                                                                 Try
                                                                     If ImguiHooker.Imgui_Ini = True Then

                                                                         If msg = WM.KEYDOWN AndAlso wParam.ToInt32 = KeyMenu Then
                                                                             ShowImGui_UI = Not ShowImGui_UI
                                                                         End If

                                                                         If ShowImGui_UI = True Then
                                                                             ImplWin32.WndProcHandler(hWnd, msg, wParam.ToInt32, lParam.ToInt32)
                                                                         End If
                                                                     End If
                                                                 Catch ex As Exception
                                                                     '   LogConsole(ex.Message) Fix error : The arithmetic operation has caused an overflow.
                                                                 End Try
                                                                 Return IntPtr.Zero
                                                             End Function

            Else

                Dim DefWindowProcW_Hook As DefWindowProc = New DefWindowProc()
                DefWindowProcW_Hook.WindowHandle = GameHandle
                DefWindowProcW_Hook.Install()
                InputHook = DefWindowProcW_Hook
                AddHandler DefWindowProcW_Hook.WindowProc, Function(ByVal hWnd As IntPtr, ByVal msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr)
                                                               Try
                                                                   If ImguiHooker.Imgui_Ini = True Then

                                                                       If msg = WM.KEYDOWN AndAlso wParam.ToInt32 = KeyMenu Then
                                                                           ShowImGui_UI = Not ShowImGui_UI
                                                                       End If

                                                                       If ShowImGui_UI = True Then
                                                                           ImplWin32.WndProcHandler(hWnd, msg, wParam, lParam)
                                                                       End If

                                                                   End If
                                                               Catch ex As Exception
                                                                   '   LogConsole(ex.Message) Fix error : The arithmetic operation has caused an overflow.
                                                               End Try
                                                               Return IntPtr.Zero
                                                           End Function

            End If

            Dim NewHookCursor As SetCursorPos = New SetCursorPos()
            NewHookCursor.Install()
            AddHandler NewHookCursor.SetCursorPos_Event, Function(ByVal x As Integer, ByVal y As Integer)
                                                             NewHookCursor.BlockInput = ShowImGui_UI
                                                             Return False
                                                         End Function

            Dim Runtime As Boolean = True

            While Runtime
                Thread.Sleep(10)

                Dim EndkeyState As Integer = WinApi.GetAsyncKeyState(Keys.F2) ' Panic Key , Terminate cheat....

                If EndkeyState = 1 OrElse EndkeyState = -32767 Then
                    Runtime = Not Runtime
                End If

            End While

            CurrentHook.Uninstall()
            InputHook.Uninstall()
            NewHookCursor.Uninstall()

        Catch ex As Exception

            MessageBox.Show(ex.Message, "Imgui Hook Error!", MessageBoxButtons.OK, MessageBoxIcon.Error)

        End Try


    End Sub

    Private Shared Sub LogConsole(ByVal msg As String)
        If Logger = True Then Console.WriteLine(msg)
    End Sub

#End Region



#Region " Imgui Menu "

    Private Shared ImguiHooker As ImguiHook = New ImguiHook
    Private Shared ShowImGui_UI As Boolean = False
    Private Shared ShowImguiDemo As Boolean = False

    Public Shared Function UI() As Boolean

        ImGuiNET.ImGui.Begin("Another Window in VB", ShowImGui_UI)
        ImGuiNET.ImGui.Text("Hello from another window!")

        If ImGuiNET.ImGui.Button("Show ImguiDemo") Then ShowImguiDemo = Not ShowImguiDemo
        If ImGuiNET.ImGui.Button("Close Me") Then ShowImGui_UI = False

        If ShowImguiDemo Then ImGuiNET.ImGui.ShowDemoWindow()

        ImGuiNET.ImGui.[End]()

        Return True
    End Function

#End Region




End Class