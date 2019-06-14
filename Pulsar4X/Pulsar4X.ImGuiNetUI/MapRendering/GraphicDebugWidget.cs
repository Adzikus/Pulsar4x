﻿using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.ECSLib;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    public class GraphicDebugWindow
    {
        GlobalUIState _state;
        public static GraphicDebugWindow _graphicDebugWindow;
        GraphicDebugWidget _debugWidget;
        bool _isEnabled = false;
        public GraphicDebugWindow(GlobalUIState state)
        {
            _state = state;
            _debugWidget = new GraphicDebugWidget(new Vector4());
        }

        public static GraphicDebugWindow GetWindow(GlobalUIState state)
        {
            if (_graphicDebugWindow == null)
                _graphicDebugWindow = new GraphicDebugWindow(state);

  

            return _graphicDebugWindow;
        }

        public void Enable(bool enable, GlobalUIState state)
        {

            if (enable && !_isEnabled)
            {
                if (!state.SelectedSysMapRender.SelectedEntityExtras.Contains(_debugWidget))
                    state.SelectedSysMapRender.SelectedEntityExtras.Add(_debugWidget);
                _isEnabled = true;
            }
            else if (!enable && _isEnabled)
            {
                if (state.SelectedSysMapRender.SelectedEntityExtras.Contains(_debugWidget))
                    state.SelectedSysMapRender.SelectedEntityExtras.Remove(_debugWidget);
                _isEnabled = false;
            }
        }

        internal void Display()
        {


            ImGui.Text("Cursor World Coordinate:");
            var mouseWorldCoord = _state.Camera.MouseWorldCoordinate();
            ImGui.Text("x" + mouseWorldCoord.X);
            ImGui.SameLine();
            ImGui.Text("y" + mouseWorldCoord.Y);

            ImGui.Text("Cursor View Coordinate from math:");
            var mouseViewCoord = _state.Camera.ViewCoordinate(mouseWorldCoord);
            ImGui.Text("x" + mouseViewCoord.x);
            ImGui.SameLine();
            ImGui.Text("y" + mouseViewCoord.y);

            ImGui.Text("Camera World Coordinate:");
            var cameraWorldCoord = _state.Camera.CameraWorldPosition;
            ImGui.Text("x" + cameraWorldCoord.X);
            ImGui.SameLine();
            ImGui.Text("y" + cameraWorldCoord.Y);
            /*
            ImGui.Text("Camera View Coordinate:");
            var cameraViewCoord = _state.Camera.CameraViewCoordinate();
            ImGui.Text("x" + cameraViewCoord.x);
            ImGui.SameLine();
            ImGui.Text("y" + cameraViewCoord.y);
            */

            ImGui.Text("VSP");
            ImGui.Text("x" + _debugWidget.ViewScreenPos.x);
            ImGui.SameLine();
            ImGui.Text("y" + _debugWidget.ViewScreenPos.y);

            ImGui.Checkbox("Scales With Zoom", ref _debugWidget.Scales);
            ImGui.SliderFloat("X offset in AU", ref _debugWidget.XOffset, -2, 2);
            ImGui.SliderFloat("Y offset in AU", ref _debugWidget.YOffset, -2, 2);
            ImGui.SliderAngle("Angle", ref _debugWidget.TestingAngle);
            foreach (var item in _debugWidget.ElementItems)
            {
                ImGui.Text(item.NameString);
                if (ImGui.IsItemHovered())
                    item.SetHighlight(true);
                else
                    item.SetHighlight(false);
                ImGui.SameLine();
                ImGui.Text(item.DataString);
            }

        }
    }

    public class GraphicDebugWidget : Icon
    {
        internal float TestingAngle = 0;
        internal float XOffset = 0;
        internal float YOffset = 0;
        internal bool Scales = false;
        internal List<ElementItem> ElementItems = new List<ElementItem>();
        private List<ComplexShape> DrawComplexShapes = new List<ComplexShape>();
        PointD _ctrPnt { get { return new PointD() { X = XOffset, Y = YOffset }; } }
        ElementItem _anglelineItem;
        ElementItem _testAngleItem;
        public GraphicDebugWidget(Vector4 position) : base(position)
        {


            SDL.SDL_Color[] reflineColour =
            {   new SDL.SDL_Color() { r = 255, g = 0, b = 0, a = 100 }};
            SDL.SDL_Color[] reflineHighlight =
            {   new SDL.SDL_Color() { r = 255, g = 0, b = 0, a = 255 },};
            ElementItem refline = new ElementItem()
            {
                NameString = "Reference Line. ",
                Colour = reflineColour,
                HighlightColour = reflineHighlight,

                DataString = "This should be from center to + x",
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                {
                    new PointD(),
                    new PointD(){X = 256}
                },
                    Colors = reflineColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = false
                },
            };
            ElementItems.Add(refline);



            SDL.SDL_Color[] anglelineColour =
            {   new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 100 }};
            SDL.SDL_Color[] anglelineHighlight =
            {   new SDL.SDL_Color() { r = 0, g = 255, b = 0, a = 255 },};
            _anglelineItem = new ElementItem()
            {
                NameString = "Angle Line ",
                Colour = anglelineColour,
                HighlightColour = anglelineHighlight,

                //DataString = "Reference Line. This should be from center to + x",
                Shape = new ComplexShape()
                {
                    Points = new PointD[]
                {
                    new PointD(),
                    new PointD(){X = 256}
                },
                    Colors = reflineColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0, 0)
                    },
                    Scales = false
                },
            };
            ElementItems.Add(_anglelineItem);



            SDL.SDL_Color[] testAngleColour =
            { new SDL.SDL_Color() { r = 0, g = 0, b = 255, a = 100 } };
            SDL.SDL_Color[] testAngleHColour =
                { new SDL.SDL_Color() { r = 0, g = 0, b = 255, a = 255 } };
            _testAngleItem = new ElementItem()
            {
                NameString = "Angle Arc",
                Colour = testAngleColour,
                HighlightColour = testAngleHColour,
                DataItem = Angle.ToDegrees(TestingAngle),
                DataString = Angle.ToDegrees(TestingAngle).ToString() + "°",
                Shape = new ComplexShape()
                {
                    Points = CreatePrimitiveShapes.AngleArc(new PointD(), 128, -16, 0, TestingAngle, 128),
                    Colors = testAngleColour,
                    ColourChanges = new (int pointIndex, int colourIndex)[]
                    {
                        (0,0),
                    },
                    Scales = false
                }
            };
            ElementItems.Add(_testAngleItem);
        }


        void UpdateElements()
        {
            foreach (var item in ElementItems)
            {
                item.Shape.StartPoint = _ctrPnt;
                item.Shape.Scales = Scales;
            }

            _anglelineItem.Shape.Points = new PointD[] 
            { 
                new PointD(), 
                DrawTools.RotatePoint(new PointD() { X = 256 }, TestingAngle) 
            };
            _testAngleItem.DataString = Angle.ToDegrees(TestingAngle).ToString() + "°";
            _testAngleItem.Shape.Points = CreatePrimitiveShapes.AngleArc(new PointD(), 128, -16, 0, TestingAngle, 128);
        }


        public override void OnFrameUpdate(Matrix matrix, Camera camera)
        {
            UpdateElements();

            ViewScreenPos = camera.ViewCoordinate(WorldPosition);
            Matrix nonZoomMatrix = new Matrix();
            nonZoomMatrix.Mirror(true, false);
            DrawComplexShapes = new List<ComplexShape>() { };

            foreach (var item in ElementItems)
            {
                var shape = item.Shape;
                var startPoint = matrix.TransformD(shape.StartPoint.X, shape.StartPoint.Y); //add zoom transformation. 

                PointD[] points = new PointD[shape.Points.Length];

                for (int i = 0; i < shape.Points.Length; i++)
                {
                    var pnt = shape.Points[i];

                    int x;
                    int y;
                    PointD transformedPoint;
                    if (shape.Scales)
                        transformedPoint = matrix.TransformD(pnt.X, pnt.Y); //add zoom transformation. 
                    else
                       transformedPoint = nonZoomMatrix.TransformD(pnt.X, pnt.Y);

                    x = (int)(ViewScreenPos.x + transformedPoint.X + startPoint.X);
                    y = (int)(ViewScreenPos.y + transformedPoint.Y + startPoint.Y);
                    points[i] = new PointD() { X = x, Y = y };

                }

                DrawComplexShapes.Add(new ComplexShape()
                {
                    Points = points,
                    Colors = shape.Colors,
                    ColourChanges = shape.ColourChanges
                });
            }
        }



        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            foreach (var shape in DrawComplexShapes)
            {
                int ci = 0;
                var colour = shape.Colors[shape.ColourChanges[ci].colourIndex];
                SDL.SDL_SetRenderDrawColor(rendererPtr, colour.r, colour.g, colour.b, colour.a);

                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    if (shape.ColourChanges.Length > i && shape.ColourChanges[ci].pointIndex == i)
                    {
                        colour = shape.Colors[shape.ColourChanges[ci].colourIndex];
                        SDL.SDL_SetRenderDrawColor(rendererPtr, colour.r, colour.g, colour.b, colour.a);
                        ci++;
                    }
                    int x1 = Convert.ToInt32(shape.Points[i].X);
                    int y1 = Convert.ToInt32(shape.Points[i].Y);
                    int x2 = Convert.ToInt32(shape.Points[i + 1].X);
                    int y2 = Convert.ToInt32(shape.Points[i + 1].Y);
                    DrawTools.DrawLine(rendererPtr, x1, y1, x2, y2);
                }
            }
        }
    }
}
