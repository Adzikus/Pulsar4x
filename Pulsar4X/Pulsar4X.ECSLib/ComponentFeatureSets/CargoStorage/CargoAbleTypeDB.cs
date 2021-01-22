﻿using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Contains info on how an entitiy can be stored.
    /// NOTE an entity with this datablob must also have a MassVolumeDB
    /// </summary>
    public class CargoAbleTypeDB : BaseDataBlob , ICargoable, IComponentDesignAttribute
    {
        [JsonProperty]
        public Guid CargoTypeID { get; internal set; }

        /// <summary>
        /// NOTE! this is an entites *Design* ID, not the EntitesID.
        /// </summary>
        [JsonIgnore]
        public Guid ID {
            get
            {
                if (OwningEntity.HasDataBlob<DesignInfoDB>())
                    return this.OwningEntity.GetDataBlob<DesignInfoDB>().DesignEntity.Guid;
                else
                    return this.OwningEntity.Guid;
            }
        }

        [JsonIgnore]
        public long MassPerUnit => (long)Math.Ceiling(OwningEntity.GetDataBlob<MassVolumeDB>().MassDry); 

        public double VolumePerUnit => OwningEntity.GetDataBlob<MassVolumeDB>().Volume_m3;

        public double Density => OwningEntity.GetDataBlob<MassVolumeDB>().Density_kgm;

        /// <summary>
        /// This should be set to true if the item has become damaged or in any other way needs to maintain state 
        /// </summary>
        [JsonIgnore]
        public string Name
        {
            
            get 
            {
                if (this.OwningEntity.GetDataBlob<NameDB>() != null)
                {
                    return this.OwningEntity.GetDataBlob<NameDB>()?.GetName(OwningEntity.FactionOwner);
                }
                else return "Unknown Object"; 
            }
        }

        public CargoAbleTypeDB()
        {
        }

        [JsonProperty]
        internal bool MustBeSpecificCargo { get; set; } = false;

        public CargoAbleTypeDB(Guid cargoTypeID)
        {
            CargoTypeID = cargoTypeID;
        }

        public CargoAbleTypeDB(CargoAbleTypeDB cargoTypeDB)
        {
            CargoTypeID = cargoTypeDB.CargoTypeID;
            MustBeSpecificCargo = cargoTypeDB.MustBeSpecificCargo;
        }

        public override object Clone()
        {
            return new CargoAbleTypeDB(this);
        }
        
        
        public void OnComponentDeInstalation(Entity ship, Entity component)
        {
            throw new NotImplementedException();
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<CargoAbleTypeDB>())
                parentEntity.SetDataBlob(new CargoAbleTypeDB(this)); //basicaly just clone the design to the instance. 
        }
        
        public string AtbName()
        {
            return "Cargoable";
        }

        public string AtbDescription()
        {
            return "Parent can be stored in " + StaticRefLib.StaticData.CargoTypes[CargoTypeID].Name + " cargo";
            ;
        }
    }
}