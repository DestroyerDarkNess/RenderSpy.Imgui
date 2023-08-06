Imports System
Imports System.Diagnostics
Imports System.Runtime.InteropServices

Public Class ModuleHider
    <DllImport("kernel32.dll")>
    Public Shared Function OpenProcess(ByVal dwDesiredAccess As Integer, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Integer) As IntPtr

    End Function
    <DllImport("kernel32.dll", CharSet:=CharSet.Auto)>
    Public Shared Function GetModuleHandle(ByVal lpModuleName As String) As IntPtr

    End Function
    <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
    Public Shared Function GetProcAddress(ByVal hModule As IntPtr, ByVal lpProcName As String) As IntPtr

    End Function
    <DllImport("kernel32.dll")>
    Public Shared Function VirtualAllocEx(ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As UInteger, ByVal flAllocationType As UInteger, ByVal flProtect As UInteger) As IntPtr

    End Function
    <DllImport("kernel32.dll")>
    Public Shared Function WriteProcessMemory(ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer As Byte(), ByVal nSize As UInteger, <Out> ByRef lpNumberOfBytesWritten As Integer) As Boolean

    End Function
    <DllImport("kernel32.dll")>
    Public Shared Function CreateRemoteThread(ByVal hProcess As IntPtr, ByVal lpThreadAttributes As IntPtr, ByVal dwStackSize As UInteger, ByVal lpStartAddress As IntPtr, ByVal lpParameter As IntPtr, ByVal dwCreationFlags As UInteger, ByVal lpThreadId As IntPtr) As IntPtr

    End Function
    <DllImport("kernel32.dll")>
    Public Shared Function CloseHandle(ByVal hObject As IntPtr) As Boolean

    End Function

    <DllImport("kernel32", SetLastError:=True)>
    Public Shared Function WaitForSingleObject(
    ByVal handle As IntPtr,
    ByVal milliseconds As UInt32) As UInt32
    End Function

    Public Shared Sub HideModule(ByVal targetProcess As Process, ByVal moduleName As String)

        Dim processHandle As IntPtr = OpenProcess(&H1F0FFF, False, targetProcess.Id)
        Dim moduleHandle As IntPtr = GetModuleHandle(moduleName)
        Dim freeLibraryAddr As IntPtr = GetProcAddress(GetModuleHandle("kernel32.dll"), "FreeLibrary")
        Dim remoteMemory As IntPtr = VirtualAllocEx(processHandle, IntPtr.Zero, 4096, &H1000, &H40)
        Dim code As Byte() = BitConverter.GetBytes(CULng(freeLibraryAddr))
        Dim bytesWritten As Integer
        WriteProcessMemory(processHandle, remoteMemory, code, CUInt(code.Length), bytesWritten)
        Dim threadHandle As IntPtr = CreateRemoteThread(processHandle, IntPtr.Zero, 0, remoteMemory, moduleHandle, 0, IntPtr.Zero)

        If threadHandle <> IntPtr.Zero Then
            WaitForSingleObject(threadHandle, 5000)
            CloseHandle(threadHandle)
        End If

        CloseHandle(processHandle)

    End Sub
End Class
