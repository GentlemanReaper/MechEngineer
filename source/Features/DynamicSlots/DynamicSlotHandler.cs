﻿using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using HBS;
using UnityEngine;

namespace MechEngineer
{
    public class DynamicSlotHandler : IValidateMech, IValidateDrop
    {
        public static DynamicSlotHandler Shared = new DynamicSlotHandler();

        public static MechLabPanel MechLab => MechEngineer.MechLab.Current;

        #region settings
        private static readonly Color DynamicSlotsSpaceMissingColor = new Color(0.5f, 0, 0); // color changes when slots dont fit
        private static readonly ChassisLocations[] Locations = // order of locations to fill up first
        {
            ChassisLocations.CenterTorso,
            ChassisLocations.Head,
            ChassisLocations.LeftLeg,
            ChassisLocations.RightLeg,
            ChassisLocations.LeftTorso,
            ChassisLocations.RightTorso,
            ChassisLocations.LeftArm,
            ChassisLocations.RightArm
        };
        #endregion

        internal void RefreshData(MechDef def)
        {
            var fillerImageCache = MechLabLocationWidgetSetDataPatch.FillerImageCache;
            if (fillerImageCache.Count < Locations.Length)
            {
                return;
            }

            var mechLab = MechLab;
            if (mechLab == null)
            {
                return;
            }

            var slots = new MechDefSlots(def);
            using (var reservedSlots = slots.GetReservedSlots().GetEnumerator())
            {
                foreach (var location in Locations)
                {
                    var fillerImages = fillerImageCache[location];
                    var widget = mechLab.GetLocationWidget((ArmorLocation)location); // by chance armorlocation = chassislocation for main locations
                    var adapter = new MechLabLocationWidgetAdapter(widget);
                    var used = adapter.usedSlots;
                    for (var i = 0; i < adapter.maxSlots; i++)
                    {
                        var fillerImage = fillerImages[i];
                        if (i >= used && reservedSlots.MoveNext())
                        {
                            var reservedSlot = reservedSlots.Current;
                            if (reservedSlot == null)
                            {
                                throw new NullReferenceException();
                            }
                            fillerImage.gameObject.SetActive(true);
                            var uicolor = reservedSlot.ReservedSlotColor;
                            var color = LazySingletonBehavior<UIManager>.Instance.UIColorRefs.GetUIColor(uicolor);
                            fillerImage.color = slots.IsOverloaded ? DynamicSlotsSpaceMissingColor : color;
                        }
                        else
                        {
                            fillerImage.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public void ValidateMech(MechDef mechDef, Dictionary<MechValidationType, List<string>> errorMessages)
        {
            var slots = new MechDefSlots(mechDef);
            var missing = slots.Missing;
            if (missing > 0)
            {
                errorMessages[MechValidationType.InvalidInventorySlots]
                    .Add($"RESERVED SLOTS: Mech requires {missing} additional free slots");
            }
            RefreshData(mechDef);
        }
        
        public MechLabDropResult ValidateDrop(MechLabItemSlotElement element, MechLabLocationWidget widget)
        {
            var component = element.ComponentRef.Def;
            Control.mod.Logger.LogDebug($"========== Slot Check: start for {component.Description.Name} ==========");

            var dynamicSlots = component.GetComponent<DynamicSlots>();
            if (dynamicSlots == null)
            {
                return null;
            }

            var adapter = new MechLabLocationWidgetAdapter(widget);
            var mechDef = adapter.mechLab.activeMechDef;

            var slots = new MechDefSlots(mechDef);
            var newReserved = dynamicSlots.ReservedSlots;

            if (slots.Used + newReserved > slots.Total)
            {
                return new MechLabDropErrorResult($"Cannot add {component.Description.Name}: Not enough space.");
            }

            return null;
        }
    }
}