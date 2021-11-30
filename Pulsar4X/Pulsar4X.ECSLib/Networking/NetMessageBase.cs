﻿using System;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ECSLib
{
    public abstract class NetMessageBase
    {
        public abstract void HandleMessage(Game game);
    }




    public class EntityDataRequest: NetMessageBase
    {
        Guid MangerGuid;
        Guid FactionGuid;
        Guid EntityGuid;

        public override void HandleMessage(Game game)
        {
            Entity entity = game.GlobalManager.GetGlobalEntityByGuid(EntityGuid);
            Entity faction = game.GlobalManager.GetLocalEntityByGuid(FactionGuid);
            if (entity.FactionOwnerID == faction.Guid)
            {
                //serialise entity and send it
                throw new NotImplementedException();
            }
        }        
    }

    public class UpdateEntityAdded : NetMessageBase
    {
        Guid ManagerGuid;
        Entity NewEntity;

        public UpdateEntityAdded(EntityManager manager, Entity newEntity)
        {
            ManagerGuid = manager.ManagerGuid;
            NewEntity = newEntity;
        }

        public override void HandleMessage(Game game)
        {
            //game.GlobalManagerDictionary[ManagerGuid]        
        }
    }
    public class UpdateEntityRemoved : NetMessageBase
    {
        Guid ManagerGuid;
        Guid RemovedEntity;

        public override void HandleMessage(Game game)
        {
            //game.GlobalManagerDictionary[ManagerGuid].RemoveEntity(
        }
    }

    public class UpdateNewTick : NetMessageBase
    {
        public override void HandleMessage(Game game)
        {
            throw new NotImplementedException();
        }
    }

}
