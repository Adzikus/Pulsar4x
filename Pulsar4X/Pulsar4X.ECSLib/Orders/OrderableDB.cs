﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class OrderableDB : BaseDataBlob
    {
        private List<EntityCommand> ActionList = new List<EntityCommand>();

        internal void ProcessOrderList(DateTime atDateTime)
        {
            //var atDatetime = OwningEntity.StarSysDateTime;
            int mask = 0;

            int i = 0;
            while (i < ActionList.Count)
            {
                EntityCommand entityCommand = ActionList[i];

                if ((mask & ((int)entityCommand.ActionLanes)) == 0) //bitwise and
                {
                    if (entityCommand.IsBlocking)
                    {
                        mask = mask | ((int)entityCommand.ActionLanes); //bitwise or
                    }
                    if (atDateTime >= entityCommand.ActionOnDate)
                    {
                        entityCommand.ActionCommand(atDateTime);
                    }
                }

                if (entityCommand.IsFinished())
                {
                    ActionList.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
        
        internal void AddCommandToList(EntityCommand command)
        {
            if (command.ActionOnDate > OwningEntity.StarSysDateTime)
            {
                OwningEntity.Manager.ManagerSubpulses.AddEntityInterupt(command.ActionOnDate, nameof(OrderableProcessor), OwningEntity);
            }
            ActionList.Add(command);
        }
        
        public int Count => ActionList.Count;

        internal void RemoveAt(int index)
        {
            ActionList.RemoveAt(index);
        }

        public List<EntityCommand> GetActionList()
        {
            //do I need a lock here?
            return new List<EntityCommand>( ActionList );
        }

        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            ActionList = new List<EntityCommand>(db.ActionList);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }


}
