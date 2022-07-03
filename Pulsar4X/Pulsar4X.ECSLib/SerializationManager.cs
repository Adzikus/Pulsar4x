﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Pulsar4X.Orbital;
using Formatting = Newtonsoft.Json.Formatting;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class is responsible for saving a game to/from disk.
    /// </summary>
    // use: http://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
    public static class SerializationManager
    {
        /// <summary>
        /// Game class of the game that is currently saving/loading. It is garunteed to be the loading/saving game from
        /// the time the operation starts, until AFTER any events are fired.
        /// </summary>
        internal static IProgress<double> Progress { get; private set; }
        internal static int ManagersProcessed { get; set; }
        private static readonly object SyncRoot = new object();
        private static readonly JsonSerializer PersistenceSerializer = new JsonSerializer { 
            Context = new StreamingContext(StreamingContextStates.Persistence), 
            NullValueHandling = NullValueHandling.Ignore, 
            Formatting = Formatting.Indented, 
            ContractResolver = new ForceUseISerializable(), 
            PreserveReferencesHandling = PreserveReferencesHandling.None 
        };
        private static readonly JsonSerializer RemoteSerializer = new JsonSerializer {
            Context = new StreamingContext(StreamingContextStates.Remoting), 
            NullValueHandling = NullValueHandling.Ignore, 
            Formatting = Formatting.None, 
            ContractResolver = new ForceUseISerializable(), 
            PreserveReferencesHandling = PreserveReferencesHandling.None
        };

        public static string Export([NotNull] Game game, bool compress = false) => Export<Game>(game, game, compress);
        public static string Export([NotNull] Game game, [NotNull] Entity entity, bool compress = false) => Export<ProtoEntity>(game, entity.Clone(), compress);
        public static string Export([NotNull] Game game, [NotNull] ProtoEntity entity, bool compress = false) => Export<ProtoEntity>(game, entity, compress);
        public static string Export([NotNull] Game game, [NotNull] BaseDataBlob datablob, bool compress = false) => Export<BaseDataBlob>(game, datablob, compress);
        public static string Export([NotNull] Game game, [NotNull] StarSystem system, bool compress = false) => Export<StarSystem>(game, system, compress);
        public static string Export([NotNull] Game game, [NotNull] EventLog eventLog, bool compress = false) => Export<EventLog>(game, eventLog, compress);
        private static string Export<TObj>([NotNull] Game game, [NotNull] TObj obj, bool compress = false)
        {
            using (var stream = new MemoryStream())
            {
                ExportInternal(game, stream, obj, compress);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void Export([NotNull] Game game, [NotNull] string filePath, bool compress = false) => ExportFile(game, filePath, game, compress);
        public static void Export([NotNull] Game game, [NotNull] string filePath, [NotNull] Entity entity, bool compress = false) => ExportFile(game, filePath, entity.Clone(), compress);
        public static void Export([NotNull] Game game, [NotNull] string filePath, [NotNull] ProtoEntity entity, bool compress = false) => ExportFile(game, filePath, entity, compress);
        public static void Export([NotNull] Game game, [NotNull] string filePath, [NotNull] StarSystem system, bool compress = false) => ExportFile(game, filePath, system, compress);
        public static void Export([NotNull] Game game, [NotNull] string filePath, [NotNull] EventLog eventLog, bool compress = false) => ExportFile(game, filePath, eventLog, compress);
        private static void ExportFile<TObj>([NotNull] Game game, [NotNull] string filePath, [NotNull] TObj obj, bool compress = false)
        {
            using (var fileStream = GetFileStream(filePath, FileAccess.Write))
            {
                ExportInternal(game, fileStream, obj, compress);
            }
        }

        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, bool compress = false) => ExportInternal(game, outputStream, game, compress);
        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] Entity entity, bool compress = false) => ExportInternal(game, outputStream, entity.Clone(), compress);
        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] ProtoEntity entity, bool compress = false) => ExportInternal(game, outputStream, entity, compress);
        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] BaseDataBlob datablob, bool compress = false) => ExportInternal(game, outputStream, datablob, compress);
        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] StarSystem system, bool compress = false) => ExportInternal(game, outputStream, system, compress);
        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] EventLog eventLog, bool compress = false) => ExportInternal(game, outputStream, eventLog, compress);
        public static void Export([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] EntityCommand entityCommand, bool compress = false) => ExportInternal(game, outputStream, entityCommand, compress);
        private static void ExportInternal<TObj>([NotNull] Game game, [NotNull] Stream outputStream, [NotNull] TObj obj, bool compress = false)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        lock (SyncRoot)
                        {
                            PersistenceSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;
                            PersistenceSerializer.Context = new StreamingContext(PersistenceSerializer.Context.State, game);
                            PersistenceSerializer.Serialize(writer, obj, typeof(TObj));
                        }
                    }
                }
                FinalizeOutput(outputStream, intermediateStream, compress);
            }
        }


        /// <summary>
        /// saves an entity to a given stream
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="outputStream"></param>
        /// <param name="progress"></param>
        /// <param name="compress"></param>

        public static void Export([NotNull] GameSettings settings, [NotNull] Stream outputStream, bool compress = false)
        {
            JsonSerializer serialiser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = compress ? Formatting.None : Formatting.Indented,
                ContractResolver = new ForceUseISerializable()
            };

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        lock (SyncRoot)
                        {
                            serialiser.Serialize(writer, settings, typeof(GameSettings));
                        }
                    }
                }
                FinalizeOutput(outputStream, intermediateStream, compress);
            }
        }

        public static void Export(Dictionary<DateTime, List<string>> procDict, Stream outputStream, bool compress = false)
        { 
            JsonSerializer serialiser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = compress ? Formatting.None : Formatting.Indented,
                ContractResolver = new ForceUseISerializable()
            };

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (var writer = new JsonTextWriter(streamWriter))
                    {
                        lock (SyncRoot)
                        {
                            serialiser.Serialize(writer, procDict, typeof(Dictionary<DateTime, List<string>>));
                        }
                    }
                }
                FinalizeOutput(outputStream, intermediateStream, compress);
            }
        }

        public static Dictionary<DateTime, List<string>> ImportInstanceProcessorDict(Stream inputStream)
        { 
            JsonSerializer serialiser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new ForceUseISerializable()
            };
            Dictionary<DateTime, List<string>> procDict;
            // Use a BufferedStream to allow reading and seeking from any stream.
            // Example: If inputStream is a NetworkStream, then we can only read once.
            using (BufferedStream inputBuffer = new BufferedStream(inputStream))
            {
                // Check if our stream is compressed.
                if (HasGZipHeader(inputBuffer))
                {
                    // File is compressed. Decompress using GZip.
                    using (GZipStream compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        // Decompress into a MemoryStream.
                        using (MemoryStream intermediateStream = new MemoryStream())
                        {
                            // Decompress the file into an intermediate MemoryStream.
                            compressionStream.CopyTo(intermediateStream);

                            // Reset the position of the MemoryStream so it can be read from the beginning.
                            intermediateStream.Position = 0;

                            // Populate the game from the uncompressed MemoryStream.
                            //PopulateEntity(entity, intermediateStream);
                            procDict = populateProcDict(intermediateStream);

                        }
                    }
                }
                else
                {
                    // Populate the game from the uncompressed inputStream.
                    procDict = populateProcDict(inputStream);
                }
            }
            return procDict;
        }

        private static Dictionary<DateTime, List<string>> populateProcDict(Stream inputStream)
        {
            
            JsonSerializer serialiser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new ForceUseISerializable()
            };
            using (StreamReader sr = new StreamReader(inputStream))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    Dictionary<DateTime, List<string>> procDict = new Dictionary<DateTime, List<string>>();
                    procDict = PersistenceSerializer.Deserialize<Dictionary<DateTime, List<string>>>(reader);
                    //serialiser.Populate(reader, procDict);

                    return procDict;
                }
            }
        }

        public static GameSettings ImportGameSettings(Stream inputStream)
        {
            JsonSerializer serialiser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new ForceUseISerializable()
            };
            GameSettings gameSettings;
            // Use a BufferedStream to allow reading and seeking from any stream.
            // Example: If inputStream is a NetworkStream, then we can only read once.
            using (BufferedStream inputBuffer = new BufferedStream(inputStream))
            {
                // Check if our stream is compressed.
                if (HasGZipHeader(inputBuffer))
                {
                    // File is compressed. Decompress using GZip.
                    using (GZipStream compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        // Decompress into a MemoryStream.
                        using (MemoryStream intermediateStream = new MemoryStream())
                        {
                            // Decompress the file into an intermediate MemoryStream.
                            compressionStream.CopyTo(intermediateStream);

                            // Reset the position of the MemoryStream so it can be read from the beginning.
                            intermediateStream.Position = 0;

                            // Populate the game from the uncompressed MemoryStream.
                            //PopulateEntity(entity, intermediateStream);
                            gameSettings = populateGameSettings(intermediateStream);

                        }
                    }
                }
                else
                {
                    // Populate the game from the uncompressed inputStream.
                    gameSettings = populateGameSettings(inputStream);
                }
            }
            return gameSettings;
        }

        private static GameSettings populateGameSettings(Stream inputStream)
        {
            JsonSerializer serialiser = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new ForceUseISerializable()
            };
            using (StreamReader sr = new StreamReader(inputStream))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    GameSettings settings = new GameSettings();
                    serialiser.Populate(reader, settings);
   
                    return settings;
                }
            }
        }


        public static Game ImportGameJson(string jsonString)
        {
            using (MemoryStream stream = GetMemoryStream(jsonString))
            {
                return ImportGame(stream);
            }
        }
        public static Entity ImportEntityJson([NotNull] Game game, string jsonString, [NotNull] EntityManager manager)
        {
            using (MemoryStream stream = GetMemoryStream(jsonString))
            {
                return ImportEntity(game, stream, manager);
            }
            
        }
        public static StarSystem ImportSystemJson([NotNull] Game game, string jsonString)
        {
            using (MemoryStream stream = GetMemoryStream(jsonString))
            {
                return ImportSystem(game, stream);
            }
        }
        public static EventLog ImportEventLogJson([NotNull] Game game, string jsonString)
        {
            using (MemoryStream stream = GetMemoryStream(jsonString))
            {
                return ImportEventLog(game, stream);
            }
        }

        public static Game ImportGame([NotNull] string filePath)
        {
            using (FileStream fs = GetFileStream(filePath, FileAccess.Read))
            {
                return ImportGame(fs);
            }
        }
        public static Entity ImportEntity([NotNull] Game game, string filePath, [NotNull] EntityManager manager)
        {
            using (FileStream fs = GetFileStream(filePath, FileAccess.Read))
            {
                return ImportEntity(game, fs, manager);
            }
        }
        public static StarSystem ImportSystem([NotNull] Game game, string filePath)
        {
            using (FileStream fs = GetFileStream(filePath, FileAccess.Read))
            {
                return ImportSystem(game, fs);
            }
        }
        public static EventLog ImportEventLog([NotNull] Game game, string filePath)
        {
            using (FileStream fs = GetFileStream(filePath, FileAccess.Read))
            {
                return ImportEventLog(game, fs);
            }
        }

        public static Game ImportGame([NotNull] Stream inputStream)
        {
            var game = new Game();
            return Import(game, inputStream, game);
        }
        public static Entity ImportEntity([NotNull] Game game, [NotNull] Stream inputStream, [NotNull] EntityManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }


            var protoEntity = new ProtoEntity(); 
            protoEntity = Import(game, inputStream, protoEntity);

            //the block of code below is a somewhat hacky way of fixing a problem where an entity has a datablob that refers back to the entity. 
            //eg a faction entitiy with a NameDB. in such cases the Import above creates an empty entity because of the NameDB's reference. 
            //then Entity.Create below checks for an exsisting entity guid, finds it, then returns an entity with a new GUID. 
            //this then gives us two entities, one with the correct guid but with no datablobs, and a second one with a new different guid and all the datablobs. 
            //checking if the datablob count is 0 is a poor and not guarenteed way of seeing if the entity hasn't been created some other way previously.
            //I'm wondering if we shouldn't throw an exception if we try to add an entity with the same guid, instead of just changing the guid. 
            Entity entity;
            if (manager.FindEntityByGuid(protoEntity.Guid, out entity) && entity.DataBlobs.Count == 0) 
                manager.RemoveEntity(entity);



            //TODO: #Seralisation we may need to find the entity owner from the json and put that in the second parameter.
            entity = Entity.Create(manager, Guid.Empty, protoEntity);
            game.PostGameLoad();
            return entity;
        }

        public static BaseDataBlob ImportDatablob([NotNull] Game game, Entity entity, Type type, Stream inputStream)
        {
            BaseDataBlob datablob = (BaseDataBlob)Activator.CreateInstance(type);
            datablob = Import<BaseDataBlob>(game, inputStream, datablob);
            return datablob;
        }

        public static EntityCommand ImportEntityCommand([NotNull] Game game, Type type, Stream inputStream)
        {
            EntityCommand command = (EntityCommand)Activator.CreateInstance(type);
            command = Import<EntityCommand>(game, inputStream, command);
            return command;
        }

        public static StarSystem ImportSystem([NotNull] Game game, [NotNull] Stream inputStream)
        {
            var system = new StarSystem();
            system = Import(game, inputStream, system);
            game.PostGameLoad();
            return system;
        }
        public static EventLog ImportEventLog([NotNull] Game game, [NotNull] Stream inputStream)
        {
            EventLog eventLog = new EventLog(game);
            eventLog = Import(game, inputStream, eventLog);
            StaticRefLib.SetEventlog(eventLog);
            return eventLog;
        }

        public static ShipDesign ImportDesign(Game game, Stream inputStream)
        {
            ShipDesign design = new ShipDesign();
            design = Import(game, inputStream, design);
            return design;
        }

        private static TObj Import<TObj>([NotNull] Game game, [NotNull] Stream inputStream, [NotNull] TObj obj)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            // Use a BufferedStream to allow reading and seeking from any stream.
            // Example: If inputStream is a NetworkStream, then we can only read once.
            using (var inputBuffer = new BufferedStream(inputStream))
            {
                // Check if our stream is compressed.
                if (HasGZipHeader(inputBuffer))
                {
                    // File is compressed. Decompress using GZip.
                    using (var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        // Decompress into a MemoryStream.
                        using (var intermediateStream = new MemoryStream())
                        {
                            // Decompress the file into an intermediate MemoryStream.
                            compressionStream.CopyTo(intermediateStream);

                            // Reset the position of the MemoryStream so it can be read from the beginning.
                            intermediateStream.Position = 0;

                            // Populate the object from the uncompressed MemoryStream.
                            obj = PopulateObject(game, intermediateStream, obj);
                        }
                    }
                }
                else
                {
                    // Populate the game from the uncompressed inputStream.
                    obj = PopulateObject(game, inputBuffer, obj);
                }
            }
            return obj;
        }



        #region StarSystem Serialization/Deserialization

        public static void ExportStarSystemsToXML(Game game)
        {
            var ser = new XmlSerializer(typeof(XmlNode));
            var writer = new StreamWriter(Path.Combine(GetWorkingDirectory(), "SystemsExport.xml"));

            var xmlDoc = new XmlDocument();
            XmlNode toplevelNode = xmlDoc.CreateNode(XmlNodeType.Element, "Systems", "NS");

            foreach (KeyValuePair<Guid, StarSystem> kvp in game.Systems)
            {
                StarSystem system = kvp.Value;
                var rootStar = system.GetFirstEntityWithDataBlob<OrbitDB>();

                // get root star:
                var orbitDB = rootStar.GetDataBlob<OrbitDB>();
                rootStar = orbitDB.Root;

                XmlNode systemNode = xmlDoc.CreateNode(XmlNodeType.Element, "System", "NS");

                // the following we serialize the body to xml, and will do the same for all child bodies:
                SerializeBodyToXML(xmlDoc, systemNode, rootStar, orbitDB);

                // add xml to to level node:
                toplevelNode.AppendChild(systemNode);
            }

            // save xml to file:
            ser.Serialize(writer, toplevelNode);
            writer.Close();
        }

        private static void SerializeBodyToXML(XmlDocument xmlDoc, XmlNode systemNode, Entity systemBody, OrbitDB orbit)
        {
            // get the datablobs:
            var systemBodyDB = systemBody.GetDataBlob<SystemBodyInfoDB>();
            var starIfnoDB = systemBody.GetDataBlob<StarInfoDB>();
            var positionDB = systemBody.GetDataBlob<PositionDB>();
            var massVolumeDB = systemBody.GetDataBlob<MassVolumeDB>();
            var nameDB = systemBody.GetDataBlob<NameDB>();
            var atmosphereDB = systemBody.GetDataBlob<AtmosphereDB>();
            var ruinsDB = systemBody.GetDataBlob<RuinsDB>();

            // create the body node:
            XmlNode bodyNode = xmlDoc.CreateNode(XmlNodeType.Element, "Body", "NS");

            // save parent id first:
            XmlNode varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ParentID", "NS");
            if (orbit.Parent != null)
                varNode.InnerText = orbit.Parent.Guid.ToString();
            else
                varNode.InnerText = Guid.Empty.ToString();
            bodyNode.AppendChild(varNode);

            // then add our ID to at the end:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "ID", "NS");
            varNode.InnerText = systemBody.Guid.ToString();
            bodyNode.AppendChild(varNode);

            if (nameDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Name", "NS");
                varNode.InnerText = nameDB.DefaultName;
                bodyNode.AppendChild(varNode);
            }

            if (starIfnoDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = starIfnoDB.SpectralType.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Class", "NS");
                varNode.InnerText = starIfnoDB.Class;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Age", "NS");
                varNode.InnerText = starIfnoDB.Age.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AverageEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.EcoSphereRadius_AU.ToString("N3");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MinEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.MinHabitableRadius_AU.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MaxEcoSphereRadius", "NS");
                varNode.InnerText = starIfnoDB.MaxHabitableRadius_AU.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Luminosity", "NS");
                varNode.InnerText = starIfnoDB.Luminosity.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = starIfnoDB.Temperature.ToString("N0");
                bodyNode.AppendChild(varNode);
            }

            if (massVolumeDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MassInKG", "NS");
                varNode.InnerText = massVolumeDB.MassDry.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MassInEarthMasses", "NS");
                varNode.InnerText = (massVolumeDB.MassDry / UniversalConstants.Units.EarthMassInKG).ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Density", "NS");
                varNode.InnerText = massVolumeDB.DensityDry_gcm.ToString("N4");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Radius", "NS");
                varNode.InnerText = massVolumeDB.RadiusInKM.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Volume_km3", "NS");
                varNode.InnerText = massVolumeDB.Volume_km3.ToString("N0");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceGravity", "NS");
                varNode.InnerText = massVolumeDB.SurfaceGravity.ToString("N4");
                bodyNode.AppendChild(varNode);
            }

            // add orbit details:
            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SemiMajorAxis", "NS");
            varNode.InnerText = orbit.SemiMajorAxis_AU.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Apoapsis", "NS");
            varNode.InnerText = orbit.Apoapsis_AU.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Periapsis", "NS");
            varNode.InnerText = orbit.Periapsis_AU.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Year", "NS");
            varNode.InnerText = orbit.OrbitalPeriod.ToString("dd\\:hh\\:mm");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Eccentricity", "NS");
            varNode.InnerText = orbit.Eccentricity.ToString("N3");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Inclination", "NS");
            varNode.InnerText = orbit.Inclination_Degrees.ToString("N2");
            bodyNode.AppendChild(varNode);

            varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Children", "NS");
            varNode.InnerText = orbit.Children.Count.ToString();
            bodyNode.AppendChild(varNode);

            if (positionDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PositionInAU", "NS");
                varNode.InnerText = "(" + positionDB.X_AU.ToString("N3") + ", " + positionDB.Y_AU.ToString("N3") + ", " + positionDB.Z_AU.ToString("N3") + ")";
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "PositionInKm", "NS");
                varNode.InnerText = "(" + positionDB.XInKm.ToString("N3") + ", " + positionDB.YInKm.ToString("N3") + ", " + positionDB.ZInKm.ToString("N3") + ")";
                bodyNode.AppendChild(varNode);
            }

            if (systemBodyDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Type", "NS");
                varNode.InnerText = systemBodyDB.BodyType.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AxialTilt", "NS");
                varNode.InnerText = systemBodyDB.AxialTilt.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Temperature", "NS");
                varNode.InnerText = systemBodyDB.BaseTemperature.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "LengthOfDay", "NS");
                varNode.InnerText = systemBodyDB.LengthOfDay.ToString("g");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "MagneticField", "NS");
                varNode.InnerText = systemBodyDB.MagneticField.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Tectonics", "NS");
                varNode.InnerText = systemBodyDB.Tectonics.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (atmosphereDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Atmosphere", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionInPercent;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "AtmosphereInATM", "NS");
                varNode.InnerText = atmosphereDB.AtomsphereDescriptionAtm;
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Pressure", "NS");
                varNode.InnerText = atmosphereDB.Pressure.ToString("N2");
                bodyNode.AppendChild(varNode);

                //varNode = xmlDoc.CreateNode(XmlNodeType.Element, "Albedo", "NS");
                //varNode.InnerText = atmosphereDB.Albedo.ToString("p1");
                //bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "SurfaceTemperature", "NS");
                varNode.InnerText = atmosphereDB.SurfaceTemperature.ToString("N1");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhouseFactor", "NS");
                varNode.InnerText = atmosphereDB.GreenhouseFactor.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "GreenhousePressure", "NS");
                varNode.InnerText = atmosphereDB.GreenhousePressure.ToString("N2");
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HasHydrosphere", "NS");
                varNode.InnerText = atmosphereDB.Hydrosphere.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "HydrosphereExtent", "NS");
                varNode.InnerText = atmosphereDB.HydrosphereExtent.ToString();
                bodyNode.AppendChild(varNode);
            }

            if (ruinsDB != null)
            {
                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinCount", "NS");
                varNode.InnerText = ruinsDB.RuinCount.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinQuality", "NS");
                varNode.InnerText = ruinsDB.RuinQuality.ToString();
                bodyNode.AppendChild(varNode);

                varNode = xmlDoc.CreateNode(XmlNodeType.Element, "RuinSize", "NS");
                varNode.InnerText = ruinsDB.RuinSize.ToString();
                bodyNode.AppendChild(varNode);
            }

            // add body node to system node:
            systemNode.AppendChild(bodyNode);

            // call recursively for children:
            foreach (var child in orbit.Children)
            {
                OrbitDB o = child.GetDataBlob<OrbitDB>();
                if (o != null)
                    SerializeBodyToXML(xmlDoc, systemNode, o.OwningEntity, o);
            }
        }

        #endregion

        private static TObj PopulateObject<TObj>(Game game, Stream inputStream, TObj obj)
        {
            using (var sr = new StreamReader(inputStream))
            {
                using (var reader = new JsonTextReader(sr))
                {
                    lock (SyncRoot)
                    {
                        PersistenceSerializer.Context = new StreamingContext(PersistenceSerializer.Context.State, game);
                        if (typeof(TObj) == typeof(ProtoEntity) || typeof(TObj) == typeof(StarSystem))
                        {
                            obj = PersistenceSerializer.Deserialize<TObj>(reader);
                            //at this point in the code, if the entity has a datablob which references entities, those entites will be created in the manager. 
                        }
                        else
                        {
                            PersistenceSerializer.Populate(reader, obj);
                        }
                    }
                }
            }
            return obj;
        }

        /// <summary>
        /// Finalizes the outputStream by applying compression.
        /// </summary>
        private static void FinalizeOutput(Stream outputStream, Stream intermediateStream, bool compress)
        {
            intermediateStream.Position = 0;

            if (compress)
            {
                using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    intermediateStream.CopyTo(compressionStream);
                }
            }
            else
            {
                intermediateStream.CopyTo(outputStream);
            }
        }

        private static FileStream GetFileStream(string filePath, FileAccess requiredAccess)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Argument is null or empty", nameof(filePath));
            }

            string fullPath = Path.GetFullPath(filePath);
            string workingDirectory = GetWorkingDirectory();
            if (Path.GetDirectoryName(fullPath) != workingDirectory)
            {
                filePath = Path.Combine(workingDirectory, Path.GetFileName(filePath));
            }

            CheckFile(filePath, requiredAccess);
            if (requiredAccess == FileAccess.Read)
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            return new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
        }

        private static MemoryStream GetMemoryStream(string jsonString)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.Write(jsonString);
                writer.Flush();

                stream.Position = 0;
                return stream;
            }
        }

        /// <summary>
        /// Check if we have a valid file. Will throw exceptions if there's issues with the file.
        /// </summary>
        private static void CheckFile(string filePath, FileAccess fileAccess)
        {
            // Test writing the file. If there's any issues with the file, this will cause them to throw.
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "No valid file path provided.");
            }

            if ((fileAccess & FileAccess.Write) != 0)
            {
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("Pulsar4X write text.");
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            if ((fileAccess & FileAccess.Read) != 0)
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.ReadByte();
                }
            }
        }

        /// <summary>
        /// Checks the stream for compression by looking for GZip header numbers.
        /// </summary>
        [PublicAPI]
        public static bool HasGZipHeader(BufferedStream inputStream)
        {
            var headerBytes = new byte[2];

            int numBytes = inputStream.Read(headerBytes, 0, 2);
            inputStream.Position = 0;
            if (numBytes != 2)
            {
                return false;
            }
            // First two bytes should be 31 and 139 according to the GZip file format.
            // http://www.gzip.org/zlib/rfc-gzip.html#header-trailer
            return headerBytes[0] == 31 && headerBytes[1] == 139;
        }

        internal static string GetWorkingDirectory()
        {
            // get list of default sub-directories:
            string workingDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath);
            if (workingDirectory == null)
            {
                throw new DirectoryNotFoundException("SerializationManager could not find/access the executable's directory.");
            }
            return workingDirectory;
        }
    }
}
