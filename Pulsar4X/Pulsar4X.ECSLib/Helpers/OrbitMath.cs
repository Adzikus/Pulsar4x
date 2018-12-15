﻿using System;
namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// A struct to hold kepler elements without the need to give a 'parent' as OrbitDB does.
    /// </summary>
    public struct KeplerElements
    {
        public double SemiMajorAxis;        //a
        public double SemiMinorAxis;        //b
        public double Eccentricity;         //e
        public double LinierEccentricity;
        public double Periapsis;            //q
        public double Apoapsis;             //Q
        public double LoAN;                 //Omega (upper case)
        public double AoP;                  //omega (lower case)
        public double Inclination;          //i
        public double MeanAnomaly;          //M
        public double TrueAnomaly;          //v or f or theta
        //public double Period              //P
        //public double EccentricAnomaly    //E
    }

    public class OrbitMath
    {

        /// <summary>
        /// Kepler elements from velocity and position.
        /// </summary>
        /// <returns>a struct of Kepler elements.</returns>
        /// <param name="standardGravParam">Standard grav parameter.</param>
        /// <param name="position">Position ralitive to parent</param>
        /// <param name="velocity">Velocity ralitive to parent</param>
        public static KeplerElements KeplerFromVelocityAndPosition(double standardGravParam, Vector4 position, Vector4 velocity)
        {
            KeplerElements ke = new KeplerElements();
            Vector4 angularVelocity = Vector4.Cross(position, velocity);
            Vector4 nodeVector = Vector4.Cross(new Vector4(0, 0, 1, 0), angularVelocity);

            Vector4 eccentVector = EccentricityVector(standardGravParam, position, velocity);

            double eccentricity = eccentVector.Length();

            double specificOrbitalEnergy = velocity.Length() * velocity.Length() * 0.5 - standardGravParam / position.Length();


            double semiMajorAxis;
            double p; //wtf is p? idk. it's not used, but it was in the origional formula. 
            if (Math.Abs(eccentricity) > 1) //hypobola
            {
                semiMajorAxis = -(-standardGravParam / (2 * specificOrbitalEnergy));
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else if (Math.Abs(eccentricity) < 1) //ellipse
            {
                semiMajorAxis = -standardGravParam / (2 * specificOrbitalEnergy);
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else //parabola
            {
                p = angularVelocity.Length() * angularVelocity.Length() / standardGravParam;
                semiMajorAxis = double.MaxValue;
            }

            /*
            if (Math.Abs(eccentricity - 1.0) > 1e-15)
            {
                semiMajorAxis = -standardGravParam / (2 * specificOrbitalEnergy);
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else //parabola
            {
                p = angularVelocity.Length() * angularVelocity.Length() / standardGravParam;
                semiMajorAxis = double.MaxValue;
            }
*/

            double semiMinorAxis = EllipseMath.SemiMinorAxis(semiMajorAxis, eccentricity);
            double linierEccentricity = eccentricity * semiMajorAxis;

            double inclination = Math.Acos(angularVelocity.Z / angularVelocity.Length()); //should be 0 in 2d. 
            if (double.IsNaN(inclination))
                inclination = 0;

            double loANlen = nodeVector.X / nodeVector.Length();
            double longdOfAN = 0;
            if (double.IsNaN(loANlen))
                loANlen = 0;
            else
                loANlen = GMath.Clamp(loANlen, -1, 1);
            if(loANlen != 0)
                longdOfAN = Math.Acos(loANlen); //RAAN or LoAN or Omega letter



            // https://en.wikipedia.org/wiki/Argument_of_periapsis#Calculation
            double argOfPeriaps;
            if (longdOfAN == 0)
            {
                argOfPeriaps = Math.Atan2(eccentVector.Y, eccentVector.X);
                if (Vector4.Cross(position, velocity).Z < 0) //anti clockwise orbit
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }

            else
            {
                double aopLen = Vector4.Dot(nodeVector, eccentVector);
                aopLen = aopLen / (nodeVector.Length() * eccentricity);
                aopLen = GMath.Clamp(aopLen, -1, 1);
                argOfPeriaps = Math.Acos(aopLen);
                if (eccentVector.Z < 0) //anti clockwise orbit.
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }




            var eccAng = Vector4.Dot(eccentVector, position);
            eccAng = semiMajorAxis / eccAng;
            eccAng = GMath.Clamp(eccAng, -1, 1);
            var eccentricAnomoly = Math.Acos(eccAng);

            var meanAnomaly = eccentricAnomoly - eccentricity * Math.Sin(eccentricAnomoly);



            ke.SemiMajorAxis = semiMajorAxis;
            ke.SemiMinorAxis = semiMinorAxis;
            ke.Eccentricity = eccentricity;

            ke.Apoapsis = EllipseMath.Apoapsis(eccentricity, semiMajorAxis);
            ke.Periapsis = EllipseMath.Periapsis(eccentricity, semiMajorAxis);
            ke.LinierEccentricity = EllipseMath.LinierEccentricity(ke.Apoapsis, semiMajorAxis);
            ke.LoAN = longdOfAN;
            ke.AoP = argOfPeriaps;
            ke.Inclination = inclination;
            ke.MeanAnomaly = meanAnomaly;
            ke.TrueAnomaly = TrueAnomaly(eccentVector, position, velocity);
            return ke;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Eccentricity_vector
        /// </summary>
        /// <returns>The vector.</returns>
        /// <param name="sgp">StandardGravParam.</param>
        /// <param name="position">Position, ralitive to parent.</param>
        /// <param name="velocity">Velocity, ralitive to parent.</param>
        public static Vector4 EccentricityVector(double sgp, Vector4 position, Vector4 velocity)
        {
            Vector4 angularMomentum = Vector4.Cross(position, velocity);
            Vector4 foo1 = Vector4.Cross(velocity, angularMomentum);
            foo1 = foo1 / sgp;
            var foo2 = position / position.Length();
            return foo1 - foo2;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/True_anomaly#From_state_vectors
        /// </summary>
        /// <returns>The anomaly.</returns>
        /// <param name="eccentVector">Eccentricity vector.</param>
        /// <param name="position">Position ralitive to parent</param>
        /// <param name="velocity">Velocity ralitive to parent</param>
        public static double TrueAnomaly(Vector4 eccentVector, Vector4 position, Vector4 velocity)
        {

            var dotEccPos = Vector4.Dot(eccentVector, position);
            var talen = eccentVector.Length() * position.Length();
            talen = dotEccPos / talen;
            talen = GMath.Clamp(talen, -1, 1);
            var trueAnomoly = Math.Acos(talen);

            if (Vector4.Dot(position, velocity) < 0)
                trueAnomoly = Math.PI * 2 - trueAnomoly;

            return trueAnomoly;
        }


        public static Vector4 Pos(double combinedMass, double semiMajAxis, double meanAnomaly, double eccentricity, double aoP, double loAN, double i)
        {
            var G = 6.6725985e-11;


            double eca = meanAnomaly + eccentricity / 2;
            double diff = 10000;
            double eps = 0.000001;
            double e1 = 0;

            while (diff > eps)
            {
                e1 = eca - (eca - eccentricity * Math.Sin(eca) - meanAnomaly) / (1 - eccentricity * Math.Cos(eca));
                diff = Math.Abs(e1 - eca);
                eca = e1;
            }

            var ceca = Math.Cos(eca);
            var seca = Math.Sin(eca);
            e1 = semiMajAxis * Math.Sqrt(Math.Abs(1 - eccentricity * eccentricity));
            var xw = semiMajAxis * (ceca - eccentricity);
            var yw = e1 * seca;

            var edot = Math.Sqrt((G * combinedMass) / semiMajAxis) / (semiMajAxis * (1 - eccentricity * ceca));
            var xdw = -semiMajAxis * edot * seca;
            var ydw = e1 * edot * ceca;

            var Cw = Math.Cos(aoP);
            var Sw = Math.Sin(aoP);
            var co = Math.Cos(loAN);
            var so = Math.Sin(loAN);
            var ci = Math.Cos(i);
            var si = Math.Sin(i);
            var swci = Sw * ci;
            var cwci = Cw * ci;
            var pX = Cw * co - so * swci;
            var pY = Cw * so + co * swci;
            var pZ = Sw * si;
            var qx = -Sw * co - so * cwci;
            var qy = -Sw * so + co * cwci;
            var qz = Cw * si;

            return new Vector4()
            {
                X = xw * pX + yw * qx,
                Y = xw * pY + yw * qy,
                Z = xw * pZ + yw * qz
            };
        }
    }

    /// <summary>
    /// A bunch of convenient functions for calculating various ellipse parameters.
    /// </summary>
    public static class EllipseMath
    {
        /// <summary>
        /// SemiMajorAxis from SGP and SpecificEnergy
        /// </summary>
        /// <returns>The major axis.</returns>
        /// <param name="sgp">Standard Gravitational Parameter</param>
        /// <param name="specificEnergy">Specific energy.</param>
        public static double SemiMajorAxis(double sgp, double specificEnergy)
        {
            return sgp / (2 * specificEnergy);
        }

        public static double SemiMajorAxisFromApsis(double apoapsis, double periapsis)
        {
            return (apoapsis + periapsis) / 2;
        }

        public static double SemiMinorAxis(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * Math.Sqrt(1 - eccentricity * eccentricity);
        }

        public static double SemiMinorAxisFromApsis(double apoapsis, double periapsis)
        {
            return Math.Sqrt(Math.Abs(apoapsis) * Math.Abs(periapsis));
        }

        public static double LinierEccentricity(double appoapsis, double semiMajorAxis)
        {
            return appoapsis - semiMajorAxis;
        }
        public static double Eccentricity(double linierEccentricity, double semiMajorAxis)
        {
            return linierEccentricity / semiMajorAxis;
        }

        public static double Apoapsis(double eccentricity, double semiMajorAxis)
        {
            return (1 + eccentricity) * semiMajorAxis;
        }
        public static double Periapsis(double eccentricity, double semiMajorAxis)
        {
            return (1 - eccentricity) * semiMajorAxis;
        }

    }
}
