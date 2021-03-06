﻿using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using UnityEngine;

namespace MechEngineer
{
    internal class CockpitHandler : IAdjustUpgradeDef, IAutoFixMechDef, IPreProcessor
    {
        internal static CockpitHandler Shared = new CockpitHandler();
        
        private readonly IdentityHelper identity;
        private readonly AutoFixMechDefHelper fixer;
        private readonly AdjustCompDefTonnageHelper resizer;

        private CockpitHandler()
        {
            identity = Control.settings.AutoFixCockpitCategorizer;

            fixer = new AutoFixMechDefHelper(
                identity,
                Control.settings.AutoFixMechDefCockpitAdder
            );

            resizer = new AdjustCompDefTonnageHelper(identity, Control.settings.AutoFixCockpitTonnageChange);
        }

        public void PreProcess(MechComponentDef target, Dictionary<string, object> values)
        {
            identity.PreProcess(target, values);
        }

        public bool ProtectsAgainstShutdownInjury(MechDef mechDef)
        {
            return mechDef.Inventory.Any(c => c.DamageLevel == ComponentDamageLevel.Functional && identity.IsCustomType(c.Def));
        }

        public void AdjustUpgradeDef(UpgradeDef upgradeDef)
        {
            resizer.AdjustComponentDef(upgradeDef);
        }

        public void AutoFixMechDef(MechDef mechDef, float originalTotalTonnage)
        {
            fixer.AutoFixMechDef(mechDef, originalTotalTonnage);
        }
    }
}