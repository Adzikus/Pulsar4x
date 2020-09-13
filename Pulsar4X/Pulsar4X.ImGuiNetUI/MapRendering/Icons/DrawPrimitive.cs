﻿using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using SDL2;
using Point = SDL2.SDL.SDL_Point;

namespace Pulsar4X.SDL2UI
{
    public static class DrawPrimitive
    {
        public static void DrawEllipse(IntPtr renderer, int posX, int posY, double xRadius, double yRadius)
        {
            byte _numberOfArcSegments = 255;

            double angle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xRadius * Math.Sin(angle));
            int lastY = posY + (int)Math.Round(yRadius * Math.Cos(angle));
            int drawX;
            int drawY;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                drawX = posX + (int)Math.Round(xRadius * Math.Sin(angle * i));
                drawY = posY + (int)Math.Round(yRadius * Math.Cos(angle * i));
                //SDL.SDL_RenderDrawPoint(renderer, drawX, drawY);
                SDL.SDL_RenderDrawLine(renderer, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }
        }


        /// <summary>
        /// This is maybe a naive possibly slow way of drawing a filled circle. maybe revisit this if it turns out to be slow.
        /// </summary>
        /// <param name="renderer">Renderer.</param>
        /// <param name="posX">Position x.</param>
        /// <param name="posY">Position y.</param>
        /// <param name="radius">Radius.</param>
        public static void DrawFilledCircle(IntPtr renderer, int posX, int posY, int radius)
        { 

            for (int w = 0; w < radius * 2; w++)
            {
                for (int h = 0; h < radius * 2; h++)
                {
                    int dx = radius - w; // horizontal offset
                    int dy = radius - h; // vertical offset
                    if ((dx * dx + dy * dy) <= (radius * radius))
                    {
                        SDL.SDL_RenderDrawPoint(renderer, posX, posY);
                    }
                }
            }        
        }

        public static void DrawArc(IntPtr renderer, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians)
        {
            byte _numberOfArcSegments = 255;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int drawX;
            int drawY;
            int totalSegments = (int)(Math.Abs(arcAngleRadians) / incrementAngle);


            SDL.SDL_Point[] points = new SDL.SDL_Point[totalSegments];

            for (int i = 0; i < totalSegments; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                points[i] = new SDL.SDL_Point() { x = drawX, y = drawY };
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                SDL.SDL_RenderDrawLine(renderer, points[i].x, points[i].y, points[i+1].x, points[i+1].y);
            }

        }


        public static void DrawAlphaFadeArc(IntPtr rendererPtr, int posX, int posY, double xWidth, double yWidth, double startAngleRadians, double arcAngleRadians, byte startAlpha, byte endAlpha)
        {
            byte r, g, b, a;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);

            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);

            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
            byte _numberOfArcSegments = 254;


            byte alpha = endAlpha;

            double incrementAngle = (Math.PI * 2.0) / (_numberOfArcSegments);

            int lastX = posX + (int)Math.Round(xWidth * Math.Sin(startAngleRadians));
            int lastY = posY + (int)Math.Round(yWidth * Math.Cos(startAngleRadians));
            int drawX;
            int drawY;
            int totalSegments = (int)(arcAngleRadians / incrementAngle);
            double alphaIncrement = (startAlpha - endAlpha) / totalSegments;
            for (int i = 1; i < totalSegments; i++)
            {
                alpha += (byte)(alphaIncrement);
                SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, alpha);

                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + (int)Math.Round(xWidth * Math.Sin(nextAngle));
                drawY = posY + (int)Math.Round(yWidth * Math.Cos(nextAngle));
                SDL.SDL_RenderDrawLine(rendererPtr, lastX, lastY, drawX, drawY);
                lastX = drawX;
                lastY = drawY;
            }

            SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was originaly
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }

    public static class CreatePrimitiveShapes
    {

        public const double PI2 = Math.PI * 2;
        public const double HalfCircle = Math.PI;
        public const double QuarterCircle = Math.PI * 0.5;
        public const double ThreeQuarterCircle = HalfCircle + QuarterCircle;


        private static Shape _centerWidget;
        
        public static Shape CenterWidget(Matrix matrix)
        {
            if(_centerWidget.Points == null)
            {
                PointD[] drawpoints = new PointD[5];
                drawpoints[0] = new PointD(0, -16);
                drawpoints[1] = new PointD(0, 16);
                drawpoints[2] = new PointD(16, 0);
                drawpoints[3] = new PointD(-16, 0);
                drawpoints[4] = new PointD(0, -16);
                byte r = 150;
                byte g = 50;
                byte b = 200;
                byte a = 255;
                SDL.SDL_Color colour = new SDL.SDL_Color() {r = r, g = g, b = b, a = a};
                _centerWidget = new Shape() {Points = drawpoints, Color = colour};
            }
            
            
            Shape centerWidget = new Shape();
            centerWidget.Points = matrix.Transform(_centerWidget.Points);
            centerWidget.Color = _centerWidget.Color;
            return centerWidget;
        }
        

        /// <summary>
        /// Creates the arc.
        /// </summary>
        /// <returns>The arc.</returns>
        /// <param name="posX">Position x.</param>
        /// <param name="posY">Position y.</param>
        /// <param name="xRadius">X width.</param>
        /// <param name="yRadius">Y width.</param>
        /// <param name="startAngleRadians">Start angle in radians.</param>
        /// <param name="arcAngleRadians">Arc angle in radians.</param>
        /// <param name="segments">Number of segments this arc will have, resolution. ie a full circle with 6 arcs will draw a hexigon.</param>
        public static PointD[] CreateArc(int posX, int posY, double xRadius, double yRadius, double startAngleRadians, double arcAngleRadians, int segments)
        {
            PointD[] points = new PointD[segments + 1];

            double incrementAngle = arcAngleRadians / segments;

            double drawX;
            double drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawX = posX + xRadius * Math.Sin(nextAngle);
                drawY = posY + yRadius * Math.Cos(nextAngle);
                points[i] = new PointD() { X = drawX, Y = drawY };
            }

            return points;
        }

        public static PointD[] AngleArc(PointD ctrPos, double radius, double tipLen, double startAngleRadians, double arcAngleRadians, int segments)
        {

            double drawX;
            double drawY;

            PointD[] points = new PointD[segments + 3];

            points[0] = new PointD()
            {
                Y = ctrPos.Y + (radius + tipLen) * Math.Sin(startAngleRadians),
                X = ctrPos.X + (radius + tipLen) * Math.Cos(startAngleRadians)
            };

            double incrementAngle = arcAngleRadians / segments;

            for (int i = 0; i < points.Length - 2; i++)
            {
                double nextAngle = startAngleRadians + incrementAngle * i;
                drawY = ctrPos.Y + radius * Math.Sin(nextAngle);
                drawX = ctrPos.X + radius * Math.Cos(nextAngle);
                points[i+1] = new PointD() { X = drawX, Y = drawY };
            }

            points[points.Length - 1] = new PointD()
            {
                Y = ctrPos.Y + (radius + tipLen) * Math.Sin(startAngleRadians + arcAngleRadians),
                X = ctrPos.X + (radius + tipLen) * Math.Cos(startAngleRadians + arcAngleRadians)
            };
            return points;
        }

        public static PointD[] RoundedCylinder(int minorRadius, int majorRadius, int offsetX, int offsetY)
        {
            List<PointD> points = new List<PointD>();
            double x1 = (minorRadius * 0.5);
            double y1 = (majorRadius * 0.5 - minorRadius * 0.5);

            points.AddRange(CreateArc(offsetX, (int)(y1 + offsetY), x1, x1, ThreeQuarterCircle, HalfCircle, 16));
            points.Add(new PointD() { X = x1 + offsetX, Y = y1 + offsetY });
            points.Add(new PointD() { X = x1 + offsetX, Y = -y1 + offsetY });

            points.AddRange(CreateArc(offsetX, (int)(-y1 + offsetY), x1, x1, QuarterCircle, HalfCircle, 16));
            points.Add(new PointD() { X = -x1 + offsetX, Y = -y1 + offsetY });
            points.Add(new PointD() { X = -x1 + offsetX, Y = y1 + offsetY });
            return points.ToArray();
        }

        public enum PosFrom
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }
        public static PointD[] Rectangle(int posX, int posY, int width, int height, PosFrom positionFrom = PosFrom.TopLeft)
        {

            var points = new PointD[4] ;
            PointD tl;
            PointD tr;
            PointD br;
            PointD bl;

            switch (positionFrom)
            {
                case PosFrom.TopLeft:
                    {
                        tl.X = posX;
                        tl.Y = posY;
                        tr.X = posX + width;
                        tr.Y = posY;
                        br.X = posX + width;
                        br.Y = posY + height;
                        bl.X = posX;
                        bl.Y = posY + height;
                        points = new PointD[] { tl, tr, br, bl };
                    }
                    break;
                case PosFrom.TopRight:
                    { 
                        tr.X = posX;
                        tr.Y = posY;
                        br.X = posX;
                        br.Y = posY + height;
                        bl.X = posX - width;
                        bl.Y = posY + height;
                        tl.X = posX - width;
                        tl.Y = posY;
                        points = new PointD[] { tr, br, bl, tl };
                    }
                    break;
                case PosFrom.BottomRight:
                    {
                        br.X = posX;
                        br.Y = posY;
                        bl.X = posX - width;
                        bl.Y = posY;
                        tl.X = posX - width;
                        tl.Y = posY - height;
                        tr.X = posX;
                        tr.Y = posY - height;
                        points = new PointD[] { br, bl, tl, tr };
                    }
                    break;
                case PosFrom.BottomLeft:
                    {

                        bl.X = posX;
                        bl.Y = posY;
                        tl.X = posX;
                        tl.Y = posY - height;
                        tr.X = posX + width;
                        tr.Y = posY - height;
                        br.X = posX + width;
                        br.Y = posY;
                        points = new PointD[] { bl, tl, tr, br };
                    }
                    break;
                case PosFrom.Center:
                    {
                        tl.X = posX - (int)(width * 0.5);
                        tl.Y = posY - (int)(height * 0.5);
                        tr.X = posX + (int)(width * 0.5);
                        tr.Y = posY - (int)(height * 0.5);
                        br.X = posX + (int)(width * 0.5);
                        br.Y = posY + (int)(height * 0.5);
                        bl.X = posX - (int)(width * 0.5);
                        bl.Y = posY + (int)(height * 0.5);
                        points = new PointD[] { tl, tr, br, bl, tl };
                    }
                    break;
            }
            return points;
        }
        
        /// <summary>
        /// CopyPasta straight from the french wikipedia page here: https://fr.wikipedia.org/wiki/Algorithme_de_trac%C3%A9_d%27arc_de_cercle_de_Bresenham#En_C#
        /// </summary>
        /// <param name="xc">center</param>
        /// <param name="yc">center</param>
        /// <param name="r">radius</param>
        /// <returns></returns>
        public static List<Point> BresenhamCircle(int xc,int yc,int r)
        {
            List<Point> ret = new List<Point>();
            int x,y,p;

            x=0;
            y=r;

            ret.Add(new Point(){x = xc+x,y = yc-y});

            p=3-(2*r);

            for(x=0;x<=y;x++)
            {
                if (p<0)
                {
                    p=(p+(4*x)+6);
                }
                else
                {
                    y-=1;
                    p+=((4*(x-y)+10));
                }

                ret.Add(new Point(){x = xc+x,y = yc-y});
                ret.Add(new Point(){x = xc-x,y = yc-y});
                ret.Add(new Point(){x = xc+x,y = yc+y});
                ret.Add(new Point(){x = xc-x,y = yc+y});
                ret.Add(new Point(){x = xc+y,y = yc-x});
                ret.Add(new Point(){x = xc-y,y = yc-x});
                ret.Add(new Point(){x = xc+y,y = yc+x});
                ret.Add(new Point(){x = xc-y,y = yc+x});
            }
            return ret;
        }
        public static PointD[] Circle(int posX, int posY, double radius, short segments)
        {
            PointD[] points = new PointD[segments + 1];
            double incrementAngle = PI2 / segments;

            double drawX;
            double drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = incrementAngle * i;
                drawX = posX + radius * Math.Sin(nextAngle);
                drawY = posY + radius * Math.Cos(nextAngle);
                points[i] = new PointD() { X = drawX, Y = drawY };
            }

            return points;

        }

        public static PointD[] Circle(PointD pos, double radius, short segments)
        {
            PointD[] points = new PointD[segments + 1];
            double incrementAngle = PI2 / segments;

            double drawX;
            double drawY;

            for (int i = 0; i < segments + 1; i++)
            {
                double nextAngle = 0 + incrementAngle * i;
                drawX = pos.X + radius * Math.Sin(nextAngle);
                drawY = pos.Y + radius * Math.Cos(nextAngle);
                points[i] = new PointD() { X = drawX, Y = drawY };
            }

            return points;

        }

        public static PointD[] CreateArrow(int len = 32)
        {
            PointD[] arrowPoints = new PointD[7];

            arrowPoints[0] = new PointD() { X =  0, Y = 0 };
            arrowPoints[1] = new PointD() { X =  0, Y = len -2 };
            arrowPoints[2] = new PointD() { X =  3, Y = len -3 };
            arrowPoints[3] = new PointD() { X =  0, Y = len };
            arrowPoints[4] = new PointD() { X = -3, Y = len -3 };
            arrowPoints[5] = new PointD() { X =  0, Y = len -2 };
            arrowPoints[6] = new PointD() { X =  0, Y = 0  };

            return arrowPoints;
        }



    }



    public static class DrawShapes
    {
        public static void Draw(IntPtr rendererPtr, Camera camera, Shape[] shapes)
        {
            //first get the current colour and blend mode and store this. 
            byte r, g, b, a;
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out r, out g, out b, out a);
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);

            //change the blendmode to blend (maybe we should store this in Shape? probilby not, I think we'll always want blend.)
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            //go through each of the shapes 
            foreach (var shape in shapes)
            {
                //set the colour as defined in the shape. 
                SDL.SDL_SetRenderDrawColor(rendererPtr, shape.Color.r, shape.Color.g, shape.Color.b, shape.Color.a);
                //then go through each of the points and draw a line from one point to the next. 
                for (int i = 0; i < shape.Points.Length - 1; i++)
                {
                    SDL.SDL_RenderDrawLine(rendererPtr, (int)shape.Points[i].X, (int)shape.Points[i].Y, (int)shape.Points[i + 1].X, (int)shape.Points[i + 1].Y);
                }
            }

            //set the colour and blendmode back to what it was originaly.
            SDL.SDL_SetRenderDrawColor(rendererPtr, r, g, b, a); //set the colour back to what it was originaly
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }

    public struct PointD
    {
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X;
        public double Y;
    }
    public static class PointDFunctions
    {
        public static PointD NewFrom(Orbital.Vector3 vector3)
        {
            return new PointD() { X = vector3.X, Y = vector3.Y };
        }
        public static PointD NewFrom(SDL.SDL_Point sDL_Point)
        {
            return new PointD() { X = sDL_Point.x, Y = sDL_Point.y };
        }
        public static PointD Add(PointD p1, PointD p2)
        {
            return new PointD() { X = p1.X + p2.X, Y = p1.Y + p2.Y };
        }
        public static PointD Sub(PointD p1, PointD p2)
        {
            return new PointD() { X = p1.X - p2.X, Y = p1.Y - p2.Y };
        }
        public static double Length(PointD point)
        {
            return Math.Sqrt(LengthSquared(point)); 
        }
        public static double LengthSquared(PointD point)
        {
            return (point.X * point.X) + (point.Y * point.Y);
        }

    }



    public interface IRectangle
    {
        float X { get;  }
        float Y { get;  }
        float Width { get;  }
        float Height { get;  }


    }

    public class Rectangle : IRectangle
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public static Rectangle operator +(Rectangle rectangle, Vector2 point)
        {
            Rectangle newRect = new Rectangle();

            newRect.X = rectangle.X + point.X;
            newRect.Y = rectangle.Y + point.Y;
            return newRect;

        }

        public bool Intersects(IRectangle icon)
        {
            float myL = X;
            float myR = X + Width;
            float myT = Y;
            float myB = Y + Height;


            float iconL = icon.X;
            float iconR = icon.X + icon.Width;
            float iconT = icon.Y;
            float iconB = icon.Y + icon.Height;


            return (myL < iconR &&
                    myR > iconL &&
                    myT < iconB &&
                    myB > iconT);
        }
    }

}
