using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImGuiFuncs
{
    public class VisaulBasicLimitations
    {

        public static void MouseDown(ImGuiIOPtr ImguiIO , int Index, bool value) {
            ImguiIO.MouseDown[Index] = value;
        }

    }
}
