﻿using Essence.classes.utility;
using GTANetworkServer;
using GTANetworkShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Essence.classes.jobs
{
    public class ShortRangeTrucking : Script
    {
        private Vector3 startPoint = new Vector3(-1328.738, -212.1859, 42.38577);
        private Vector3 midPoint = new Vector3(-1264.165, -306.5757, 36.02926);
        private Vector3 endPoint = new Vector3(-1263.66, -307.4156, 35.51007);
        private List<Vector3> locations = new List<Vector3>();
        private const int reward = 18;
        // For Spawns
        private DateTime lastTimeCheck = DateTime.Now;
        private List<SpawnInfo> spawns = new List<SpawnInfo>();

        public ShortRangeTrucking()
        {
            PointHelper.addNewPoint(3, 477, startPoint, "Short Range Trucking", true, "JOB_SHORT_RANGE_TRUCKING");
            loadLocations();
            loadSpawns();
            API.onUpdate += API_onUpdate;
        }

        private void API_onUpdate()
        {
            spawnChecker();
        }

        private void spawnChecker()
        {
            if (DateTime.Now > lastTimeCheck)
            {
                lastTimeCheck = DateTime.Now.AddMilliseconds(10000);
                foreach (SpawnInfo spawn in spawns)
                {
                    spawn.checkOccupied();
                }
            }
        }

        private void loadLocations()
        {
            locations = Utility.pullLocationsFromFile("resources/Essence/data/shortrangetrucking.txt");
        }

        private void loadSpawns()
        {
            List<Vector3> locs = Utility.pullLocationsFromFile("resources/Essence/data/shortrangetruckingspawns.txt");
            List<Vector3> rots = Utility.pullLocationsFromFile("resources/Essence/data/shortrangetruckingspawnsrotations.txt");

            int count = 0;

            foreach (Vector3 loc in locs)
            {
                SpawnInfo spawn = new SpawnInfo(loc, rots[count]);
                spawns.Add(spawn);
                count++;
            }
        }

        public void startShortRangeTruckingJob(Client player)
        {
            if (player.position.DistanceTo(startPoint) >= 5)
            {
                return;
            }

            Mission mission;

            if (API.hasEntityData(player, "Mission"))
            {
                mission = API.getEntityData(player, "Mission");
                if (mission.MissionObjectiveCount > 0)
                {
                    API.sendChatMessageToPlayer(player, "~r~You seem to already have a mission running.");
                    return;
                }
            } else {
                mission = new Mission();
            }

            // Basic Setup.
            
            mission.useTimer();
            mission.MissionTime = 60 * 5;
            mission.MissionReward = reward;
            mission.MissionTitle = "Short Range Trucking";

            API.setEntitySyncedData(player, "Mission_New_Instance", true);
            API.setEntityData(player, "Mission", mission);
            mission.addPlayer(player);

            // Mission Framework
            Objective objective;
            // Queue System
            SpawnInfo openSpot = null;
            while (openSpot == null)
            {
                foreach (SpawnInfo spawn in spawns)
                {
                    if (!spawn.Occupied)
                    {
                        openSpot = spawn;
                        openSpot.Occupied = true;
                        openSpot.Vehicle = player;
                        break;
                    }
                }

                if (openSpot != null)
                {
                    break;
                }
            }
            // Setup a unique ID for the vehicle.
            int unID = new Random().Next(1, 50000);
            // Set Our Start Location
            objective = mission.CreateNewObjective(openSpot.Position, Objective.ObjectiveTypes.RetrieveVehicle);
            objective.addObjectiveVehicle(mission, openSpot.Position, VehicleHash.Youga2, rotation: openSpot.Rotation, uniqueID: unID);
            // Add the unique id.
            objective.addUniqueIDToAllObjectives(unID);
            // Setup pickup Objective.
            NetHandle newObject = API.createObject(-719727517, new Vector3(-1321.264, -253.4067, 41.13453), new Vector3());
            objective.setupObjective(new Vector3(-1321.264, -253.4067, 41.13453), Objective.ObjectiveTypes.PickupObject, obj: newObject);
            // Mid Point
            objective = mission.CreateNewObjective(midPoint, Objective.ObjectiveTypes.VehicleLocation);
            objective.addUniqueIDToAllObjectives(unID);
            // Pull a random location.
            int random = new Random().Next(0, locations.Count);
            objective = mission.CreateNewObjective(locations[random], Objective.ObjectiveTypes.VehicleCapture);
            objective.addUniqueIDToAllObjectives(unID);
            // Setup end point.
            objective = mission.CreateNewObjective(endPoint, Objective.ObjectiveTypes.VehicleLocation);
            objective.addUniqueIDToAllObjectives(unID);
            // Deliver truck back to end points
            objective = mission.CreateNewObjective(startPoint, Objective.ObjectiveTypes.VehicleLocation);
            objective.addUniqueIDToAllObjectives(unID);
            mission.startMission();
            
        }
    }
}
