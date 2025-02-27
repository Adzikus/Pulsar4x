﻿using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Pulsar4X.Tests
{
    [TestFixture, Description("Tests the static data import/export/manager/store")]
    public class StaticDataTests
    {
        [Test]
        public void TestExport()
        {
            WeightedList<AtmosphericGasSD> atmoGases = new WeightedList<AtmosphericGasSD>();
            AtmosphericGasSD gas = new AtmosphericGasSD();
            gas.BoilingPoint = 100;
            gas.MeltingPoint = 0;
            gas.ChemicalSymbol = "H20";
            gas.Name = "Water";
            gas.IsToxic = false;

            atmoGases.Add(1.0, gas);

            gas.BoilingPoint = 100;
            gas.MeltingPoint = 0;
            gas.ChemicalSymbol = "H2O";
            gas.Name = "Water Second take";
            gas.IsToxic = false;

            atmoGases.Add(1.0, gas);

            StaticDataManager.ExportStaticData(atmoGases, "AtmoGasesExportTest.json");

            StaticDataManager.ExportStaticData(VersionInfo.PulsarVersionInfo, "VersionInfoExportTest.vinfo");

            List<MineralSD> minList = new List<MineralSD>();
            MineralSD min = new MineralSD();
            min.Abundance = new Dictionary<BodyType, double>();
            min.Abundance.Add(BodyType.Asteroid, 0.01);
            min.Abundance.Add(BodyType.Comet, 0.05);
            min.Abundance.Add(BodyType.DwarfPlanet, 0.075);
            min.Abundance.Add(BodyType.GasDwarf, 0.1);
            min.Abundance.Add(BodyType.GasGiant, 1.0);
            min.Abundance.Add(BodyType.IceGiant, 0.5);
            min.Abundance.Add(BodyType.Moon, 0.5);
            min.Abundance.Add(BodyType.Terrestrial, 1.0);
            min.ID = Guid.NewGuid();
            min.Name = "Sorium";
            min.Description = "des";
            minList.Add(min);

            StaticDataManager.ExportStaticData(minList, "MineralsExportTest.json");

            //Dictionary<ID, TechSD> techs = Tech();
            //TechSD tech1 = new TechSD();
            //tech1.Name = "Trans-Newtonian Technology";
            //tech1.Requirements = new Dictionary<ID, int>();
            //tech1.Description = "Unlocks almost all other technology.";
            //tech1.Cost = 1000;
            //tech1.Category = ResearchCategories.ConstructionProduction;
            //tech1.ID = ID.NewGuid();

            //TechSD tech2 = new TechSD();
            //tech2.Name = "Construction Rate";
            //tech2.Requirements = new Dictionary<ID, int>();
            //tech2.Requirements.Add(tech1.ID, 0);
            //tech2.Description = "Boosts Construction Rate by 12 BP";
            //tech2.Cost = 3000;
            //tech2.Category = ResearchCategories.ConstructionProduction;
            //tech2.ID = ID.NewGuid();

            //techs.Add(tech1.ID, tech1);
            //techs.Add(tech2.ID, tech2);






            //StaticDataManager.ExportStaticData(techs, "./TechnologyDataExportTest.json");

            //InstallationSD install = new InstallationSD();
            //install.Name = "Mine";
            //install.Description = "Employs population to mine transnewtonian resources.";
            //install.PopulationRequired = 1;
            //install.CargoSize = 1;
            //install.BaseAbilityAmounts = new Dictionary<AbilityType, int>();
            //install.BaseAbilityAmounts.Add(AbilityType.Mine, 1);
            //install.TechRequirements = new List<ID>();
            //install.TechRequirements.Add(tech1.ID); //use trans-newtonian techology you just added to the tech list
            //install.ResourceCosts = new Dictionary<ID, int>();
            //install.ResourceCosts.Add(min.ID,60); //use Sorium that you just added to the mineral list
            //install.WealthCost = 120;
            //install.BuildPoints = 120;

            //installations.Add(install);



            //ComponentAbilitySD launchAbility = new ComponentAbilitySD();
            //launchAbility.Ability = AbilityType.LaunchMissileSize;
            //launchAbility.AbilityAmount = new List<float>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            //launchAbility.CrewAmount = new List<float>() { 3, 6, 9, 12, 15, 18, 21, 24, 27, 30};
            ////launchAbility.ID = ID.NewGuid();
            //launchAbility.Name = "Missile Launcher Size";
            //launchAbility.Description = "Can fire a missile of this size or smaller";
            //launchAbility.WeightAmount = new List<float>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            //launchAbility.TechRequirements = new List<ID>() { };

            //ComponentAbilitySD reloadAbility = new ComponentAbilitySD();
            //reloadAbility.Name = "Missile Launcher Reload Rate";
            //reloadAbility.Description = "Speed at which this launcher can reload from a magazine";
            ////reloadAbility.ID = ID.NewGuid();
            //reloadAbility.Ability = AbilityType.ReloadRateFromMag;
            //reloadAbility.AbilityAmount = new List<float>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            //reloadAbility.CrewAmount = new List<float>() { };

            //ComponentSD missileLauncher = new ComponentSD();
            //missileLauncher.ComponentAtbSDs = new List<ComponentAbilitySD>() { launchAbility, reloadAbility};
            //missileLauncher.Name = "MissileLauncher";
            //missileLauncher.Description = "Can launch Missiles and be reloaded via a magazine";
            //missileLauncher.ID = ID.NewGuid();

            ////StaticDataManager.ExportStaticData(launchAbility, "./launcherabilitytest.json");
            ////StaticDataManager.ExportStaticData(reloadAbility, "./launcherabilitytest.json");
            //Dictionary<ID, ComponentSD> components = new Dictionary<ID, ComponentSD>();
            //components.Add(missileLauncher.ID, missileLauncher);
            //StaticDataManager.ExportStaticData(components, "./Componentstest.json");

            // test export of galaxy settings:
            GalaxyFactory gf = new GalaxyFactory(true, 1);
            StaticDataManager.ExportStaticData(gf.Settings, "SystemGenSettings.json");
        }

        [Test]
        public void TestCargoType()
        {
            Dictionary<Guid, CargoTypeSD> cargoTypes = new Dictionary<Guid, CargoTypeSD>();
            CargoTypeSD cargoTypeGeneral = new CargoTypeSD()
            {
                ID = new Guid("16B4C4F0-7292-4F4D-8FEA-22103C70B288"),
                Name = "General",
                Description = "Storage for general cargo items"
            };
            cargoTypes.Add(cargoTypeGeneral.ID, cargoTypeGeneral);

            CargoTypeSD cargoTypeFuel = new CargoTypeSD()
            {
                ID = new Guid("D8E8DA2D-8DC8-4A3F-B989-5F2E67C55E77"),
                Name = "Fuel",
                Description = "Storage for fuel"
            };
            cargoTypes.Add(cargoTypeFuel.ID, cargoTypeFuel);

            CargoTypeSD cargoTypePopulation = new CargoTypeSD()
            {
                ID = new Guid("9E52A3AF-66AF-4935-982D-26F3FEE775B0"),
                Name = "Cryogenic Storage",
                Description = "Storage for frozen people"
            };
            cargoTypes.Add(cargoTypePopulation.ID, cargoTypePopulation);

            StaticDataManager.ExportStaticData(cargoTypes, "CargoTypeDataExportTest.json");
        }

        [Test]
        public void TestRefinedMatsSave()
        {
            Dictionary<Guid, ProcessedMaterialSD> mats = new Dictionary<Guid, ProcessedMaterialSD>();

            ProcessedMaterialSD soriumFuel = new ProcessedMaterialSD()
            {
                Name = "Sorium Fuel",
                Description = "Fuel for SpaceShips",
                ID = new Guid("33E6AC88-0235-4917-A7FF-35C8886AAD3A"),
                MineralsRequired = new Dictionary<Guid, long>(),

                MassPerUnit = 1,
                //soriumFuel.CargoType = CargoType.Fuel;
                IndustryPointCosts = 10,
                OutputAmount = 1
            };
            soriumFuel.MineralsRequired.Add(new Guid("08f15d35-ea1d-442f-a2e3-bde04c5c22e9"), 1);
            mats.Add(soriumFuel.ID, soriumFuel);

            ProcessedMaterialSD DepleatedDuranuim = new ProcessedMaterialSD()
            {
                Name = "Depleated Duranuim",
                Description = "A mix of Duranium and refined fuel to teset refinarys",
                ID = new Guid("6DA93677-EE08-4853-A8A5-0F46D93FE0EB"),
                MineralsRequired = new Dictionary<Guid, long>(),

                MaterialsRequired = new Dictionary<Guid, long>(),

                MassPerUnit = 1,
                //DepleatedDuranuim.CargoType = CargoType.General;
                IndustryPointCosts = 20,
                OutputAmount = 6
            };
            DepleatedDuranuim.MineralsRequired.Add(new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), 5);
            DepleatedDuranuim.MaterialsRequired.Add(new Guid("33E6AC88-0235-4917-A7FF-35C8886AAD3A"), 1);
            mats.Add(DepleatedDuranuim.ID, DepleatedDuranuim);

            StaticDataManager.ExportStaticData(mats, "ReinfedMaterialsDataExportTest.json");

        }

        [Test]
        public void TestTechSave()
        {
            Dictionary<Guid, TechSD> techs = new Dictionary<Guid, TechSD>();
            TechSD enginePowerModMax = new TechSD();
            enginePowerModMax.ID = new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e");
            enginePowerModMax.MaxLevel = 7;
            enginePowerModMax.DataFormula = "[Level] * 1.5";
            enginePowerModMax.Name = "Maximum Engine Power Modifier";
            enginePowerModMax.Description = "";
            enginePowerModMax.Category = ResearchCategories.PowerAndPropulsion;
            enginePowerModMax.CostFormula = "[Level] * 1";
            enginePowerModMax.Requirements = new Dictionary<Guid, int>();

            techs.Add(enginePowerModMax.ID, enginePowerModMax);

            TechSD enginePowerModMin = new TechSD();
            enginePowerModMin.ID = new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de");
            enginePowerModMin.MaxLevel = 7;
            enginePowerModMin.DataFormula = "1.0 - [Level] * 0.05";
            enginePowerModMin.Name = "Minimum Engine Power Modifier";
            enginePowerModMin.Description = "";
            enginePowerModMin.Category = ResearchCategories.PowerAndPropulsion;
            enginePowerModMin.CostFormula = "[Level] * 1";
            enginePowerModMin.Requirements = new Dictionary<Guid, int>();

            techs.Add(enginePowerModMin.ID, enginePowerModMin);


            TechSD fuelUsage = new TechSD();
            fuelUsage.ID = new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc");
            fuelUsage.MaxLevel = 12;
            fuelUsage.DataFormula = "1 - [Level] * 0.1";
            fuelUsage.Name = "Fuel Consumption: 1 Litre per Engine Power Hour";
            fuelUsage.Description = "";
            fuelUsage.Category = ResearchCategories.PowerAndPropulsion;
            fuelUsage.CostFormula = "[Level] * 1";
            fuelUsage.Requirements = new Dictionary<Guid, int>();
            techs.Add(fuelUsage.ID, fuelUsage);


            TechSD EngineTech1 = new TechSD();
            EngineTech1.ID = new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c");
            EngineTech1.MaxLevel = 1;
            EngineTech1.DataFormula = "0.2";
            EngineTech1.Name = "Conventional Engine Technology";
            EngineTech1.Description = "";
            EngineTech1.Category = ResearchCategories.PowerAndPropulsion;
            EngineTech1.CostFormula = "[Level] * 500";
            EngineTech1.Requirements = new Dictionary<Guid, int>();
            techs.Add(EngineTech1.ID, EngineTech1);

            TechSD EngineTech2 = new TechSD();
            EngineTech2.ID = new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7");
            EngineTech2.MaxLevel = 1;
            EngineTech2.DataFormula = "5";
            EngineTech2.Name = "Nuclear Thermal Engine Technology";
            EngineTech2.Description = "";
            EngineTech2.Category = ResearchCategories.PowerAndPropulsion;
            EngineTech2.CostFormula = "[Level] * 2500";
            EngineTech2.Requirements = new Dictionary<Guid, int>();
            EngineTech2.Requirements.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            techs.Add(EngineTech2.ID, EngineTech2);

            TechSD EngineTech3 = new TechSD();
            EngineTech3.ID = new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2");
            EngineTech3.MaxLevel = 1;
            EngineTech3.DataFormula = "5";
            EngineTech3.Name = "Nuclear Pulse Engine Technology";
            EngineTech3.Description = "";
            EngineTech3.Category = ResearchCategories.PowerAndPropulsion;
            EngineTech3.CostFormula = "[Level] * 5000";
            EngineTech3.Requirements = new Dictionary<Guid, int>();
            EngineTech3.Requirements.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1);
            techs.Add(EngineTech3.ID, EngineTech3);

            TechSD EngineTech4 = new TechSD();
            EngineTech4.ID = new Guid("f3f10e56-9345-40cc-af42-342e7240355d");
            EngineTech4.MaxLevel = 1;
            EngineTech4.DataFormula = "5";
            EngineTech4.Name = "Ion Drive Technology";
            EngineTech4.Description = "";
            EngineTech4.Category = ResearchCategories.PowerAndPropulsion;
            EngineTech4.CostFormula = "[Level] * 10000"; ;
            EngineTech4.Requirements = new Dictionary<Guid, int>();
            EngineTech4.Requirements.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);
            techs.Add(EngineTech4.ID, EngineTech4);

            StaticDataManager.ExportStaticData(techs, "TechnologyDataExportTest.json");

        }

        [Test]
        public void TestLoadDefaultData()
        {
            Game game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 0 });
            var staticDataStore = game.StaticData;

            // store counts for later:
            int mineralsNum = staticDataStore.CargoGoods.GetMineralsList().Count;
            int techNum = staticDataStore.Techs.Count;
            int constructableObjectsNum = staticDataStore.CargoGoods.GetMaterialsList().Count;

            // check that data was loaded:
            Assert.IsNotEmpty(staticDataStore.CargoGoods.GetMineralsList());
            Assert.IsNotEmpty(staticDataStore.AtmosphericGases);
            Assert.IsNotEmpty(staticDataStore.CargoGoods.GetMaterialsList());
            Assert.IsNotEmpty(staticDataStore.CargoTypes);
            Assert.IsNotEmpty(staticDataStore.Techs);

            // now lets re-load the same data, to test that duplicates don't occure as required:
            StaticDataManager.LoadData("Pulsar4x", game);

            // now check that overwriting occured and that there were no duplicates:
            Assert.AreEqual(mineralsNum, staticDataStore.CargoGoods.GetMineralsList().Count);
            Assert.AreEqual(techNum, staticDataStore.Techs.Count);
            Assert.AreEqual(constructableObjectsNum, staticDataStore.CargoGoods.GetMaterialsList().Count);

            // now lets test some malformed data folders.
            StaticDataLoadException ex = Assert.Throws<StaticDataLoadException>(
            delegate { StaticDataManager.LoadData("MalformedData", game); });
            Assert.That(ex.Message, Is.EqualTo("Error while loading static data: Bad Json provided in directory: MalformedData"));

            // now ,lets try for a directory that does not exist.
            Assert.Throws<DirectoryNotFoundException>(
            delegate { StaticDataManager.LoadData("DoesNotExist", game); });
        }

        [Test]
        public void TestOverwriteDefaultData()
        {
            Game game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 0 });
            StaticDataManager.LoadData("Pulsar4x", game);
            var staticDataStore = game.StaticData;

            // store counts for later:
            int mineralsNum = staticDataStore.CargoGoods.GetMineralsList().Count;
            Guid someGuid = new Guid("08f15d35-ea1d-442f-a2e3-bde04c5c22e9");
            string someName = staticDataStore.CargoGoods.GetMineral(someGuid).Name;

            StaticDataManager.LoadData("Other", game);
            staticDataStore = game.StaticData;

            // now check that overwriting occured and that there were no duplicates:
            Assert.AreEqual(mineralsNum, staticDataStore.CargoGoods.GetMineralsList().Count);
            //check the name has been overwritten
            Assert.AreNotEqual(someName, staticDataStore.CargoGoods.GetMineral(someGuid).Name);
        }

        [Test]
        public void TestIDLookup()
        {
            // Create an empty data store:
            Game game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 0 });
            var staticDataStore = game.StaticData;

            // test when the store is empty:
            object testNullObj = staticDataStore.FindDataObjectUsingID(Guid.NewGuid());
            Assert.IsNull(testNullObj);

            // Load the default static data to test against:
            StaticDataManager.LoadData("Pulsar4x", game);

            // test with a guid that is not in the store:
            object testObj = staticDataStore.FindDataObjectUsingID(Guid.Empty);  // empty guid should never be in the store.
            Assert.IsNull(testObj);

            // now lets test for values that are in the store:
            Guid testID = staticDataStore.CargoGoods.GetMinerals().FirstOrDefault().Key;
            testObj = staticDataStore.FindDataObjectUsingID(testID);
            Assert.IsNotNull(testObj);
            Assert.AreEqual(testID, ((MineralSD)testObj).ID);

            testObj = staticDataStore.FindDataObjectUsingID(testID);
            Assert.IsNotNull(testObj);

            testID = staticDataStore.Techs.First().Key;
            testObj = staticDataStore.FindDataObjectUsingID(testID);
            Assert.IsNotNull(testObj);
            Assert.AreEqual(testID, ((TechSD)testObj).ID);
        }

        //for want of a better place to put it.
        [Test]
        public void TestJdicExtension()
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            dict.Add(1,1);
            dict.SafeValueAdd(1, 2); //add to exsisting
            dict.SafeValueAdd(2, 5); //add to non exsisting.

            Assert.AreEqual(dict[1], 3);
            Assert.AreEqual(dict[2], 5);
        }
    }
}