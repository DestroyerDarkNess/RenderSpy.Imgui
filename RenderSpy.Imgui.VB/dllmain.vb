Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms
Imports ImGuiFuncs
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
    Public Shared KeyMenu As Integer = Keys.Insert 'Or 45 =  VK_INSERT
    Public Shared WheelDelta As Integer = SystemInformation.MouseWheelScrollDelta

#End Region

#Region " Dll EntryPoint "

    <InjectionEntryPoint(CreateThread:=True, MergeLibs:=True)>
    Public Shared Sub EntryPoint()

        Try

            While (GameHandle.ToInt32() = 0) : GameHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle : End While


            If Logger = True Then RenderSpy.Globals.WinApi.AllocConsole()

            ResourcesExtractor.LoadAllRuntimes()

            Dim GraphicsT As RenderSpy.Graphics.GraphicsType = RenderSpy.Graphics.Detector.GetCurrentGraphicsType()
            Dim CurrentHooks As List(Of RenderSpy.Interfaces.IHook) = New List(Of RenderSpy.Interfaces.IHook)

            LogConsole("Current Graphics: " & GraphicsT.ToString() & " LIB: " + RenderSpy.Graphics.Detector.GetLibByEnum(GraphicsT))

            Select Case GraphicsT
                Case Graphics.GraphicsType.d3d9
                    ImguiHooker.D3dVersion = GraphicsT

                    Dim PresentHook_9 As Graphics.d3d9.Present = New Graphics.d3d9.Present()
                    PresentHook_9.Install()
                    CurrentHooks.Add(PresentHook_9)

                    AddHandler PresentHook_9.PresentEvent, Function(ByVal device As IntPtr, ByVal sourceRect As IntPtr, ByVal destRect As IntPtr, ByVal hDestWindowOverride As IntPtr, ByVal dirtyRegion As IntPtr)

                                                               If ImguiHooker.ImguiHook_Ini(device, GameHandle) = True AndAlso ShowImGui_UI = True Then
                                                                   ImguiHooker.ImguiHook_RenderBegin()

                                                                   UI()

                                                                   ImguiHooker.ImguiHook_RenderEnd()
                                                               End If

                                                               Return PresentHook_9.Present_orig(device, sourceRect, destRect, hDestWindowOverride, dirtyRegion)
                                                           End Function

                    Dim ResetHook_9 As Graphics.d3d9.Reset = New Graphics.d3d9.Reset()
                    ResetHook_9.Install()
                    CurrentHooks.Add(ResetHook_9)
                    AddHandler ResetHook_9.Reset_Event, Function(ByVal device As IntPtr, ByRef presentParameters As SharpDX.Direct3D9.PresentParameters)
                                                            If ImguiHooker.Imgui_Ini = True Then ImplDX9.InvalidateDeviceObjects()
                                                            Dim Reset As Integer = ResetHook_9.Reset_orig(device, presentParameters)
                                                            If ImguiHooker.Imgui_Ini = True Then ImplDX9.CreateDeviceObjects()
                                                            Return Reset
                                                        End Function

                Case Else
                    Throw New Exception("The game does not render in Directx 9 mode")
            End Select

            Dim Runtime As Boolean = True

            If ResourcesExtractor.DirectInputFuck = True Then

                Dim SetWindowLongPtr_Hook As SetWindowLongPtr = New SetWindowLongPtr()
                SetWindowLongPtr_Hook.WindowHandle = GameHandle
                SetWindowLongPtr_Hook.Install()
                CurrentHooks.Add(SetWindowLongPtr_Hook)
                AddHandler SetWindowLongPtr_Hook.WindowProc, Function(ByVal hWnd As IntPtr, ByVal msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr)
                                                                 Try

                                                                     If msg = WM.KEYDOWN AndAlso wParam.ToInt32 = Keys.F2 Then
                                                                         Runtime = Not Runtime
                                                                     End If

                                                                     If ImguiHooker.Imgui_Ini = True Then

                                                                         If msg = WM.KEYDOWN AndAlso wParam.ToInt32 = KeyMenu Then
                                                                             ShowImGui_UI = Not ShowImGui_UI
                                                                         End If

                                                                         Dim IO As ImGuiIOPtr = ImGuiNET.ImGui.GetIO()
                                                                         IO.MouseDrawCursor = ShowImGui_UI

                                                                         If ShowImGui_UI = True Then
                                                                             ImplWin32.WndProcHandler(hWnd, msg, wParam.ToInt32, lParam.ToInt32)
                                                                         End If

                                                                     End If
                                                                 Catch ex As Exception
                                                                     '   LogConsole(ex.Message) Fix error : The arithmetic operation has caused an overflow.
                                                                 End Try
                                                                 Return IntPtr.Zero
                                                             End Function


                Dim DirectInputHook_Hook As DirectInputHook = New DirectInputHook()
                DirectInputHook_Hook.WindowHandle = GameHandle
                DirectInputHook_Hook.Install()
                CurrentHooks.Add(DirectInputHook_Hook)
                AddHandler DirectInputHook_Hook.GetDeviceState, Function(ByVal hDevice As IntPtr, ByVal cbData As Integer, ByVal lpvData As IntPtr)

                                                                    If ImguiHooker.Imgui_Ini = True AndAlso ShowImGui_UI Then

                                                                        Try
                                                                            Dim Result As Integer = DirectInputHook_Hook.Hook_orig(hDevice, cbData, lpvData)

                                                                            If Result = 0 Then
                                                                                Dim IO As ImGuiIOPtr = ImGuiNET.ImGui.GetIO()

                                                                                If cbData = 16 OrElse cbData = 20 Then
                                                                                    Dim MouseData As DirectInputHook.LPDIMOUSESTATE = DirectInputHook_Hook.GetMouseData(lpvData)
                                                                                    VisaulBasicLimitations.MouseDown(IO, 0, (MouseData.rgbButtons(0) <> 0))
                                                                                    VisaulBasicLimitations.MouseDown(IO, 1, (MouseData.rgbButtons(1) <> 0))
                                                                                    IO.MouseWheel += CSng((MouseData.lZ / CSng(WheelDelta)))
                                                                                End If
                                                                            End If

                                                                            Return Result
                                                                        Catch ex As Exception : End Try

                                                                    End If

                                                                    Return DirectInputHook_Hook.Hook_orig(hDevice, cbData, lpvData)
                                                                End Function


            Else

                Dim DefWindowProcW_Hook As DefWindowProc = New DefWindowProc()
                DefWindowProcW_Hook.WindowHandle = GameHandle
                DefWindowProcW_Hook.Install()
                CurrentHooks.Add(DefWindowProcW_Hook)
                AddHandler DefWindowProcW_Hook.WindowProc, Function(ByVal hWnd As IntPtr, ByVal msg As UInteger, ByVal wParam As IntPtr, ByVal lParam As IntPtr)
                                                               Try

                                                                   If msg = WM.KEYDOWN AndAlso wParam.ToInt32 = Keys.F2 Then
                                                                       Runtime = Not Runtime
                                                                   End If

                                                                   If ImguiHooker.Imgui_Ini = True Then

                                                                       If msg = WM.KEYDOWN AndAlso wParam.ToInt32 = KeyMenu Then
                                                                           ShowImGui_UI = Not ShowImGui_UI
                                                                       End If

                                                                       Dim IO As ImGuiIOPtr = ImGuiNET.ImGui.GetIO()
                                                                       IO.MouseDrawCursor = ShowImGui_UI

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
            CurrentHooks.Add(NewHookCursor)
            AddHandler NewHookCursor.SetCursorPos_Event, Function(ByVal x As Integer, ByVal y As Integer)
                                                             NewHookCursor.BlockInput = ShowImGui_UI
                                                             Return False
                                                         End Function


            While Runtime

                '' ----->>>>> External code without WNDPROC Hooks // DirectInputHook <<<<<--------

                'Thread.Sleep(100)

                'Dim EndkeyState As Integer = WinApi.GetAsyncKeyState(Keys.F2) ' Panic Key , Terminate cheat....

                'If Not EndkeyState = 0 Then Runtime = Not Runtime

                'Dim MenuKeyState As Integer = WinApi.GetAsyncKeyState(KeyMenu) ' Show Menu....

                'If Not MenuKeyState = 0 Then ShowImGui_UI = Not ShowImGui_UI

                '' Alternative , use dinput8.dll to rawinput (required Instalation)  https : //github.com/geeky/dinput8wrapper
                '' Review the commented code in the class "ResourcesExtractor" To implement.

                '' Simple Mouse Hook XD

                'If ImguiHooker.Imgui_Ini = True AndAlso ShowImGui_UI Then

                '    Dim IO As ImGuiIOPtr = ImGuiNET.ImGui.GetIO()
                '    IO.MouseDrawCursor = ShowImGui_UI

                '    Dim LButton As Integer = WinApi.GetAsyncKeyState(Keys.LButton) : VisaulBasicLimitations.MouseDown(IO, 0, (LButton <> 0))

                '    Dim RButton As Integer = WinApi.GetAsyncKeyState(Keys.RButton) : VisaulBasicLimitations.MouseDown(IO, 1, (RButton <> 0))

                '    Dim MButton As Integer = WinApi.GetAsyncKeyState(Keys.MButton) : VisaulBasicLimitations.MouseDown(IO, 2, (MButton <> 0))

                '    Dim XButton1 As Integer = WinApi.GetAsyncKeyState(Keys.XButton1) : VisaulBasicLimitations.MouseDown(IO, 3, (XButton1 <> 0))

                '    Dim XButton2 As Integer = WinApi.GetAsyncKeyState(Keys.XButton2) : VisaulBasicLimitations.MouseDown(IO, 4, (XButton2 <> 0))

                'End If

            End While

            For Each Hook As IHook In CurrentHooks
                Hook.Uninstall()
            Next


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