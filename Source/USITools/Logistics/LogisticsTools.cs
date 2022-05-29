﻿using System;
using System.Collections.Generic;
using UnityEngine;
using USITools.Logistics;

namespace USITools
{
    
    public class LogisticsTools
    {
        public const float PHYSICS_RANGE = 2000f;
        public static double GetRange(Vessel a, Vessel b)
        {
            var posCur = a.GetWorldPos3D();
            var posNext = b.GetWorldPos3D();
            return Vector3d.Distance(posCur, posNext);
        }

        public static bool AnyNearbyPartModules<T>(double range, Vessel referenceVessel)
            where T: PartModule
        {
            try
            {
                if (FlightGlobals.Vessels == null ||
                    FlightGlobals.Vessels.Count < 2 ||
                    referenceVessel == null)
                {
                    return false;
                }
                var referencePosition = referenceVessel.GetWorldPos3D();
                foreach (var vessel in FlightGlobals.Vessels)
                {
                    if (vessel.persistentId == referenceVessel.persistentId)
                    {
                        continue;
                    }
                    var partModule = vessel.FindPartModuleImplementing<T>();
                    if (partModule == null)
                    {
                        continue;
                    }
                    var distance
                        = Vector3d.Distance(vessel.GetWorldPos3D(), referencePosition);
                    if (distance <= range)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[USITools] {nameof(LogisticsTools)}.{nameof(AnyNearbyPartModules)}: {ex.Message}");
                return false;
            }
        }

        public static bool AnyNearbyVessels(double range, Vessel referenceVessel)
        {
            try
            {
                if (FlightGlobals.Vessels == null ||
                    FlightGlobals.Vessels.Count < 2 ||
                    referenceVessel == null)
                {
                    return false;
                }
                var referencePosition = referenceVessel.GetWorldPos3D();
                foreach (var vessel in FlightGlobals.Vessels)
                {
                    if (vessel.persistentId == referenceVessel.persistentId)
                    {
                        continue;
                    }
                    var distance
                        = Vector3d.Distance(vessel.GetWorldPos3D(), referencePosition);
                    if (distance <= range)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[USITools] LogisticsTools.AnyNearbyVessels: {ex.Message}");
                return false;
            }
        }

        public static List<T> GetNearbyPartModules<T>(
            float range,
            Vessel referenceVessel,
            bool includeReference = false,
            bool landedOnly = true)
            where T: PartModule
        {
            try
            {
                var partModules = new List<T>();
                var count = FlightGlobals.Vessels.Count;
                for (int i = 0; i < count; i++)
                {
                    var vessel = FlightGlobals.Vessels[i];
                    if (vessel.mainBody == referenceVessel.mainBody &&
                        (vessel.Landed || !landedOnly))
                    {
                        if (!includeReference && vessel == referenceVessel)
                        {
                            continue;
                        }
                        if (GetRange(referenceVessel, vessel) <= range)
                        {
                            var modules = vessel.FindPartModulesImplementing<T>();
                            if (modules != null && modules.Count > 0)
                            {
                                partModules.AddRange(modules);
                            }
                        }
                    }
                }
                return partModules;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[USITools] {nameof(LogisticsTools)}.{nameof(GetNearbyPartModules)}: {ex.Message}");
                return null;
            }
        }

        public static List<Vessel> GetNearbyVessels(float range, bool includeSelf, Vessel thisVessel, bool landedOnly = true)
        {
            try
            {
                var vessels = new List<Vessel>();
                var count = FlightGlobals.Vessels.Count;
                for (int i = 0; i < count; ++i)
                {
                    var v = FlightGlobals.Vessels[i];
                    if (v.mainBody == thisVessel.mainBody
                        && (v.Landed || !landedOnly || v == thisVessel))
                    {
                        if (v == thisVessel && !includeSelf)
                            continue;

                        if (GetRange(thisVessel, v) < range)
                        {
                            vessels.Add(v);
                        }
                    }
                }
                return vessels;
            }
            catch (Exception ex)
            {
                Debug.Log(String.Format("[MKS] - ERROR in GetNearbyVessels - {0}", ex.Message));
                return new List<Vessel>();
            }
        }

        public static List<T> GetRegionalModules<T>(Vessel referenceVessel)
            where T: class
        {
            var range = (float)LogisticsSetup.Instance.Config.MaintenanceRange;
            var nearbyVessels = GetNearbyVessels(range, true, referenceVessel, false);
            var moduleList = new List<T>();
            foreach (var vessel in nearbyVessels)
            {
                foreach (var part in vessel.parts)
                {
                    var module = part.FindModuleImplementing<T>();
                    if (module != null)
                    {
                        moduleList.Add(module);
                    }
                }
            }
            return moduleList;
        }

        public static List<Part> GetRegionalWarehouses(Vessel vessel, string module)
        {
            var pList = new List<Part>();
            var vList = GetNearbyVessels((float)LogisticsSetup.Instance.Config.MaintenanceRange, true, vessel, false);
            var count = vList.Count;
            for(int i = 0; i < count; ++i)
            {
                var v = vList[i];
                var parts = v.parts;
                var pCount = parts.Count;
                for (int x = 0; x < pCount; ++x)
                {
                    Part p = parts[x];
                    if(p.Modules.Contains(module) || vessel == v)
                        pList.Add(p);
                }
            }
            return pList;
        }

        public static bool HasCrew(Vessel v, string skill)
        {
            var crew = v.GetVesselCrew();
            var count = crew.Count;
            for (int i = 0; i < count; ++i)
            {
                if (crew[i].experienceTrait.TypeName == skill)
                    return true;
            }
            return false;
        }

        public static bool NearbyCrew(Vessel v, float range, String effect)
        {
            List<Vessel> nearby = GetNearbyVessels(range, true, v, true);
            var count = nearby.Count;
            for (int i = 0; i < count; ++i)
            {
                var vsl = nearby[i];
                var crew = vsl.GetVesselCrew();
                var cCount = crew.Count;
                for (int x = 0; x < cCount; ++x)
                {
                    if(crew[x].HasEffect(effect))
                        return true;
                }
            }
            return false;
        }
    }
}
