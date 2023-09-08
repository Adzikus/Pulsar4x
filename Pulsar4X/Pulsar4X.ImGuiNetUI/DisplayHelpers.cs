using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class DisplayHelpers
    {
        public static void Header(string text, string tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(text);
            if(!string.IsNullOrEmpty(tooltip))
            {
                ImGui.SameLine();
                ImGui.Text("[?]");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
            }
            ImGui.PopStyleColor();
            ImGui.Separator();
        }

        public static void PrintRow(string one, string two, string tooltipOne = null, string tooltipTwo = null, bool separator = true)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(one);
            ImGui.PopStyleColor();
            if(tooltipOne != null)
            {
                if(ImGui.IsItemHovered()) ImGui.SetTooltip(tooltipOne);
            }
            ImGui.NextColumn();

            ImGui.Text(two);
            if(tooltipTwo != null)
            {
                if(ImGui.IsItemHovered()) ImGui.SetTooltip(tooltipTwo);
            }
            ImGui.NextColumn();

            if(separator)
                ImGui.Separator();
        }

        public static void ShipTooltip(Entity ship)
        {
            if(!ship.TryGetDatablob<ShipInfoDB>(out var shipInfo))
                return;

            if(ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Design: " + shipInfo.Design.Name);
                ImGui.EndTooltip();
            }
        }
    }
}