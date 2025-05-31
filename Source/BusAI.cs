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
        bool found1 = false;
        bool found2 = false;
        for( int i = 0; i < codes.Count; ++i )
        {
            // Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
            // The function has code:
            // NetInfo.Lane lane = info.m_lanes[position.m_lane];
            // Verify that the result is stored in loc #2.
            if( codes[ i ].opcode == OpCodes.Ldloc_2
                && i + 1 < codes.Count
                && codes[ i + 1 ].opcode == OpCodes.Ldfld && codes[ i + 1 ].operand.ToString() == "System.Single m_stopOffset" )
            {
                found1 = true;
            }
            // The function has code:
            // CalculateStopPositionAndDirection((float)(int)offset * 0.003921569f, num, out pos, out dir);
            // Change to:
            // CalculateStopPositionAndDirection(CalculateSegmentPosition_Hook((float)(int)offset * 0.003921569f, vehicleData, lane),
            //     num, out pos, out dir);
            if( found1 && codes[ i ].opcode == OpCodes.Ldarg_S && codes[ i ].operand.ToString() == "5"
                && i + 7 < codes.Count
                && codes[ i + 2 ].opcode == OpCodes.Ldc_R4
                && codes[ i + 3 ].opcode == OpCodes.Mul
                && codes[ i + 7 ].opcode == OpCodes.Call
                && codes[ i + 7 ].operand.ToString() == "Void CalculateStopPositionAndDirection(Single, Single, Vector3 ByRef, Vector3 ByRef)" )
            {
                // Keep the offset calculation argument.
                codes.Insert( i + 4, new CodeInstruction( OpCodes.Ldarg_2 )); // Load 'vehicleData'.
                codes.Insert( i + 5, new CodeInstruction( OpCodes.Ldloc_2 )); // Load 'lane' (loc #2 above).
                codes.Insert( i + 6, new CodeInstruction( OpCodes.Call,
                    typeof( BusAI_Patch ).GetMethod( nameof( CalculateSegmentPosition_Hook ))));
                // Return value will replace the offset.
                found2 = true;
                break;
            }
        }
        if( !found1 || !found2 )
            Debug.LogError("BetterBusStopPosition: Failed to patch BusAI.CalculateSegmentPosition()");
        return codes;
    }

    public static float CalculateSegmentPosition_Hook( float laneOffset, ref Vehicle vehicleData, NetInfo.Lane laneInfo )
    {
        // Do not change anything when leaving a stop, the start point is calculated from the position of the bus,
        // so everything is correct without changes.
        if(( vehicleData.m_flags & Vehicle.Flags.Leaving ) != 0 )
            return laneOffset;
        // The lane offset is 0.5f when at the (vanilla) stop position, move that place to 0.8f.
        const float newStopOffset = 0.8f;
        // When the stop is in the opposite direction of the segment, flip, calculate and flip back.
        bool inverted = ( laneInfo.m_finalDirection & NetInfo.Direction.Backward ) != 0;
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
