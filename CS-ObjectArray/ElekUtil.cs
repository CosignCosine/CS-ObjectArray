using System;
using UnityEngine;
using System.Collections.Generic;
public static class ElekUtil
{
    // Flag tools
    public static bool IsFlagSet(this Enum value, Enum flag)
    {
        long f = Convert.ToInt32(flag);
        long v = Convert.ToInt32(value);

        return value != null && (v & f) == f;
    }
}

public enum ObjectType
{

    // Ingame
    None,
    Building,
    NetNode,
    NetSegment,
    Prop,
    Tree,

    // Moveable
    Citizen,
    Vehicle,
    ParkedVehicle,

    // Metadata
    TransportLine,
    District,
    ParkDistrict,
    Disaster,

    // Attribute
    Position,

    // Modded
    PO
}


public class ElekRaycastUtil : ToolBase
{
    // Raycasting tools

    public static void RaycastTo(ObjectType type, out RaycastOutput output)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        ToolBase.RaycastInput input = new ToolBase.RaycastInput(ray, Camera.main.farClipPlane);
        input.m_ignoreSegmentFlags = type.IsFlagSet(ObjectType.NetSegment) ? NetSegment.Flags.None : NetSegment.Flags.All;
        input.m_ignoreNodeFlags = type.IsFlagSet(ObjectType.NetNode) ? NetNode.Flags.None : NetNode.Flags.All;
        input.m_ignoreParkFlags = type.IsFlagSet(ObjectType.ParkDistrict) ? DistrictPark.Flags.None : DistrictPark.Flags.All;
        input.m_ignorePropFlags = type.IsFlagSet(ObjectType.Prop) ? PropInstance.Flags.None : PropInstance.Flags.All;
        input.m_ignoreTreeFlags = type.IsFlagSet(ObjectType.Tree) ? TreeInstance.Flags.None : TreeInstance.Flags.All;
        input.m_ignoreCitizenFlags = type.IsFlagSet(ObjectType.Citizen) ? CitizenInstance.Flags.None : CitizenInstance.Flags.All;
        input.m_ignoreVehicleFlags = type.IsFlagSet(ObjectType.Vehicle) ? Vehicle.Flags.Deleted : Vehicle.Flags.Created;
        input.m_ignoreBuildingFlags = type.IsFlagSet(ObjectType.Building) ? Building.Flags.None : Building.Flags.All;
        input.m_ignoreDisasterFlags = type.IsFlagSet(ObjectType.Disaster) ? DisasterData.Flags.None : DisasterData.Flags.All;
        input.m_ignoreTransportFlags = type.IsFlagSet(ObjectType.TransportLine) ? TransportLine.Flags.None : TransportLine.Flags.All;
        input.m_ignoreParkedVehicleFlags = type.IsFlagSet(ObjectType.ParkedVehicle) ? VehicleParked.Flags.None : VehicleParked.Flags.All;
        input.m_ignoreDistrictFlags = type.IsFlagSet(ObjectType.District) ? District.Flags.None : District.Flags.All;
        input.m_ignoreTerrain = !type.IsFlagSet(ObjectType.Position);

        RayCast(input, out ToolBase.RaycastOutput outputx);

        output = outputx;

    }
}

public struct Barycentric
{
    public float u;
    public float v;
    public float w;

    public Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Using Cramer's rule to solve https://en.wikipedia.org/wiki/Barycentric_coordinate_system equation at the bottom of "Conversion between barycentric and Cartesian coordinates"
        Vector2 x = b - a, y = c - a, z = p - a;
        float d = 1.0f / (x.x * y.y - y.x * x.y);
        v = (z.x * y.y - y.x * z.y) * d;
        w = (x.x * z.y - z.x * x.y) * d;
        u = 1.0f - v - w;
    }
}