using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ImGuiNET;
using RenderSpy.Globals;
using RenderSpy.Inputs;
using RenderSpy.Interfaces;

namespace RenderSpy.Imgui
{
 

    [AttributeUsage(AttributeTargets.Method)]
    public class InjectionEntryPoint : Attribute
    {
        public bool CreateThread { get; set; } = true;
        public string BuildTarget { get; set; } = ".dll";
        public bool MergeLibs { get; set; } = false;
    }

    public class dllmain
    {
        private static bool Logger = false;
        public static IntPtr GameHandle = IntPtr.Zero;
        public static int KeyMenu = (int)Keys.Insert; // Or 45 =  VK_INSERT
        public static int WheelDelta = SystemInformation.MouseWheelScrollDelta;


        [InjectionEntryPoint(CreateThread = true, MergeLibs = true)]
        public static void EntryPoint()
        {
            try
            {
                while ((GameHandle.ToInt32() == 0))
                    GameHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;


                if (Logger == true)
                    RenderSpy.Globals.WinApi.AllocConsole();

                ResourcesExtractor.LoadAllRuntimes();

                RenderSpy.Graphics.GraphicsType GraphicsT = RenderSpy.Graphics.Detector.GetCurrentGraphicsType();
               List<RenderSpy.Interfaces.IHook> CurrentHooks = new List<RenderSpy.Interfaces.IHook> ();



                LogConsole("Current Graphics: " + GraphicsT.ToString() + " LIB: " + RenderSpy.Graphics.Detector.GetLibByEnum(GraphicsT));

                switch (GraphicsT)
                {
                    case  Graphics.GraphicsType.d3d9:
                        {
                            ImguiHooker.D3dVersion = GraphicsT;

                            Graphics.d3d9.Present PresentHook_9 = new Graphics.d3d9.Present();
                            PresentHook_9.Install();
                            CurrentHooks.Add( PresentHook_9);

                            PresentHook_9.PresentEvent += (IntPtr device, IntPtr sourceRect, IntPtr destRect, IntPtr hDestWindowOverride, IntPtr dirtyRegion) =>
                            {
                              
                                if (ImguiHooker.ImguiHook_Ini(device, GameHandle) == true && ShowImGui_UI == true)
                                {
                                    ImguiHooker.ImguiHook_RenderBegin();

                                    UI();

                                    ImguiHooker.ImguiHook_RenderEnd();
                                }

                                return PresentHook_9.Present_orig(device, sourceRect, destRect, hDestWindowOverride, dirtyRegion);
                            };

                            Graphics.d3d9.Reset ResetHook_9 = new Graphics.d3d9.Reset();
                            ResetHook_9.Install();
                            CurrentHooks.Add(ResetHook_9);

                            ResetHook_9.Reset_Event += (IntPtr device, ref SharpDX.Direct3D9.PresentParameters presentParameters) =>
                            {

                                if (ImguiHooker.Imgui_Ini == true)
                                ImplDX9.InvalidateDeviceObjects();

                                int Reset = ResetHook_9.Reset_orig(device, ref presentParameters);

                                if (ImguiHooker.Imgui_Ini == true)
                                    ImplDX9.CreateDeviceObjects();

                                return Reset;
                            };

                            break;
                        }

                    default:
                        {
                            throw new Exception("The game does not render in Directx 9 mode");
                            break;
                        }
                }

                bool Runtime = true;

                LogConsole("dinput = " + ResourcesExtractor.DirectInputFuck);

                if (ResourcesExtractor.DirectInputFuck == true)
                {

                SetWindowLongPtr SetWindowLongPtr_Hook = new SetWindowLongPtr();
                SetWindowLongPtr_Hook.WindowHandle = GameHandle;
                SetWindowLongPtr_Hook.Install();
                CurrentHooks.Add(SetWindowLongPtr_Hook);
                SetWindowLongPtr_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                {
                    try
                    {
                        if ((WM)msg == WM.KEYDOWN && wParam.ToInt32() == (int)Keys.F2)
                            Runtime = !Runtime;

                        if (ImguiHooker.Imgui_Ini == true)
                        {
                            if ((WM)msg == WM.KEYDOWN && wParam.ToInt32() == KeyMenu)
                                ShowImGui_UI = !ShowImGui_UI;

                            ImGuiIOPtr IO = ImGuiNET.ImGui.GetIO();
                            IO.MouseDrawCursor = ShowImGui_UI;

                            if (ShowImGui_UI == true)
                                ImplWin32.WndProcHandler(hWnd, msg, wParam.ToInt32(), (uint)lParam);
                        }
                    }
                    catch (Exception ex)
                    {
                        //LogConsole(ex.Message); //Fix error : The arithmetic operation has caused an overflow.
                    }
                    return IntPtr.Zero;
                };

                DirectInputHook DirectInputHook_Hook = new DirectInputHook();
                DirectInputHook_Hook.WindowHandle = GameHandle;
                DirectInputHook_Hook.Install();
                CurrentHooks.Add(DirectInputHook_Hook);
                DirectInputHook_Hook.GetDeviceState += (IntPtr hDevice, int cbData, IntPtr lpvData) =>
                {

                    if (ImguiHooker.Imgui_Ini == true && ShowImGui_UI)
                    {
                        try
                        {

                            int Result = DirectInputHook_Hook.Hook_orig(hDevice, cbData, lpvData);

                            if (Result == 0)
                            {
                                ImGuiIOPtr IO = ImGuiNET.ImGui.GetIO();

                                if (cbData == 16 || cbData == 20)
                                {
                                    DirectInputHook.LPDIMOUSESTATE MouseData = DirectInputHook_Hook.GetMouseData(lpvData);
                                    IO.MouseDown[0] = (MouseData.rgbButtons[0] != 0);
                                    IO.MouseDown[1] = (MouseData.rgbButtons[1] != 0);

                                    IO.MouseWheel += (float)(MouseData.lZ / (float)WheelDelta);

                                }

                            }
                            return Result;
                        }
                        catch (Exception ex)
                        {
                            //LogConsole(ex.Message); //Fix error : The arithmetic operation has caused an overflow.
                        }

                    }

                    return DirectInputHook_Hook.Hook_orig(hDevice, cbData, lpvData);
                };

                }
                else
                {
                    DefWindowProc DefWindowProcW_Hook = new DefWindowProc();
                    DefWindowProcW_Hook.WindowHandle = GameHandle;
                    DefWindowProcW_Hook.Install();
                    CurrentHooks.Add(DefWindowProcW_Hook);
                    DefWindowProcW_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                    {

                        try
                        {
                            if ((WM)msg == WM.KEYDOWN && wParam.ToInt32() == (int)Keys.F2)
                                Runtime = !Runtime;

                            if (ImguiHooker.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && wParam.ToInt32() == KeyMenu)
                                    ShowImGui_UI = !ShowImGui_UI;

                                ImGuiIOPtr IO = ImGuiNET.ImGui.GetIO();
                                IO.MouseDrawCursor = ShowImGui_UI;

                                if (ShowImGui_UI == true)
                                    ImplWin32.WndProcHandler(hWnd, msg, (long)wParam, (uint)lParam);
                            }
                        }
                        catch (Exception ex)
                        {
                            //LogConsole(ex.Message); //Fix error : The arithmetic operation has caused an overflow.
                        }
                        return IntPtr.Zero;
                    };
                }


                SetCursorPos NewHookCursor = new SetCursorPos();
                NewHookCursor.Install();
                CurrentHooks.Add(NewHookCursor);
                NewHookCursor.SetCursorPos_Event += (int x, int y) =>
                {
                    NewHookCursor.BlockInput = ShowImGui_UI;
                    return false;
                };

              

                while (Runtime)
                {

                    // ----->>>>> External code without WNDPROC Hooks // DirectInputHook <<<<<--------

                    //Thread.Sleep(100);

                    //int EndkeyState = WinApi.GetAsyncKeyState(Keys.F2); // Panic Key , Terminate cheat....

                    //if (EndkeyState != 0) { Runtime = !Runtime; }

                    //int MenuKeyState = WinApi.GetAsyncKeyState((Keys)KeyMenu); // Show Menu...

                    //if (MenuKeyState != 0) { ShowImGui_UI = !ShowImGui_UI; }

                    //// Alternative , use dinput8.dll to rawinput (required Instalation) : https://github.com/geeky/dinput8wrapper
                    //// Review the commented code in the class "ResourcesExtractor" To implement.

                    //// Simple Mouse Hook XD
                    //if (ImguiHooker.Imgui_Ini == true && ShowImGui_UI)
                    //{

                    //    ImGuiIOPtr IO = ImGuiNET.ImGui.GetIO();
                    //    IO.MouseDrawCursor = ShowImGui_UI;

                    //    int LButton = WinApi.GetAsyncKeyState(Keys.LButton); IO.MouseDown[0] = (LButton != 0);

                    //    int RButton = WinApi.GetAsyncKeyState(Keys.RButton); IO.MouseDown[1] = (RButton != 0);

                    //    int MButton = WinApi.GetAsyncKeyState(Keys.MButton); IO.MouseDown[2] = (MButton != 0);

                    //    int XButton1 = WinApi.GetAsyncKeyState(Keys.XButton1); IO.MouseDown[3] = (XButton1 != 0);

                    //    int XButton2 = WinApi.GetAsyncKeyState(Keys.XButton2); IO.MouseDown[4] = (XButton2 != 0);

                    //}
                }

                foreach (IHook Hook in CurrentHooks)
                {
                    Hook.Uninstall();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace, "Imgui Hook Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LogConsole(string msg)
        {
            if (Logger == true)
                Console.WriteLine(msg);
        }



        private static ImguiHook ImguiHooker = new ImguiHook();
        private static bool ShowImGui_UI = false;
        private static bool ShowImguiDemo = false;

        public static bool UI()
        {
            ImGuiNET.ImGui.Begin("Another Window in C#", ref ShowImGui_UI);
            ImGuiNET.ImGui.Text("Hello from another window!");

            if (ImGuiNET.ImGui.Button("Show ImguiDemo"))
                ShowImguiDemo = !ShowImguiDemo;
            if (ImGuiNET.ImGui.Button("Close Me"))
                ShowImGui_UI = false;

            if (ShowImguiDemo)
                ImGuiNET.ImGui.ShowDemoWindow();

            ImGuiNET.ImGui.End();

            return true;
        }
    }

}
