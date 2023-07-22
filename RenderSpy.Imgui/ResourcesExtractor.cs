using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RenderSpy.Globals;

namespace RenderSpy.Imgui
{
   
    public class ResourcesExtractor
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string lpFileName);

        public static bool UsesModernHook = false;

        public static bool LoadAllRuntimes()
        {
            try
            {
                IntPtr handle = WinApi.GetModuleHandle("dinput8.dll");

                if (handle != IntPtr.Zero)
                {
                FreeLibrary(handle);

                    if (System.IO.File.Exists("dinput8.dll") == false)
                    {
                        try
                        {
                            System.IO.File.WriteAllBytes("dinput8.dll", Properties. Resources.dinput8);
                            MessageBox.Show("dinput8 Pathched!! please restart the game.");
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        LoadLibrary("dinput8.dll");
                        UsesModernHook = true;
                    }
                }

                string CimguiPath = Path.Combine(Path.GetTempPath(), "cimgui.dll");

                try {
                    if (File.Exists(CimguiPath))
                        File.Delete(CimguiPath);
                } finally { }
               

                System.IO.File.WriteAllBytes(CimguiPath, Properties.Resources.cimgui);

                LoadLibrary(CimguiPath);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

}
