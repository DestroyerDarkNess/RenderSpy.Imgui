Imports ImGuiNET
Imports System.Runtime.InteropServices

Public Class ImguiHook


    <DllImport("cimgui")>
    Public Shared Function igCreateContext(ByVal fontAtlas As IntPtr) As IntPtr
    End Function

    Public Property Imgui_Ini As Boolean = False
    Public Property D3dVersion As Graphics.GraphicsType = Graphics.GraphicsType.unknown

    Public Function ImguiHook_Ini(ByVal Adress As IntPtr, ByVal GameHandle As IntPtr) As Boolean
        Try
            If D3dVersion = Graphics.GraphicsType.d3d9 Then
                If Imgui_Ini = True Then
                    Return True
                Else
                    Imgui_Ini = True
                    Dim context = igCreateContext(IntPtr.Zero)
                    ImplWin32.Init(GameHandle)
                    ImplDX9.Init(Adress)
                End If
            End If
        Catch e As Exception
            Imgui_Ini = False
        End Try

        Return Imgui_Ini
    End Function

    Public Function ImguiHook_RenderBegin() As Boolean
        Dim Result As Boolean = True

        Try
            If D3dVersion = Graphics.GraphicsType.d3d9 Then

                ImplDX9.NewFrame()
                ImplWin32.NewFrame()

                ImGuiNET.ImGui.NewFrame()

            End If

        Catch ex As Exception
            Result = False
        End Try

        Return Result
    End Function

    Public Function ImguiHook_RenderEnd() As Boolean
        Dim Result As Boolean = True

        Try
            If D3dVersion = Graphics.GraphicsType.d3d9 Then

                ImGuiNET.ImGui.EndFrame()
                ImGuiNET.ImGui.Render()

                ImplDX9.RenderDrawData(ImGuiNET.ImGui.GetDrawData())

            End If

        Catch ex As Exception
            Result = False
        End Try

        Return Result
    End Function

End Class
