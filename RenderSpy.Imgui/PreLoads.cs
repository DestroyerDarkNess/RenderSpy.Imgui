using RenderSpy.Globals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static RenderSpy.Imgui.dllmain;

namespace RenderSpy.Imgui
{
    public class PreLoads
    {
        [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")] public static extern IntPtr LoadLibrary(String lpFileName);

        public static InputType HookInputType = InputType.UniversalUnsafe;
        public static bool LoadAllRuntimes() {

            try {
                // Extract important and native resources.
                //System.IO.File.WriteAllBytes("cimgui.dll", Properties.Resources.cimgui);
               
                //Fix DirectInput8
                IntPtr handle = WinApi.GetModuleHandle("dinput8.dll");
                if (handle != IntPtr.Zero)
                {
                    FreeLibrary(handle);
                    if (System.IO.File.Exists("dinput8.dll") == false)
                    {
                        try
                        {
                            // By : https://github.com/geeky/dinput8wrapper

                            System.IO.File.WriteAllBytes("dinput8.dll", Properties.Resources.dinput8); // <<<----  On windows it sometimes fails with UnauthorizedAccessException, such as when the game is on a drive other than the OS drive.

                            System.Diagnostics.Process.GetCurrentProcess().Kill();


                        }
                        catch  { }

                    }
                    else
                    {
                        LoadLibrary("dinput8.dll");
                        HookInputType = InputType.ModernWndProc;
                    }

                }


                return true;
            } catch (Exception ex) {  return false; }

        }

    }
}
