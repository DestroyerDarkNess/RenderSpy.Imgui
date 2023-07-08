using ImGuiNET;
using RenderSpy.Inputs;
using RenderSpy.Globals;
using SharpDX.DXGI;
using System;
using System.IO;
using Khronos;
using SharpDX.Direct3D9;
using System.Windows.Forms;

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

        public enum InputType
        {
            OldWndProc = 0,
            RawInput = 1,
            UniversalUnsafe = 2,
            ModernWndProc = 3,
            AlternativeWndProc = 4
        }

        public enum D3d9HookType
        {
            EndScene = 0,
            Present = 1
        }

        public enum DxVer
        {
            d3d9 = 0,
            d3d11 = 1
        }

        public static int KeyMenu = 0x2D; // VK_INSERT
        public static IntPtr GameHandle = IntPtr.Zero;
        public static D3d9HookType D3dHook = D3d9HookType.EndScene;
        public static ImguiHook imguiHook;

        [InjectionEntryPoint(MergeLibs = true, BuildTarget = ".dll")]
        public static void EntryPoint()
        {
           
            while (GameHandle.ToInt32() == 0)
            {
                GameHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;  // Get Main Game Window Handle
            }


            bool Runtine = PreLoads.LoadAllRuntimes();

            if (WinApi.GetModuleHandle("d3d9.dll") != IntPtr.Zero)
            {
                imguiHook = new ImguiHook(DxVer.d3d9);
            }
            else if (WinApi.GetModuleHandle("d3d11.dll") != IntPtr.Zero)
            {
                imguiHook = new ImguiHook(DxVer.d3d11);
            }


            // Identify the Game.

            string ProcName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().ProcessName);

                switch (ProcName.ToLower())
                {
                    case "gta_sa":
                    // Set Specifig Hook

                        PreLoads.HookInputType = InputType.ModernWndProc;
                        D3dHook = D3d9HookType.Present;

                        break;
                    case "haloce":

                        break;
                    default:

                        break;
                }
         
            try
            {
                if (imguiHook.D3dVersion == DxVer.d3d9)
                {
                    switch (D3dHook)
                    {
                        case D3d9HookType.EndScene:
                            // EndScene Hook

                            Graphics.d3d9.EndScene NewEndScene = new Graphics.d3d9.EndScene();

                            //Create your Custom Device
                            //SharpDX.Direct3D9.Direct3D d3d = new SharpDX.Direct3D9.Direct3D();
                            //NewEndScene.GlobalDevice = new SharpDX.Direct3D9.Device(d3d, 0, DeviceType.NullReference, GameHandle, CreateFlags.SoftwareVertexProcessing | CreateFlags.DisableDriverManagement, Graphics.d3d9.Globals.GetPresentParameters(GameHandle));

                            NewEndScene.Install();

                            NewEndScene.EndSceneEvent += (device) =>
                            {
                               
                                if (imguiHook.ImguiHook_Ini(device, GameHandle) == true)
                                {
                                    bool Render = imguiHook.ImguiHook_Render(device, ShowImGui_UI);
                                    
                                }
                                return NewEndScene.EndScene_orig(device);
                            };


                            break;
                        case D3d9HookType.Present:
                            // Present Hook

                            Graphics.d3d9.Present NewPresent = new Graphics.d3d9.Present();
                            NewPresent.Install();

                            NewPresent.PresentEvent += (IntPtr device, IntPtr sourceRect, IntPtr destRect, IntPtr hDestWindowOverride, IntPtr dirtyRegion) =>
                            {
                                if (imguiHook.ImguiHook_Ini(device, GameHandle) == true)
                                {
                                    bool Render = imguiHook.ImguiHook_Render(device, ShowImGui_UI);
                                }
                                return NewPresent.Present_orig(device, sourceRect, destRect, hDestWindowOverride, dirtyRegion);
                            };

                            break;
                        default:

                            break;
                    }

                    Graphics.d3d9.Reset NewD3D9ResetHook = new Graphics.d3d9.Reset();
                    NewD3D9ResetHook.Install();

                    NewD3D9ResetHook.Reset_Event += (IntPtr device, ref SharpDX.Direct3D9.PresentParameters presentParameters) =>
                    {

                        if (imguiHook.Imgui_Ini == true)
                        {
                            ImplDX9.InvalidateDeviceObjects();
                        }

                      int result =  NewD3D9ResetHook.Reset_orig(device, ref presentParameters);
                     
                        if (imguiHook.Imgui_Ini == true)
                        {
                            ImplDX9.CreateDeviceObjects();
                        }

                        return result;
                    };


                }
                else if (imguiHook.D3dVersion == DxVer.d3d11)
                {
                    Graphics.d3d11.Present NewPresent = new Graphics.d3d11.Present();
                    NewPresent.Install();

                    NewPresent.PresentEvent += (swapChainPtr, syncInterval, flags) =>
                    {
                        if (imguiHook.ImguiHook_Ini(swapChainPtr, GameHandle) == true)
                        {
                            bool Render = imguiHook.ImguiHook_Render(swapChainPtr, ShowImGui_UI);
                        }
                        return NewPresent.Present_orig(swapChainPtr, syncInterval, flags);
                    };


                    Graphics.d3d11.ResizeTarget NewD3D11ResizeTargetHook = new Graphics.d3d11.ResizeTarget();
                    NewD3D11ResizeTargetHook.Install();

                    NewD3D11ResizeTargetHook.ResizeTarget_Event += (IntPtr swapChainPtr, ref ModeDescription newTargetParameters) =>
                    {

                        if (imguiHook.Imgui_Ini == true)
                        {
                            ImplDX9.InvalidateDeviceObjects();
                        }
                        int result = NewD3D11ResizeTargetHook.ResizeTarget_orig(swapChainPtr , ref newTargetParameters);
                        if (imguiHook.Imgui_Ini == true)
                        {
                            ImplDX9.CreateDeviceObjects();
                        }
                        return result;
                    };


                }
                else { Runtine = false; }


                // Hook Inputs
              
                switch (PreLoads.HookInputType)
                {
                    case InputType.OldWndProc:
                        // WndProc Hook
                      
                        SetWindowLongPtr WndProc_Hook = new SetWindowLongPtr();
                        WndProc_Hook.WindowHandle = GameHandle;
                        WndProc_Hook.Install();
                        WndProc_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                        {
                          
                            if (imguiHook.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && (int)wParam == dllmain.KeyMenu)
                                {

                                    dllmain.ShowImGui_UI = !dllmain.ShowImGui_UI;

                                }

                                if (dllmain.ShowImGui_UI == true)
                                {
                                    ImplWin32.WndProcHandler(hWnd, (uint)msg, (long)wParam, (uint)lParam); 
                                }
                            }
                            return IntPtr.Zero;
                        };

                        break;
                    case InputType.RawInput:
                        // RawInput Hook

                        GetRawInputData GetRawInputData_Hook = new GetRawInputData();
                        GetRawInputData_Hook.WindowHandle = GameHandle;
                        GetRawInputData_Hook.Install();
                        GetRawInputData_Hook.WindowProc += (IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader) =>
                        {

                            if (imguiHook.Imgui_Ini == true)
                            {
                                 System.Windows.Forms.Message message = GetRawInputData_Hook.GetLastMessage();
                                if ((WM)message.Msg == WM.KEYDOWN && (int)message.WParam == dllmain.KeyMenu)
                                {

                                    dllmain.ShowImGui_UI = !dllmain.ShowImGui_UI;
                                }

                                if (dllmain.ShowImGui_UI == true)
                                {
                                    ImplWin32.WndProcHandler(message.HWnd, (uint)message.Msg, (long)message.WParam, (uint)message.LParam);
                                }
                            }
                            return 0;
                        };


                        break;
                    case InputType.UniversalUnsafe:
                        // DefWindowProcW Hook

                        DefWindowProc DefWindowProcW_Hook = new DefWindowProc();
                        DefWindowProcW_Hook.WindowHandle = GameHandle;
                        DefWindowProcW_Hook.Install();
                        DefWindowProcW_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                        {
                            if (imguiHook.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && (int)wParam == dllmain.KeyMenu)
                                {

                                    dllmain.ShowImGui_UI = !dllmain.ShowImGui_UI;

                                }

                                if (dllmain.ShowImGui_UI == true)
                                {
                                    ImplWin32.WndProcHandler(hWnd, (uint)msg, (long)wParam, (uint)lParam);
                                }
                            }
                            return IntPtr.Zero;
                        };


                        break;
                    case InputType.ModernWndProc:

                        GetWindowLongPtr GetWindowLongPtr_Hook = new GetWindowLongPtr();
                        GetWindowLongPtr_Hook.WindowHandle = GameHandle;
                        GetWindowLongPtr_Hook.Install();
                        GetWindowLongPtr_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                        {
                            if (imguiHook.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && (int)wParam == dllmain.KeyMenu)
                                {

                                    dllmain.ShowImGui_UI = !dllmain.ShowImGui_UI;

                                }

                                if (dllmain.ShowImGui_UI == true)
                                {
                                    ImplWin32.WndProcHandler(hWnd, (uint)msg, (long)wParam, (uint)lParam);
                                }
                            }
                            return IntPtr.Zero;
                        };

                        break;

                    case InputType.AlternativeWndProc:

                        SetWindowSubclass SetWindowSubclass_Hook = new SetWindowSubclass();
                        SetWindowSubclass_Hook.WindowHandle = GameHandle;
                        SetWindowSubclass_Hook.Install();
                        SetWindowSubclass_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                        {
                            if (imguiHook.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && (int)wParam == dllmain.KeyMenu)
                                {

                                    dllmain.ShowImGui_UI = !dllmain.ShowImGui_UI;

                                }

                                if (dllmain.ShowImGui_UI == true)
                                {
                                    ImplWin32.WndProcHandler(hWnd, (uint)msg, (long)wParam, (uint)lParam);
                                }
                            }
                            return IntPtr.Zero;
                        };

                        break;

                    default:

                        break;
                }


                // Hook Mouse Cursor From Games Type GTA SA.
                SetCursorPos NewHookCursor = new SetCursorPos();
                NewHookCursor.Install();

                NewHookCursor.SetCursorPos_Event += (int x, int y) =>
                {
                    NewHookCursor.BlockInput = ShowImGui_UI;
                    return false;
                };
              
            }
            catch (Exception ex) 
            {

                Runtine = false;
            }

            while (Runtine)
            {
                int keyState = WinApi.GetAsyncKeyState(Keys.F2);

                if (keyState == 1 || keyState == -32767)
                {
                    ShowImGui_UI = !ShowImGui_UI;
                }

            }

            ImplDX9.Shutdown();
            ImplWin32.Shutdown();


        }


        public static bool ShowImGui_UI = false;
        private static bool ShowImguiDemo = false;
        public static bool UI()
        {

            if (ShowImGui_UI)
            {

                ImGui.Begin("Another Window in C#", ref ShowImGui_UI);
                ImGui.Text("Hello from another window!");

                if (ImGui.Button("Show ImguiDemo"))
                    ShowImguiDemo = !ShowImguiDemo;

                if (ImGui.Button("Close Me"))
                    ShowImGui_UI = false;

                if (ShowImguiDemo)
                    ImGui.ShowDemoWindow();

                ImGui.End();
            }

            return true;
        }


    }
}
