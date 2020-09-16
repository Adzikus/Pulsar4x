﻿using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using SDL2;

namespace Pulsar4X.SDL2UI
{
    /// <summary>
    /// Drawing helpers class, inverts Y on drawcalls 
    /// </summary>
    public static class DrawTools
    {

        /// <summary>
        /// Rotates a given point to a given angle.
        /// </summary>
        /// <returns>The point.</returns>
        /// <param name="point">Point.</param>
        /// <param name="angle">Angle.</param>
        public static Orbital.Vector2 RotatePoint(Orbital.Vector2 point, double angle)
        {
            Orbital.Vector2 newPoint = new Orbital.Vector2()
            {
                X = (point.X * Math.Cos(angle)) - (point.Y * Math.Sin(angle)),
                Y = (point.X * Math.Sin(angle)) + (point.Y * Math.Cos(angle))
            };
            return newPoint;
        }
    }

    /// <summary>
    /// A collection of points and a single color.
    /// </summary>
    public struct Shape
    {
        public SDL.SDL_Color Color;    //could change due to entity changes. 
        public Orbital.Vector2[] Points; //relative to the IconPosition. could change with entity changes. 
    }

    public class MutableShape
    {
        public SDL.SDL_Color Color;
        public List<Orbital.Vector2> Points;
        public bool Scales = true;
    }


    public class ComplexShape
    {
        public Orbital.Vector2 StartPoint;
        public Orbital.Vector2[] Points;
        public SDL.SDL_Color[] Colors;
        public (int pointIndex, int colourIndex)[] ColourChanges; //at Points[item1] we change to Colors[item2]
        public bool Scales;

    }

    internal class ElementItem
    {
        internal string NameString;
        internal double DataItem;
        internal string DataString = "";
        internal ComplexShape Shape;
        internal SDL.SDL_Color[] Colour;
        internal SDL.SDL_Color[] HighlightColour;

        internal void SetHighlight(bool isHighlighted)
        {
            if (isHighlighted)
                Shape.Colors = HighlightColour;
            else
                Shape.Colors = Colour;
        }
    }

}
