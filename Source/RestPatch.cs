using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace YDISComfortableVehiclesPatch
{
    // Problem (verified by decompiling Comfortable Vehicles' VFCompatsForCV.dll and You Drive, I Sleep):
    //  - Vanilla Caravan_NeedsTracker.TrySatisfyRestNeed gives rest when !caravan.pather.MovingNow.
    //  - Comfortable Vehicles' VF compat prefix skips it for every pawn InVehicle, and its own rest
    //    logic skips caravan members when You Drive, I Sleep is active (and always skips drivers/gunners).
    //  - You Drive, I Sleep only gives rest while MovingNow, never to the driver.
    //  => stopped vehicle caravan with both mods: nobody inside a vehicle regains sleep.
    //
    // Fix: postfix (runs even when a prefix skipped the original). When the caravan is stopped,
    // the pawn is inside something other than the caravan itself (a vehicle), and nobody gave it
    // rest this tick, apply exactly what vanilla would have applied.
    //
    // Only active when both mods are loaded. Without You Drive, I Sleep, Comfortable Vehicles rests
    // passengers itself, and this postfix would stack on top of it.
    [StaticConstructorOnStartup]
    public static class Startup
    {
        internal const string Tag = "[YDIS + CV Patch]";
        internal const string HarmonyId = "SirApoka.YDISComfortableVehiclesPatch";
        private const string YdisId = "spacemoth.youdriveisleep";
        private const string CvId = "eclair155.comfortablevehicles";

        static Startup()
        {
            try
            {
                if (!ModsConfig.IsActive(YdisId) || !ModsConfig.IsActive(CvId))
                {
                    Log.Warning($"{Tag} Needs both You Drive, I Sleep and Comfortable Vehicles active. Patch disabled.");
                    return;
                }

                var target = AccessTools.Method(typeof(Caravan_NeedsTracker), "TrySatisfyRestNeed");
                if (target == null)
                {
                    Log.Error($"{Tag} Caravan_NeedsTracker.TrySatisfyRestNeed not found. Patch disabled.");
                    return;
                }

                var ps = target.GetParameters();
                if (ps.Length < 2 || ps[0].ParameterType != typeof(Pawn) || ps[1].ParameterType != typeof(Need_Rest))
                {
                    Log.Error($"{Tag} Unexpected signature of TrySatisfyRestNeed ({ps.Length} params). Patch disabled.");
                    return;
                }

                if (!RestPatch.Init())
                    return;

                var postfix = new HarmonyMethod(typeof(RestPatch), nameof(RestPatch.Postfix)) { priority = Priority.Last };
                new Harmony(HarmonyId).Patch(target, postfix: postfix);
                Log.Message($"{Tag} Active. Pawns inside vehicles now rest while the caravan is stopped.");
            }
            catch (Exception e)
            {
                Log.Error($"{Tag} Failed to initialize. Patch disabled. {e}");
            }
        }
    }

    public static class RestPatch
    {
        private static AccessTools.FieldRef<Need_Rest, int> lastRestTick;
        private static bool disabled;
        private static bool loggedFirstUse;

        internal static bool Init()
        {
            try
            {
                lastRestTick = AccessTools.FieldRefAccess<Need_Rest, int>("lastRestTick");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"{Startup.Tag} Need_Rest.lastRestTick not accessible. Patch disabled. {e}");
                return false;
            }
        }

        // __0 = pawn, __1 = rest (positional, so parameter names in the game build don't matter).
        public static void Postfix(Caravan ___caravan, Pawn __0, Need_Rest __1)
        {
            if (disabled)
                return;

            try
            {
                if (___caravan == null || __0 == null || __1 == null || __0.Dead)
                    return;

                // Moving: leave it to You Drive, I Sleep (passengers only).
                // Vehicle Framework redirects MovingNow to the vehicle pather for vehicle caravans.
                if (___caravan.pather == null || ___caravan.pather.MovingNow)
                    return;

                // Pawns held directly by the caravan are vanilla's job and already handled.
                if (__0.ParentHolder is Caravan)
                    return;

                // Someone (vanilla, another mod) already gave rest this tick: don't override it.
                int now = Find.TickManager.TicksGame;
                if (lastRestTick(__1) == now)
                    return;

                // Same value vanilla uses.
                Building_Bed bed = __0.CurrentCaravanBed();
                float effectiveness = bed != null
                    ? bed.GetStatValue(StatDefOf.BedRestEffectiveness)
                    : StatDefOf.BedRestEffectiveness.valueIfMissing;

                __1.TickResting(effectiveness);

                if (!loggedFirstUse)
                {
                    loggedFirstUse = true;
                    Log.Message($"{Startup.Tag} First rest applied: {__0.LabelShort} in {___caravan.LabelCap}, " +
                                $"effectiveness {effectiveness:0.##}{(bed != null ? " (bedroll)" : "")}.");
                }
            }
            catch (Exception e)
            {
                disabled = true;
                Log.Error($"{Startup.Tag} Error while applying rest. Patch disabled for this session. {e}");
            }
        }
    }
}
