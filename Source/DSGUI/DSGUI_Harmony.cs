﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DSGUI
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("net.netrve.dsgui");

            // We patch all as we use annotations
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "TryMakeFloatMenu")]
        private static class Patch_TryMakeFloatMenu
        {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(Pawn pawn)
            {
                return DSGUI.Create(UI.MouseMapPosition(), pawn);
            }
        }

        [HarmonyPatch(typeof(GenUI), "TargetsAt")]
        private static class Patch_TargetsAt
        {
            public static bool Prepare(Harmony instance)
            {
                return !DSGUIMod.settings.DSGUI_UseTranspiler;
            }

            [HarmonyPriority(Priority.First)]
            public static void Postfix(ref IEnumerable<LocalTargetInfo> __result, Vector3 clickPos, TargetingParameters clickParams, bool thingsOnly)
            {
                if (GlobalStorage.currThing == null || !thingsOnly || !clickParams.canTargetItems)
                    return;

                var localTargetInfos = __result.ToList();

                localTargetInfos.RemoveAll(target => target.Thing != null);
                localTargetInfos.Add(new LocalTargetInfo(GlobalStorage.currThing));

                __result = localTargetInfos;
            }
        }

        [HarmonyPatch(typeof(GridsUtility), "GetThingList")]
        private static class Patch_GetThingList
        {
            public static bool Prepare(Harmony instance)
            {
                return !DSGUIMod.settings.DSGUI_UseTranspiler;
            }

            [HarmonyPriority(Priority.First)]
            public static bool Prefix(ref List<Thing> __result)
            {
                if (GlobalStorage.currThing == null)
                    return true;

                __result = new List<Thing> {GlobalStorage.currThing};

                return false;
            }
        }

        [HarmonyPatch(typeof(GridsUtility), "GetFirstItem")]
        private static class Patch_GetFirstItem
        {
            public static bool Prepare(Harmony instance)
            {
                return !DSGUIMod.settings.DSGUI_UseTranspiler;
            }

            [HarmonyPriority(Priority.First)]
            public static bool Prefix(ref Thing __result)
            {
                if (GlobalStorage.currThing == null)
                    return true;

                __result = GlobalStorage.currThing;

                return false;
            }
        }

        public static List<Thing> dsguiThingList(this IntVec3 c, Map map)
        {
            return GlobalStorage.currThing == null ? c.GetThingList(map) : new List<Thing> {GlobalStorage.currThing};
        }
            
        public static Thing dsguiFirstItem(this IntVec3 c, Map map)
        {
            return GlobalStorage.currThing == null ? c.GetFirstItem(map) : GlobalStorage.currThing;
        }
        
        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        private static class Patch_AddHumanlikeOrders
        {
            private static readonly MethodInfo getFI = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetFirstItem));
            private static readonly MethodInfo getTL = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList));

            private static readonly MethodInfo dsguiFI = AccessTools.Method(typeof(HarmonyPatches), nameof(dsguiFirstItem));
            private static readonly MethodInfo dsguiTL = AccessTools.Method(typeof(HarmonyPatches), nameof(dsguiThingList));
            
            public static bool Prepare(Harmony instance) 
            {
                return DSGUIMod.settings.DSGUI_UseTranspiler;
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions
                    .MethodReplacer(getFI, dsguiFI)
                    .MethodReplacer(getTL, dsguiTL);
            }
        }
    }
}