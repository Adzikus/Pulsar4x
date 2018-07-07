﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class JPFactory
    {
        public static Entity CreateJumpPoint(StarSystemFactory ssf, StarSystem system)
        {
            var primaryStarInfoDB = system.GetFirstEntityWithDataBlob<StarInfoDB>().GetDataBlob<OrbitDB>().Root.GetDataBlob<StarInfoDB>();

            NameDB jpNameDB = new NameDB("Jump Point");
            PositionDB jpPositionDB = new PositionDB(0,0,0, system.Guid);
            TransitableDB jpTransitableDB = new TransitableDB();

            jpTransitableDB.IsStabilized = system.Game.Settings.AllJumpPointsStabilized ?? false;

            if (!jpTransitableDB.IsStabilized)
            {
                // TODO: Introduce a random chance to stablize jumppoints.
            }
            
            var jpPositionLimits = new MinMaxStruct(ssf.GalaxyGen.Settings.OrbitalDistanceByStarSpectralType[primaryStarInfoDB.SpectralType].Min, ssf.GalaxyGen.Settings.OrbitalDistanceByStarSpectralType[primaryStarInfoDB.SpectralType].Max);

            jpPositionDB.X = GMath.SelectFromRange(jpPositionLimits, system.RNG.NextDouble());
            jpPositionDB.Y = GMath.SelectFromRange(jpPositionLimits, system.RNG.NextDouble());

            // Randomly flip the position sign to allow negative values.
            if (system.RNG.Next(0, 100) < 50)
            {
                jpPositionDB.X = 0 - jpPositionDB.X;
            }
            if (system.RNG.Next(0, 100) < 50)
            {
                jpPositionDB.Y = 0 - jpPositionDB.Y;
            }

            var dataBlobs = new List<BaseDataBlob> { jpNameDB, jpTransitableDB, jpPositionDB};

            Entity jumpPoint = Entity.Create(system, Guid.Empty, dataBlobs);
            return jumpPoint;
        }

        /// <summary>
        /// Gets the number of jumppoints that should generated for a system.
        /// Based on Aurora 7.0 mechanics as described here: 
        /// http://aurora2.pentarch.org/index.php?topic=7255.msg80028#msg80028
        /// </summary>
        public static int GetNumJPForSystem(StarSystem system)
        {
            Entity primaryStar = system.GetFirstEntityWithDataBlob<StarInfoDB>().GetDataBlob<OrbitDB>().Root;
            var starMVDB = primaryStar.GetDataBlob<MassVolumeDB>();

            int numJumpPoints = 0;
            int baseJPChance = 90;

            Random RNG = new Random();

            double jpChance;
            int random;
            do
            {
                numJumpPoints++;

                jpChance = baseJPChance + (starMVDB.Mass / GameConstants.Units.SolarMassInKG);

                if (jpChance > 90)
                {
                    jpChance = 90;
                }

                if (baseJPChance == 90)
                {
                    baseJPChance = 60;
                }
                else if (baseJPChance == 60)
                {
                    baseJPChance = 30;
                }

                random = RNG.Next(0, 100);
            } while (jpChance > random);

            return numJumpPoints;
        }

        /// <summary>
        /// Generates jump points for this system.
        /// </summary>
        public static void GenerateJumpPoints(StarSystemFactory ssf, StarSystem system)
        {
            int numJumpPoints = GetNumJPForSystem(system);

            while (numJumpPoints > 0)
            {
                numJumpPoints--;

                CreateJumpPoint(ssf, system);
            }
        }


        private static void CreateConnection(Game game, Entity jumpPoint)
        {
            var jpTransitableDB = jumpPoint.GetDataBlob<TransitableDB>();
            var jpPositionDB = jumpPoint.GetDataBlob<PositionDB>();

            StarSystem system = game.Systems[jpPositionDB.SystemGuid];
            int systemIndex = system.SystemIndex;
        }

        private static void LinkJumpPoints(Entity JP1, Entity JP2)
        {
            var jp1TransitableDB = JP1.GetDataBlob<TransitableDB>();
            var jp2TransitableDB = JP2.GetDataBlob<TransitableDB>();

            jp1TransitableDB.Destination = JP2;
            jp2TransitableDB.Destination = JP1;
        }
    }
}
