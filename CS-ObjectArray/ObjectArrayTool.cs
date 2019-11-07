using System;
using UnityEngine;
using ColossalFramework.Math;
using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace ObjectArray
{
    public class ObjectArrayTool : ToolBase
    {
        // @TODO: Pattern flags! Checkerboard (offset?), Radial, Random

        // Instance
        public static ObjectArrayTool instance;

        // Tool fields
        private Vector3 m_mouseIngamePosition;
        private int m_currentDraggedNode = -1;
        private bool m_isDraggingCentroid = false;

        private List<Vector3> m_bounds = new List<Vector3>(); // positions of points in game
        public List<ObjectWrapper> m_objectsToPlace = new List<ObjectWrapper>();
        private Vector3? m_centroid = null;

        // Constants
        Color kHoveredColor = new Color32(0, 181, 255, ObjectArray.selectionOpacity);
        Color kSelectedColor = new Color32(95, 166, 0, ObjectArray.selectionOpacity);

        // Utility functions
        public bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c){
            Barycentric s = new Barycentric(p, a, b, c);

            return s.u >= 0f && s.v >= 0f && s.w >= 0f;
        }
        public Vector2? GetPointInPolygon(PolygonCollider2D poly){

            return GetPointInPolygon(poly.points);
        }

        public Vector2? GetPointInPolygon(Vector2[] points){
            if (points.Length < 3) return null;

            Vector2 v = points[1]; // Picking the first point as 1 makes future calculations easier.
            Vector2 a = points[0];
            Vector2 b = points[2];

            if(points.Length == 3){
                // Since there is no way to have a concave triangle, we can make a miniscule optimization by simply calculating the centroid of the triangle.
                float ox = (v.x + a.x + b.x) / 3f;
                float oy = (v.y + a.y + b.y) / 3f;
                return new Vector2(ox, oy);
            }

            // Unfortunately, the ability to have concave shapes throws a monkey-wrench in our calculations. In order to find an arbitrary point p in a given polygon, we must use a four-step method:
            // 1. Pick a point v.
            // 2. Pick previous point a and succeding point b.
            // 3. Iterate over all other points: if a point "q" is inside triangle a-b-v then compute the distance from q to v. If the distance is a new minimum, save new q.
            // 4. Compute the midpoint of v and q. That point must be within the shape or at least satisfactorily close.
            // 5. If there is no q, the midpoint of a and b is within the shape.

            // We have already set aside v, a, and b. Now we need to do the rest of the calculations.
            // We will start at 3 as another optimization: 0 is a, 1 is v, and 2 is b so they do not need to be iterated over.
            Vector2 q = Vector2.zero;
            bool w = false;
            float d = float.MaxValue; // beginning maximum distance so that it can be overwritten with smaller values

            for (int i = 3; i < points.Length; i++){
                Vector2 r = points[i];
                if(IsPointInTriangle(r, a, b, v)){
                    float f = Vector2.Distance(r, v);
                    if (f < d){
                        d = f;
                        q = r;
                        w = true;
                    }
                }
            }

            // no q?
            if (w) {
                float ox1 = (a.x + b.x) / 2f;
                float oy1 = (a.y + b.y) / 2f;
            }

            Vector2 q0 = (Vector2)q;

            float ox2 = (v.x + q0.x) / 2f;
            float oy2 = (v.y + q0.y) / 2f;

            return new Vector2(ox2, oy2);
        }

        public bool IsPointInPolygon(Vector2[] polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y || polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y)
                {
                    if (polygon[i].x + (testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        // Unfortunately, to test whether a certain object position is necessary, we will need to implement boundingbox collision between the boundingbox and an arbitrary polygon. This is a two-step process:
        // 1: If any line segment of an arbitrary polygon intersects other line segments of another arbitrary shape, they are colliding.
        // 2: If the entire polygon is within the other arbitrary shape (very simple test) they are colliding.
        // Therefore, the following two functions are necessary:
        public bool IntersectLines(Vector2 A, Vector2 B, Vector2 C, Vector2 D){
            // Roughly based on Jason Cohen's answer from here with suggested changes by Elemental: https://stackoverflow.com/a/563275
            Vector2 E = B - A;
            Vector2 F = D - C;

            Vector2 P = new Vector2(-E.y, E.x);

            float h = Vector2.Dot(A - C, P) / Vector2.Dot(F, P);
            float g = Vector2.Dot(F, P) / Vector2.Dot(A - C, P);

            return (0 <= g && 1 >= g) && (0 <= h && 1 >= h);
        }

        // Works in all directions, to the detriment of performance
        public bool IsQuadInPolygon(Vector2 A, Vector2 B, Vector2 C, Vector2 D, Vector2[] polygon){
            for (int i = 0; i < polygon.Length; i++){
                Vector2 E = polygon[i];
                Vector2 F = polygon[(i + 1 == polygon.Length) ? 0 : i];

                // Last two cases may be redundant. I'll test this later.
                if (IntersectLines(A, B, E, F) || IntersectLines(B, C, E, F) || IntersectLines(C, D, E, F) || IntersectLines(D, A, E, F) || IntersectLines(A, C, E, F) || IntersectLines(B, D, E, F)) return true;
            }

            return false;
        }
        public Vector2 Rotate(Vector2 vector, Vector3 center, float th)
        {
            return Rotate(vector.x, vector.y, new Vector2(center.x, center.y), th);
        }
        public Vector2 Rotate(Vector2 vector, Vector2 center, float th){
            return Rotate(vector.x, vector.y, center, th);
        }
        public Vector2 Rotate(float x, float y, Vector2 c, float th){
            double r = Math.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y));
            double initialTheta = Math.Atan((y - c.y) / (x - c.x));

            // Rotated coordinates 
            float x2 = c.x + (float)(Math.Cos(initialTheta + th) * r);
            float y2 = c.y + (float)(Math.Sin(initialTheta + th) * r);

            return new Vector2(x2, y2);
        }

        // Tool overrides
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnToolUpdate(){

            // Base update
            base.OnToolUpdate();

            // Acquire raycast output and set position
            ElekRaycastUtil.RaycastTo(ObjectType.Position, out RaycastOutput output);
            m_mouseIngamePosition = output.m_hitPos;

            // Test if object is being dragged
            for (int i = 0; i < m_bounds.Count; i++){
                if(Vector3.Magnitude(m_mouseIngamePosition - m_bounds[i]) < ObjectArray.selectionRadius && Input.GetMouseButtonDown(0)){
                    if(m_currentDraggedNode == -1){
                        m_currentDraggedNode = i;
                    }
                }
            }


            if(m_currentDraggedNode != -1){

                // Update dragged object if it exists and we are not currently dragging the "centroid"
                if(!m_isDraggingCentroid) m_bounds[m_currentDraggedNode] = m_mouseIngamePosition;

                // If the centroid exists...
            }else if(m_centroid != null){
                Vector3 centroid = (Vector3)m_centroid;

                // If we're already dragging the centroid, update its position
                if(m_isDraggingCentroid){
                    m_centroid = m_mouseIngamePosition;

                    // If we aren't already dragging the centroid and we can drag it, set it as draggable
                }else if (Vector3.Magnitude(m_mouseIngamePosition - centroid) < ObjectArray.selectionRadius * 0.7f && Input.GetMouseButtonDown(0)){
                    m_isDraggingCentroid = true;
                }
            }


            if(Input.GetMouseButtonUp(0)){


                // Reset dragging state
                if(m_currentDraggedNode != -1){
                    m_currentDraggedNode = -1;
                    return;
                }

                if (m_isDraggingCentroid)
                {
                    m_isDraggingCentroid = false;
                    return;
                }

                m_bounds.Add(m_mouseIngamePosition);

                // recalculate centroid
                Debug.Log("c");
                Vector2[] projections = m_bounds.Select(point => new Vector2(point.x, point.z)).ToArray();
                if(m_centroid == null || !IsPointInPolygon(projections, new Vector2(((Vector3) m_centroid).x, ((Vector3) m_centroid).y))){
                    if(m_bounds.Count < 3){
                        m_centroid = null;
                    }
                    Vector2? pt = GetPointInPolygon(projections);
                    if (pt != null){
                        Vector2 unNullifiedPoint = (Vector2)pt;
                        float yPos = TerrainManager.instance.GetDetailHeight((int) unNullifiedPoint.x, (int) unNullifiedPoint.y);
                        Debug.Log(yPos);
                        m_centroid = new Vector3(unNullifiedPoint.x, m_bounds[0].y, unNullifiedPoint.y); // @TODO make aproper y position implementation
                    }
                }

                        
            }else if (Input.GetMouseButtonUp(1)){
                foreach(Vector3 position in m_bounds){
                    if(Vector3.Magnitude(m_mouseIngamePosition - position) < ObjectArray.selectionRadius){
                        m_bounds.Remove(position);
                        break;
                    }
                }
            }

            if(Input.GetKeyUp(KeyCode.K)){

                Debug.Log("Initialized object placement");
                // @TODO place this into separate function to prevent clutter!
                float decalW = 8f; // @TODO IMPLEMENT
                float decalH = 8f; // @TODO IMPLEMENT

                float angleTHETA = 128f;

                Vector3 center = (Vector3)m_centroid;

                Vector2[] bounds = m_bounds.Select(point => new Vector2(point.x, point.z)).ToArray();


                float xMin = m_bounds.Min(v => v.x);
                float zMin = m_bounds.Min(v => v.z);
                float xMax = m_bounds.Max(v => v.x);
                float zMax = m_bounds.Max(v => v.z);

                int xL = (int)Math.Floor((xMin - center.x) / decalW);
                int xR = (int)Math.Ceiling((xMax - center.x) / decalW);

                int zL = (int)Math.Floor((zMin - center.z) / decalH);
                int zR = (int)Math.Ceiling((zMax - center.z) / decalH);

                Debug.Log("Placing decals over area (" + xMin + ", " + zMin + "), (" + xMax + ", " + zMax + ")");
                Debug.Log("Iterating over value (" + xL + ", " + xR + "), (" + zL + ", " + zR + ")");


                for (int i = xL; i < xR; i++)
                {
                    for (int j = zL; j < zR; j++)
                    {
                        
                        // Create tiling at decal width and height
                        float x1 = center.x + i * decalW;
                        float z1 = center.z + j * decalH;

                        // Adjust these coordinates for rotation
                        Vector2 a = Rotate(x1, z1, center, angleTHETA); // top left
                        Vector2 b = Rotate(x1 + decalW, z1, center, angleTHETA); // top right
                        Vector2 c = Rotate(x1 + decalW, z1 + decalH, center, angleTHETA); // bottom right
                        Vector2 d = Rotate(x1, z1 + decalH, center, angleTHETA); // bottom left

                        float y = TerrainManager.instance.SampleOriginalRawHeightSmooth(a.x, a.y);
                        Debug.Log("Sampled height: " + y);

                        if (IsQuadInPolygon(a, b, c, d, bounds))
                        {
                            PropInfo t = PrefabCollection<PropInfo>.FindLoaded("Amusement Park Tiles 01");
                            Randomizer randomizer = new Randomizer();
                            if (Singleton<PropManager>.instance.CreateProp(out ushort _propID, ref randomizer, t, new Vector3(a.x, y, a.y), angleTHETA, true))
                            {
                                Debug.Log("Success!");
                            }
                        }
                    }
                }
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (!instance.enabled) return;

            for (int i = 0; i < m_bounds.Count; i++){
                Vector3 position = m_bounds[i];
                Vector3 previousPosition = i == 0 ? m_bounds[m_bounds.Count - 1] : m_bounds[i - 1];
                RenderManager.instance.OverlayEffect.DrawCircle(
                    cameraInfo, 
                    (Vector3.Magnitude(m_mouseIngamePosition - position) < ObjectArray.selectionRadius) ? kSelectedColor : kHoveredColor, 
                    position, ObjectArray.selectionRadius, position.y - 1f, position.y + 1f, true, true
                );
                Quad3 quad = new Quad3(position, position, previousPosition, previousPosition);
                RenderManager.instance.OverlayEffect.DrawQuad(cameraInfo, kHoveredColor, quad, Math.Min(position.y - 1f, previousPosition.y - 1f), Math.Max(position.y - 1f, previousPosition.y - 1f), true, true);
            }

            if(m_centroid != null){
                Vector3 centroid = (Vector3)m_centroid;
                RenderManager.instance.OverlayEffect.DrawCircle(
                    cameraInfo,
                    (Vector3.Magnitude(m_mouseIngamePosition - centroid) < ObjectArray.selectionRadius * 0.7f) ? kSelectedColor : kHoveredColor,
                    centroid, ObjectArray.selectionRadius * 0.7f, centroid.y - 1f, centroid.y + 1f, true, true
                );
            }
        }

    }
}
