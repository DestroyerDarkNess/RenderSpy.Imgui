using System;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace RenderSpy.Imgui
{
   
    public class ImguiHook
    {
        [DllImport("cimgui")]
        public static extern IntPtr igCreateContext(IntPtr fontAtlas);

        public bool Imgui_Ini { get; set; } = false;
        public Graphics.GraphicsType D3dVersion { get; set; } = Graphics.GraphicsType.unknown;


        public bool ImguiHook_Ini(IntPtr Adress, IntPtr GameHandle)
        {
            try
            {
                if (D3dVersion == Graphics.GraphicsType.d3d9)
                {
                    if (Imgui_Ini == true)
                        return true;
                    else
                    {
                        Imgui_Ini = true;
                        var context = igCreateContext(IntPtr.Zero);
                        ImplWin32.Init(GameHandle);
                        ImplDX9.Init(Adress);
                    }
                }
            }
            catch (Exception ex)
            {
                Imgui_Ini = false;
            }

            return Imgui_Ini;
        }

        public bool ImguiHook_RenderBegin()
        {
            bool Result = true;

            try
            {
                if (D3dVersion == Graphics.GraphicsType.d3d9)
                {
                    ImplDX9.NewFrame();
                    ImplWin32.NewFrame();

                    ImGuiNET.ImGui.NewFrame();

                
                }
            }
            catch (Exception ex)
            {
                 Result = false;
            }

            return Result;
        }

        public bool ImguiHook_RenderEnd()
        {
            bool Result = true;

            try
            {
                if (D3dVersion == Graphics.GraphicsType.d3d9)
                {
                    ImGuiNET.ImGui.EndFrame();
                    ImGuiNET.ImGui.Render();

                    ImplDX9.RenderDrawData(ImGuiNET.ImGui.GetDrawData());
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
