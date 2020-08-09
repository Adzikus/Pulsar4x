﻿using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Orbit processor.
    /// How Orbits are calculated:
    /// First we get the time since epoch. (time from when the planet is at its closest to it's parent)
    /// Then we get the Mean Anomaly. (stored) 
    /// Eccentric Anomaly is calculated from the Mean Anomaly, and takes the most work. 
    /// True Anomaly, is calculated using the Eccentric Anomaly this is the angle from the parent (or focal point of the ellipse) to the body. 
    /// With the true anomaly, we can then use trig to calculate the position.  
    /// </summary>
    public class OrbitProcessor : OrbitProcessorBase, IHotloopProcessor
    {
        /// <summary>
        /// TypeIndexes for several dataBlobs used frequently by this processor.
        /// </summary>
        private static readonly int OrbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
        private static readonly int PositionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
        private static readonly int StarInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pulsar4X.ECSLib.OrbitProcessor"/> use relative velocity.
        /// </summary>
        /// <value><c>true</c> if use relative velocity; otherwise, uses absolute <c>false</c>.</value>
        public static bool UserelativeVelocity { get; set; } = true;

        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface. 
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DateTime toDate = entity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(entity, entity.GetDataBlob<OrbitDB>().Parent.GetDataBlob<PositionDB>(), toDate);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateSystemOrbits(manager, toDate);
        }

        internal static void UpdateSystemOrbits(EntityManager manager, DateTime toDate)
        {
       
            //TimeSpan orbitCycle = manager.Game.Settings.OrbitCycleTime;
            //DateTime toDate = manager.ManagerSubpulses.SystemLocalDateTime + orbitCycle;
            //starSystem.SystemSubpulses.AddSystemInterupt(toDate + orbitCycle, UpdateSystemOrbits);
            //manager.ManagerSubpulses.AddSystemInterupt(toDate + orbitCycle, PulseActionEnum.OrbitProcessor);
            // Find the first orbital entity.
            Entity firstOrbital = manager.GetFirstEntityWithDataBlob(StarInfoTypeIndex);

            if (!firstOrbital.IsValid)
            {
                // No orbitals in this manager.
                return;
            }

            Entity root = firstOrbital.GetDataBlob<OrbitDB>(OrbitTypeIndex).Root;
            var rootPositionDB = root.GetDataBlob<PositionDB>(PositionTypeIndex);

            // Call recursive function to update every orbit in this system.
            UpdateOrbit(root, rootPositionDB, toDate);
        }

        public static void UpdateOrbit(ProtoEntity entity, PositionDB parentPositionDB, DateTime toDate)
        {
            var entityOrbitDB = entity.GetDataBlob<OrbitDB>(OrbitTypeIndex);
            var entityPosition = entity.GetDataBlob<PositionDB>(PositionTypeIndex);

            //if(toDate.Minute > entityOrbitDB.OrbitalPeriod.TotalMinutes)

            // Get our Parent-Relative coordinates.
            try
            {
                Vector3 newPosition = GetPosition_AU(entityOrbitDB, toDate);

                // Get our Absolute coordinates.
                entityPosition.AbsolutePosition_AU = parentPositionDB.AbsolutePosition_AU + newPosition;

            }
            catch (OrbitProcessorException e)
            {
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.  
                Event evt = new Event(StaticRefLib.CurrentDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + entity.Guid + " " + e.Message);
                evt.EventType = EventType.Opps;
                StaticRefLib.EventLog.AddEvent(evt);
            }

            // Update our children.
            foreach (Entity child in entityOrbitDB.Children)
            {
                // RECURSION!
                UpdateOrbit(child, entityPosition, toDate);
            }
        }


        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetPosition_AU(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new Vector3(0, 0, 0);
            }
            return GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
        }

        public static Vector3 GetPosition_m(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new Vector3(0, 0, 0);
            }

            return GetPosition_m(orbit, GetTrueAnomaly(orbit, time));
        }
        
        public static Vector3 GetPosition_m(KeplerElements orbit, DateTime time)
        {
            return GetPosition_m(orbit, GetTrueAnomaly(orbit, time));
        }

        /// <summary>
        /// Calculates the root relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetAbsolutePosition_AU(OrbitDB orbit, DateTime time)
        {
            if (orbit.Parent == null)//if we're the parent sun
                return GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
            //else if we're a child
            Vector3 rootPos = orbit.Parent.GetDataBlob<PositionDB>().AbsolutePosition_AU;
            if (orbit.IsStationary)
            {
                return rootPos;
            }

            if(orbit.Eccentricity < 1)
                return rootPos + GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
            else
                return rootPos + GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
            //if (orbit.Eccentricity == 1)
            //    return GetAbsolutePositionForParabolicOrbit_AU();
            //else
            //    return GetAbsolutePositionForHyperbolicOrbit_AU(orbit, time);

        }
        
        public static Vector3 GetAbsolutePosition_m(OrbitDB orbit, DateTime time)
        {
            if (orbit.Parent == null)//if we're the parent sun
                return GetPosition_m(orbit, GetTrueAnomaly(orbit, time));
            //else if we're a child
            Vector3 rootPos = orbit.Parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            if (orbit.IsStationary)
            {
                return rootPos;
            }

            if(orbit.Eccentricity < 1)
                return rootPos + GetPosition_m(orbit, GetTrueAnomaly(orbit, time));
            else
                return rootPos + GetPosition_m(orbit, GetTrueAnomaly(orbit, time));
            //if (orbit.Eccentricity == 1)
            //    return GetAbsolutePositionForParabolicOrbit_AU();
            //else
            //    return GetAbsolutePositionForHyperbolicOrbit_AU(orbit, time);

        }

        //public static Vector4 GetAbsolutePositionForParabolicOrbit_AU()
        //{ }

        //public static Vector4 GetAbsolutePositionForHyperbolicOrbit_AU(OrbitDB orbitDB, DateTime time)
        //{
            
        //}

        public static double GetTrueAnomaly(OrbitDB orbit, DateTime time)
        {
            TimeSpan timeSinceEpoch = time - orbit.Epoch;

            // Don't attempt to calculate large timeframes.
            while (timeSinceEpoch > orbit.OrbitalPeriod && orbit.OrbitalPeriod.Ticks != 0)
            {
                long years = timeSinceEpoch.Ticks / orbit.OrbitalPeriod.Ticks;
                timeSinceEpoch -= TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
                orbit.Epoch += TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
            }

            double m0 = orbit.MeanAnomalyAtEpoch;
            double n = orbit.MeanMotion;
            double currentMeanAnomaly = OrbitMath.GetMeanAnomalyFromTime(m0, n, timeSinceEpoch.TotalSeconds);

            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);
            return OrbitMath.TrueAnomalyFromEccentricAnomaly(orbit.Eccentricity, eccentricAnomaly);
            /*
            var x = Math.Cos(eccentricAnomaly) - orbit.Eccentricity;
            var y = Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly);
            return Math.Atan2(y, x);
            */
        }
        
        public static double GetTrueAnomaly(KeplerElements orbit, DateTime time)
        {
            TimeSpan timeSinceEpoch = time - orbit.Epoch;

            // Don't attempt to calculate large timeframes.
            while (timeSinceEpoch.TotalSeconds > orbit.OrbitalPeriod && orbit.OrbitalPeriod != 0)
            {
                double years = timeSinceEpoch.TotalSeconds / orbit.OrbitalPeriod;
                timeSinceEpoch -= TimeSpan.FromSeconds(years * orbit.OrbitalPeriod);
                orbit.Epoch += TimeSpan.FromSeconds(years * orbit.OrbitalPeriod);
            }

            double m0 = orbit.MeanAnomalyAtEpoch;
            double n = orbit.MeanMotion;
            double currentMeanAnomaly = OrbitMath.GetMeanAnomalyFromTime(m0, n, timeSinceEpoch.TotalSeconds);

            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);
            return OrbitMath.TrueAnomalyFromEccentricAnomaly(orbit.Eccentricity, eccentricAnomaly);
            /*
            var x = Math.Cos(eccentricAnomaly) - orbit.Eccentricity;
            var y = Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly);
            return Math.Atan2(y, x);
            */
        }

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static Vector3 GetPosition_AU(OrbitDB orbit, double trueAnomaly)
        {

            if (orbit.IsStationary)
            {
                return new Vector3(0, 0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = orbit.SemiMajorAxis_AU * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            double incl = orbit.Inclination;

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = orbit.LongitudeOfAscendingNode;
            //double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double angleFromLoAN = trueAnomaly + orbit.ArgumentOfPeriapsis;

            double x = Math.Cos(lofAN) * Math.Cos(angleFromLoAN) - Math.Sin(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(angleFromLoAN) + Math.Cos(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(angleFromLoAN);

            return new Vector3(x, y, z) * radius;
        }
        
        public static Vector3 GetPosition_m(OrbitDB orbit, double trueAnomaly)
        {

            if (orbit.IsStationary)
            {
                return new Vector3(0, 0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            double incl = orbit.Inclination;

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = orbit.LongitudeOfAscendingNode;
            //double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double angleFromLoAN = trueAnomaly + orbit.ArgumentOfPeriapsis;

            double x = Math.Cos(lofAN) * Math.Cos(angleFromLoAN) - Math.Sin(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(angleFromLoAN) + Math.Cos(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(angleFromLoAN);

            return new Vector3(x, y, z) * radius;
        }
        
        public static Vector3 GetPosition_m(KeplerElements orbit, double trueAnomaly)
        {
            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            double incl = orbit.Inclination;

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = orbit.LoAN;
            //double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double angleFromLoAN = trueAnomaly + orbit.AoP;

            double x = Math.Cos(lofAN) * Math.Cos(angleFromLoAN) - Math.Sin(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(angleFromLoAN) + Math.Cos(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(angleFromLoAN);

            return new Vector3(x, y, z) * radius;
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        public static double GetEccentricAnomaly(OrbitDB orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            const int numIterations = 1000;
            var e = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (orbit.Eccentricity > 0.8)
            {
                e[i] = Math.PI;
            }
            else
            {
                e[i] = currentMeanAnomaly;
            }

            do
            {
                // Newton's Method.
                /*					 E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                e[i + 1] = e[i] - (e[i] - orbit.Eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cos(e[i]));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly.");
                gameEvent.Entity = orbit.OwningEntity;
                gameEvent.EventType = EventType.Opps;

                StaticRefLib.EventLog.AddEvent(gameEvent);
                //throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit.OwningEntity);
            }

            return e[i - 1];
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        public static double GetEccentricAnomaly(KeplerElements orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            const int numIterations = 1000;
            var e = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (orbit.Eccentricity > 0.8)
            {
                e[i] = Math.PI;
            }
            else
            {
                e[i] = currentMeanAnomaly;
            }

            do
            {
                // Newton's Method.
                /*					 E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                e[i + 1] = e[i] - (e[i] - orbit.Eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cos(e[i]));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly from kepler Elements.");
                gameEvent.EventType = EventType.Opps;
                StaticRefLib.EventLog.AddEvent(gameEvent);
            }

            return e[i - 1];
        }
        
        /// <summary>
        /// Untested.
        /// Gets the Eccentric Anomaly for a hyperbolic trajectory.
        /// This still requres the Mean Anomaly to be known however.
        /// Which I'm unsure how to calculate from time. so this may not be useful. included for completeness. 
        /// </summary>
        /// <param name="orbit"></param>
        /// <param name="currentMeanAnomaly"></param>
        /// <returns></returns>
        public static double GetEccentricAnomalyH(OrbitDB orbit, double currentMeanAnomaly)
        {
            
            //Kepler's Equation
            const int numIterations = 1000;
            var f = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (orbit.Eccentricity > 0.8)
            {
                f[i] = Math.PI;
            }
            else
            {
                f[i] = currentMeanAnomaly;
            }
            
            do
            {
                // Newton's Method.
                /*					 F(n) - e sinh(E(n)) - M(t)
                 * F(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cosh(F(n)
                 * 
                 * F == EccentricAnomalyH, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                f[i + 1] = f[i] - (f[i] - orbit.Eccentricity * Math.Sinh(f[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cosh(f[i]));
                i++;
            } while (Math.Abs(f[i] - f[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly.");
                gameEvent.Entity = orbit.OwningEntity;
                gameEvent.EventType = EventType.Opps;

                StaticRefLib.EventLog.AddEvent(gameEvent);
                //throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit.OwningEntity);
            }

            return f[i - 1];
        }

        /// <summary>
        /// Gets the orbital vector, will be either Absolute or relative depending on static bool UserelativeVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 GetOrbitalVector_AU(OrbitDB orbit, DateTime atDateTime)
        {


            if (UserelativeVelocity)
            {
                return InstantaneousOrbitalVelocityVector_AU(orbit, atDateTime);
            }
            else
            {
                return AbsoluteOrbitalVector_AU(orbit, atDateTime);
            }
        }
        
        /// <summary>
        /// Gets the orbital vector, will be either Absolute or relative depending on static bool UserelativeVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 GetOrbitalVector_m(OrbitDB orbit, DateTime atDateTime)
        {
            if (UserelativeVelocity)
            {
                return InstantaneousOrbitalVelocityVector_m(orbit, atDateTime);
            }
            else
            {
                return AbsoluteOrbitalVector_m(orbit, atDateTime);
            }
        }

        public static Vector3 GetOrbitalInsertionVector_m(Vector3 departureVelocity, OrbitDB targetOrbit, DateTime arrivalDateTime)
        {
            if (UserelativeVelocity)
                return departureVelocity;
            else
            {
                var targetVelocity = AbsoluteOrbitalVector_m(targetOrbit, arrivalDateTime);
                return departureVelocity - targetVelocity;
            }
        }

        /// <summary>
        /// The orbital vector.
        /// </summary>
        /// <returns>The orbital vector, relative to the root object (ie sun)</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 AbsoluteOrbitalVector_AU(OrbitDB orbit, DateTime atDateTime)       
        {
            Vector3 vector = InstantaneousOrbitalVelocityVector_AU(orbit, atDateTime);
            if(orbit.Parent != null)
                vector += AbsoluteOrbitalVector_AU((OrbitDB)orbit.ParentDB, atDateTime);
            return vector;

        }
        
        /// <summary>
        /// The orbital vector.
        /// </summary>
        /// <returns>The orbital vector, relative to the root object (ie sun)</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 AbsoluteOrbitalVector_m(OrbitDB orbit, DateTime atDateTime)       
        {
            Vector3 vector = InstantaneousOrbitalVelocityVector_m(orbit, atDateTime);
            if(orbit.Parent != null)
            {
                if (orbit is OrbitUpdateOftenDB)//this is a horrbile hack. very brittle. 
                    vector += AbsoluteOrbitalVector_m(orbit.Parent.GetDataBlob<OrbitDB>(), atDateTime);
                else
                    vector += AbsoluteOrbitalVector_m((OrbitDB)orbit.ParentDB, atDateTime);
            }
            return vector;

        }

        /// <summary>
        /// PreciseOrbital Velocy in polar coordinates
        /// 
        /// </summary>
        /// <returns>item1 is speed, item2 angle</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static (double speed, double heading) InstantaneousOrbitalVelocityPolarCoordinate(OrbitDB orbit, DateTime atDateTime)
        {
            var position = GetPosition_AU(orbit, atDateTime);
            var sma = orbit.SemiMajorAxis_AU;
            if (orbit.GravitationalParameter_Km3S2 == 0 || sma == 0)
                return (0,0); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameterAU;
            
            double e = orbit.Eccentricity;
            double trueAnomaly = GetTrueAnomaly(orbit, atDateTime);
            double aoP = orbit.ArgumentOfPeriapsis;
            
            (double speed,double heading) polar = OrbitMath.ObjectLocalVelocityPolar(sgp, position, sma, e, trueAnomaly, aoP);
            
            return polar;
            
        }

        /// <summary>
        /// Parent relative velocity vector. 
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 InstantaneousOrbitalVelocityVector_AU(OrbitDB orbit, DateTime atDateTime)
        {
            var position = GetPosition_AU(orbit, atDateTime);
            var sma = orbit.SemiMajorAxis_AU;
            if (orbit.GravitationalParameter_Km3S2 == 0 || sma == 0)
                return new Vector3(); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameterAU;
      
            double e = orbit.Eccentricity;
            double trueAnomaly = GetTrueAnomaly(orbit, atDateTime);
            double aoP = orbit.ArgumentOfPeriapsis;
            double i = orbit.Inclination;
            double loAN = orbit.LongitudeOfAscendingNode;
            return OrbitMath.ParentLocalVeclocityVector(sgp, position, sma, e, trueAnomaly, aoP, i, loAN);
        }
        
        /// <summary>
        /// Parent relative velocity vector. 
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 InstantaneousOrbitalVelocityVector_m(OrbitDB orbit, DateTime atDateTime)
        {
            var position = GetPosition_m(orbit, atDateTime);
            var sma = orbit.SemiMajorAxis;
            if (orbit.GravitationalParameter_Km3S2 == 0 || sma == 0)
                return new Vector3(); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameter_m3S2;
      
            double e = orbit.Eccentricity;
            double trueAnomaly = GetTrueAnomaly(orbit, atDateTime);
            double aoP = orbit.ArgumentOfPeriapsis;
            double i = orbit.Inclination;
            double loAN = orbit.LongitudeOfAscendingNode;
            return OrbitMath.ParentLocalVeclocityVector(sgp, position, sma, e, trueAnomaly, aoP, i, loAN);
        }
        

        /// <summary>
        /// Gets the SOI radius of a given body.
        /// </summary>
        /// <returns>The SOI radius in AU</returns>
        /// <param name="entity">Entity which has OrbitDB and MassVolumeDB</param>
        public static double GetSOI_AU(Entity entity)
        {
            var orbitDB = entity.GetDataBlob<OrbitDB>();
            if (orbitDB.Parent != null) //if we're not the parent star
            {
                var semiMajAxis = orbitDB.SemiMajorAxis_AU;

                var myMass = entity.GetDataBlob<MassVolumeDB>().MassDry;

                var parentMass = orbitDB.Parent.GetDataBlob<MassVolumeDB>().MassDry;

                return OrbitMath.GetSOI(semiMajAxis, myMass, parentMass);
            }
            else return double.MaxValue; //if we're the parent star, then soi is infinate. 
        }

        public static double GetSOI_m(Entity entity)
        {
            
            var orbitDB = entity.GetDataBlob<OrbitDB>();
            if (orbitDB.Parent != null) //if we're not the parent star
            {
                var semiMajAxis = orbitDB.SemiMajorAxis;

                var myMass = entity.GetDataBlob<MassVolumeDB>().MassDry;

                var parentMass = orbitDB.Parent.GetDataBlob<MassVolumeDB>().MassDry;

                return OrbitMath.GetSOI(semiMajAxis, myMass, parentMass);
            }
            else return double.PositiveInfinity; //if we're the parent star, then soi is infinate.
        }

        public static Entity FindSOIForPosition(StarSystem starSys, Vector3 AbsolutePosition)
        {
            var orbits = starSys.GetAllDataBlobsOfType<OrbitDB>();
            var withinSOIOf = new List<Entity>(); 
            foreach (var orbit in orbits)
            {
                var subOrbit = FindSOIForOrbit(orbit, AbsolutePosition);
                if(subOrbit != null)
                    withinSOIOf.Add(subOrbit.OwningEntity);
            }


            var closestDist = double.PositiveInfinity;
            Entity closestEntity = orbits[0].Root;
            foreach (var entity in withinSOIOf)
            {
                var pos = entity.GetDataBlob<PositionDB>().AbsolutePosition_m;
                var distance = (AbsolutePosition - pos).Length();
                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestEntity = entity;
                }

            }
            return closestEntity;
        }

        public static OrbitDB FindSOIForOrbit(OrbitDB orbit, Vector3 AbsolutePosition)
        {
            var soi = orbit.SOI_m;
            var pos = orbit.OwningEntity.GetDataBlob<PositionDB>();
            if (PositionDB.GetDistanceBetween_m(AbsolutePosition, pos) < soi)
            {
                foreach (OrbitDB subOrbit in orbit.ChildrenDBs)
                {
                    var suborbitb = FindSOIForOrbit(subOrbit, AbsolutePosition);
                    if (suborbitb != null)
                        return suborbitb;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates a cartisian position for an intercept for a ship and an target's orbit. 
        /// </summary>
        /// <returns>The intercept position and DateTime</returns>
        /// <param name="mover">The entity that is trying to intercept a target.</param>
        /// <param name="targetOrbit">Target orbit.</param>
        /// <param name="atDateTime">Datetime of transit start</param>
        public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition_m(Entity mover, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
        {
            Vector3 moverPos = mover.GetAbsoluteFuturePosition(atDateTime);
            double spd_m = mover.GetDataBlob<WarpAbilityDB>().MaxSpeed;
            return OrbitMath.GetInterceptPosition_m(moverPos, spd_m, targetOrbit, atDateTime, offsetPosition);
        }
        
        internal class OrbitProcessorException : Exception
        {
            public override string Message { get; }
            public Entity Entity { get; }

            public OrbitProcessorException(string message, Entity entity)
            {
                Message = message;
                Entity = entity;
            }
        }

        #endregion
    }



    public class OrbitUpdateOftenProcessor : IHotloopProcessor
    {
        private static readonly int OrbitTypeIndex = EntityManager.GetTypeIndex<OrbitUpdateOftenDB>();
        private static readonly int PositionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
        
        public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitUpdateOftenDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface. 
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>(OrbitTypeIndex);
            DateTime toDate = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(orbit, toDate);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var orbits = manager.GetAllDataBlobsOfType<OrbitUpdateOftenDB>(OrbitTypeIndex);
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var orbit in orbits)
            {
                UpdateOrbit(orbit, toDate);
            }
        }

        public static void UpdateOrbit(OrbitUpdateOftenDB entityOrbitDB, DateTime toDate)
        {
            
            PositionDB entityPosition = entityOrbitDB.OwningEntity.GetDataBlob<PositionDB>(PositionTypeIndex);
            try
            {
                Vector3 newPosition = OrbitProcessor.GetPosition_m(entityOrbitDB, toDate);
                entityPosition.RelativePosition_m = newPosition;
            }
            catch (OrbitProcessor.OrbitProcessorException e)
            {
                var entity = e.Entity;
                string name = "Un-Named";
                if (entity.HasDataBlob<NameDB>())
                    name = entity.GetDataBlob<NameDB>().OwnersName;
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.  
                Event evt = new Event(StaticRefLib.CurrentDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + name + " " + entity.Guid + " " + e.Message);
                evt.EventType = EventType.Opps;
                StaticRefLib.EventLog.AddEvent(evt);
            }
        }
    }
}