using System;
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
        public static int KeyMenu = 0x2D; // Or 45 =  VK_INSERT



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
                RenderSpy.Interfaces.IHook CurrentHook = null;

                LogConsole("Current Graphics: " + GraphicsT.ToString() + " LIB: " + RenderSpy.Graphics.Detector.GetLibByEnum(GraphicsT));

                switch (GraphicsT)
                {
                    case  Graphics.GraphicsType.d3d9:
                        {
                            ImguiHooker.D3dVersion = GraphicsT;

                            Graphics.d3d9.Present PresentHook_9 = new Graphics.d3d9.Present();
                            PresentHook_9.Install();
                            CurrentHook = PresentHook_9;

                            PresentHook_9.PresentEvent += (IntPtr device, IntPtr sourceRect, IntPtr destRect, IntPtr hDestWindowOverride, IntPtr dirtyRegion) =>
                            {
                                if (ImguiHooker.ImguiHook_Ini(device, GameHandle) == true && ShowImGui_UI == true)
                                {
                                    ImguiHooker.ImguiHook_RenderBegin(ShowImGui_UI);

                                    UI();

                                    ImguiHooker.ImguiHook_RenderEnd();
                                }

                                return PresentHook_9.Present_orig(device, sourceRect, destRect, hDestWindowOverride, dirtyRegion);
                            };
                            break;
                        }

                    default:
                        {
                            throw new Exception("The game does not render in Directx 9 mode");
                            break;
                        }
                }

                IHook InputHook = null;

                if (ResourcesExtractor.UsesModernHook == true)
                {
                    GetWindowLongPtr GetWindowLongPtr_Hook = new GetWindowLongPtr();
                    GetWindowLongPtr_Hook.WindowHandle = GameHandle;
                    GetWindowLongPtr_Hook.Install();
                    InputHook = GetWindowLongPtr_Hook;
                    GetWindowLongPtr_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                    {
                        try
                        {
                            if (ImguiHooker.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && wParam.ToInt32() == KeyMenu)
                                    ShowImGui_UI = !ShowImGui_UI;

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
                }
                else
                {
                    DefWindowProc DefWindowProcW_Hook = new DefWindowProc();
                    DefWindowProcW_Hook.WindowHandle = GameHandle;
                    DefWindowProcW_Hook.Install();
                    InputHook = DefWindowProcW_Hook;
                    DefWindowProcW_Hook.WindowProc += (IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) =>
                    {

                        try
                        {
                            if (ImguiHooker.Imgui_Ini == true)
                            {
                                if ((WM)msg == WM.KEYDOWN && wParam.ToInt32() == KeyMenu)
                                    ShowImGui_UI = !ShowImGui_UI;

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
                NewHookCursor.SetCursorPos_Event += (int x, int y) =>
                {
                    NewHookCursor.BlockInput = ShowImGui_UI;
                    return false;
                };

                bool Runtime = true;

                while (Runtime)
                {
                    Thread.Sleep(10);

                    int EndkeyState = WinApi.GetAsyncKeyState(Keys.F2); // Panic Key , Terminate cheat....

                    if (EndkeyState == 1 || EndkeyState == -32767) { Runtime = !Runtime; } 


                }

                CurrentHook.Uninstall();
                InputHook.Uninstall();
                NewHookCursor.Uninstall();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Imgui Hook Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            ImGuiNET.ImGui.Begin("Another Window in VB", ref ShowImGui_UI);
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
