using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterBusStopPosition
{

[HarmonyPatch(typeof(BusAI))]
public static class BusAI_Patch
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CalculateSegmentPosition))]
    public static IEnumerable<CodeInstruction> CalculateSegmentPosition(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool found = false;
        for( int i = 0; i < codes.Count; ++i )
        {
            // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
            // The function has code:
            // CalculateStopPositionAndDirection((float)(int)offset * 0.003921569f, num, out pos, out dir);
            // Change to:
            // CalculateStopPositionAndDirection(CalculateSegmentPosition_Hook((float)(int)offset * 0.003921569f, vehicleData), num, out pos, out dir);
            if( codes[ i ].opcode == OpCodes.Ldarg_S && codes[ i ].operand.ToString() == "5"
                && i + 7 < codes.Count
                && codes[ i + 2 ].opcode == OpCodes.Ldc_R4
                && codes[ i + 3 ].opcode == OpCodes.Mul
                && codes[ i + 7 ].opcode == OpCodes.Call
                && codes[ i + 7 ].operand.ToString() == "Void CalculateStopPositionAndDirection(Single, Single, Vector3 ByRef, Vector3 ByRef)" )
            {
                codes.Insert( i + 4, new CodeInstruction( OpCodes.Ldarg_2 )); // Load 'vehicleData'.
                codes.Insert( i + 5, new CodeInstruction( OpCodes.Call,
                    typeof( BusAI_Patch ).GetMethod( nameof( CalculateSegmentPosition_Hook ))));
                found = true;
                break;
            }
        }
        if( !found )
            Debug.LogError("BetterBusStopPosition: Failed to patch BusAI.CalculateSegmentPosition()");
        return codes;
    }

    public static float CalculateSegmentPosition_Hook( float laneOffset, ref Vehicle vehicleData )
    {
        // Debug.Log("XXX:" + (vehicleData.m_flags & Vehicle.Flags.Leaving) + ":" + (vehicleData.m_flags & Vehicle.Flags.Arriving) + ":" + laneOffset);
        // Do not change anything when leaving a stop, the start point is calculated from the position of the bus,
        // so everything is correct without changes.
        if(( vehicleData.m_flags & Vehicle.Flags.Leaving ) != 0 )
            return laneOffset;
        // The lane offset is 0.5f when at the (vanilla) stop position, move that place to 0.8f.
        const float newStopOffset = 0.8f;
        // When the stop is in the opposite direction of the segment, flip, calculate and flip back.
        bool inverted = laneOffset > newStopOffset;
        if( inverted )
            laneOffset = 1f - laneOffset;
        // This will make the bus stop more forward in the segment.
        laneOffset *= 2 * newStopOffset; // (2 is to convert the max 0.5f to 1f)
        if( inverted )
            laneOffset = 1f - laneOffset;
        return laneOffset;
    }
}

}
