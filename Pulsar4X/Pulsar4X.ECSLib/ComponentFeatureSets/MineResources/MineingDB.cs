﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class MiningDB : BaseDataBlob
    {
        public Dictionary<Guid, long> MineingRate { get; set; }

        public Dictionary<Guid, MineralDepositInfo> MineralDeposit => OwningEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().Minerals;

        public MiningDB()
        {
            MineingRate = new Dictionary<Guid, long>();
        }

        public MiningDB(MiningDB db)
        {
            
        }

        public override object Clone()
        {
            return new MiningDB(this);
        }
    }
}