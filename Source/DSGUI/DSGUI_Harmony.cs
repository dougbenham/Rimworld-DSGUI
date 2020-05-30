﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
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
            static bool Prefix(Pawn pawn)
            {
                return DSGUI.Create(UI.MouseMapPosition(), pawn);
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
                if (!Find.WindowStack.IsOpen(typeof(DSGUI_ListModal)) || GlobalStorage.currThing == null)
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
                if (!Find.WindowStack.IsOpen(typeof(DSGUI_ListModal)))
                    return true;

                __result = GlobalStorage.currThing;
            
                return false;
            }
        }

        [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
        private static class Patch_AddHumanlikeOrders
        {
            private static readonly MethodInfo getFI = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetFirstItem));
            private static readonly MethodInfo getTL = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList));

            private static readonly MethodInfo dsguiFI = AccessTools.Method(typeof(DSGUI), nameof(DSGUI.GetFirstItem));
            private static readonly MethodInfo dsguiTL = AccessTools.Method(typeof(DSGUI), nameof(DSGUI.GetThingList));
            
            public static bool Prepare(Harmony instance) 
            {
                return DSGUIMod.settings.DSGUI_UseTranspiler;
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();

                var patched = false;

                foreach (var instruction in instructionList)
                {
                    if (patched)
                        yield return instruction;
                        
                    if (instruction.Calls(getFI))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = dsguiFI;
                        yield return instruction;
                    }
                        
                    if (instruction.Calls(getTL))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = dsguiTL;
                        yield return instruction;
                    }
                    
                    patched = true;
                    
                    yield return instruction;
                }
            }
        }
    }
}