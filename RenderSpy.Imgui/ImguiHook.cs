using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static RenderSpy.Imgui.dllmain;

namespace RenderSpy.Imgui
{
    public class ImguiHook
    {
        public readonly DxVer D3dVersion = DxVer.d3d9;
        public ImguiHook(DxVer d3dver) { D3dVersion = d3dver; }

        [DllImport("cimgui")] public static extern IntPtr igCreateContext(IntPtr fontAtlas); // CreateContext

        public  bool Imgui_Ini = false; 

        public  bool ImguiHook_Ini(IntPtr Adress, IntPtr GameHandle) {

            try
            {
                if (Imgui_Ini == true)
                {
                    return true;
                }
                else {
                    Imgui_Ini = true;
                    var context = igCreateContext(IntPtr.Zero);

                    ImplWin32.Init(GameHandle);
                    switch (D3dVersion)
                    {
                        case DxVer.d3d9:
                            ImplDX9.Init(Adress);
                            break;
                        case DxVer.d3d11:
                            ImplDx11.Init(Adress);
                            break;
                        default:

                            break;
                    }

                }
            }
            catch (Exception e)
            {
                //Console.WriteLine("Imgui Init Failed!!");
                //Console.WriteLine(e.Message);
                Imgui_Ini = false;
            }

            return Imgui_Ini;
        }

        public bool ImguiHook_Render(IntPtr Device, bool ShowUI)
        {
            bool Result = true;
            try
            {

                if (Imgui_Ini == true && ShowUI == true)
                {
                   
                    switch (D3dVersion)
                    {
                        case DxVer.d3d9:
                            ImplDX9.NewFrame();
                            break;
                        case DxVer.d3d11:
                            ImplDx11.NewFrame();
                            break;
                        default:

                            break;
                    }

                    ImplWin32.NewFrame();  //cimgui_wrapper.ImGui_ImplWin32_NewFrame();
                    ImGui.NewFrame();     //cimgui_wrapper.igNewFrame();

                    ImGuiIOPtr IO = ImGui.GetIO();

                    IO.MouseDrawCursor = ShowUI;

                    //ImGui_ImplWin32_UpdateMousePos();

                    dllmain.UI();


                    ImGui.EndFrame(); //cimgui_wrapper.igEndFrame();
                    ImGui.Render(); //cimgui_wrapper.igRender();
                   
                    switch (D3dVersion)
                    {
                        case DxVer.d3d9:
                            ImplDX9.RenderDrawData(ImGui.GetDrawData());
                            break;
                        case DxVer.d3d11:
                            ImplDx11.RenderDrawData(ImGui.GetDrawData());
                            break;
                        default:

                            break;
                    }


                }

            }
            catch (Exception ex)
            {
               
                Result = false;
            }

            return Result;
        }

    }
}
