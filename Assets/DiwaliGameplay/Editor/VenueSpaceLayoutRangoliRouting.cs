#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiwaliGameplay.Editor
{
    public static class VenueSpaceLayoutRangoliRouting
    {
        [MenuItem("Diwali Island/Gameplay/Organize Rangoli and Routing work areas")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Apply the venue layout in Edit mode.");
            var game = UnityEngine.Object.FindFirstObjectByType<RangoliGame>();
            var routing = UnityEngine.Object.FindFirstObjectByType<RoutingGame>();
            var venue = GameObjectUtility.FindExact("Rangoli - Approved Warm Palette");
            if (!game || !routing || !venue || !game.preserveAuthoredLayout)
                throw new InvalidOperationException("The active authored Rangoli and routing games are required.");
            var board = Required(venue, "Cube.005");
            var table = Required(venue, "Cube.001");
            var rack = Required(venue, "Cube");
            var pavilion = Required(venue, "Cube.002");
            var floor = Required(venue, "Plane");
            if (game.pieces == null || game.pieces.Length != 5 || game.pieces.Any(p => !p) ||
                game.sockets == null || game.sockets.Length != 5 || game.sockets.Any(s => !s))
                throw new InvalidOperationException("All five authored pieces and sockets must be assigned.");
            if (game.pieces.Any(p => !p.gameObject.activeInHierarchy || !p.transform.IsChildOf(venue)) ||
                game.sockets.Any(s => !s.gameObject.activeInHierarchy))
                throw new InvalidOperationException("The active authored rack pieces and sockets could not be identified safely.");

            var beforeBoard = GameObjectUtility.BoundsOf(board);
            var beforeRack = GameObjectUtility.BoundsOf(rack);
            var structure = GameObjectUtility.BoundsOf(pavilion);
            var ground = GameObjectUtility.BoundsOf(floor);
            var front = Vector3.ProjectOnPlane(-venue.right, Vector3.up).normalized;
            var side = Vector3.Cross(Vector3.up, front);
            float boardHalf = Extent(beforeBoard, side);
            float rackHalf = Extent(beforeRack, side);
            const float gap = .28f;
            float frontEdge = Vector3.Dot(structure.center, front) + Extent(structure, front);
            float workDepth = frontEdge + Extent(beforeBoard, front) + .08f;
            float centre = Vector3.Dot(structure.center, side);
            var boardTarget = beforeBoard.center;
            boardTarget += front * (workDepth - Vector3.Dot(boardTarget, front));
            boardTarget += side * (centre + rackHalf + gap * .5f - Vector3.Dot(boardTarget, side));
            var rackTarget = beforeRack.center;
            rackTarget += front * (workDepth - Vector3.Dot(rackTarget, front));
            rackTarget += side * (centre - boardHalf - gap * .5f - Vector3.Dot(rackTarget, side));
            rackTarget.y += ground.max.y + .95f - beforeRack.max.y;
            var tableDelta = boardTarget - beforeBoard.center;
            var rackDelta = rackTarget - beforeRack.center;

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize Rangoli work area");
            // Sockets and grabbed pivots live outside their visible support meshes.
            foreach (var part in new[] { table, board }.Concat(game.sockets.Select(s => s.transform))) Move(part, tableDelta);
            foreach (var part in new[] { rack }.Concat(game.pieces.Select(p => p.transform))) Move(part, rackDelta);
            var rackBounds = GameObjectUtility.BoundsOf(rack);
            float supportHeight = rackBounds.min.y - ground.max.y;
            if (supportHeight > .02f)
            {
                var support = GameObjectUtility.FindUnder(venue, "Rangoli rack working-height support");
                if (!support)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Rangoli rack working-height support";
                    Undo.RegisterCreatedObjectUndo(go, "Support the raised Rangoli rack");
                    support = go.transform;
                    support.SetParent(venue, true);
                }
                Undo.RecordObject(support, "Seat rack support on the floor");
                support.SetPositionAndRotation(new Vector3(rackBounds.center.x, ground.max.y + supportHeight * .5f, rackBounds.center.z), Quaternion.identity);
                support.localScale = new Vector3(rackBounds.size.x / support.parent.lossyScale.x, supportHeight / support.parent.lossyScale.y, rackBounds.size.z / support.parent.lossyScale.z);
                support.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/DiwaliGameplay/Rangoli/Rack pedestal stone.mat");
            }
            Undo.CollapseUndoOperations(undo);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);

            var afterBoard = GameObjectUtility.BoundsOf(board);
            var afterRack = GameObjectUtility.BoundsOf(rack);
            var menuFront = Vector3.ProjectOnPlane(afterBoard.center - ground.center, Vector3.up).normalized;
            var menuSide = Vector3.Cross(Vector3.up, menuFront);
            var runtimeMenu = afterBoard.center + menuSide * (Extent(afterBoard, menuSide) + .65f) + menuFront * .35f;
            runtimeMenu.y = ground.max.y + 1.5f;
            Debug.Log($"Rangoli layout: board/rack centre distance {Vector3.Distance(beforeBoard.center, beforeRack.center):F2} -> {Vector3.Distance(afterBoard.center, afterRack.center):F2} m; side edge gap {gap:F2} m; rack top {beforeRack.max.y:F2} -> {afterRack.max.y:F2} m. Board centre {afterBoard.center:F3}; rack centre {afterRack.center:F3}; runtime menu {runtimeMenu:F3}. Axes, scales, socket offsets and gameplay references preserved. Routing work area retained: console already in front of lamps, rack beside console, menu on the opposite side. Scene left unsaved.");
        }

        static Transform Required(Transform parent, string name)
        {
            var part = GameObjectUtility.FindUnder(parent, name);
            if (!part || !part.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Missing active authored Rangoli part: " + name);
            return part;
        }

        static float Extent(Bounds bounds, Vector3 axis) => Vector3.Dot(bounds.extents,
            new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)));

        static void Move(Transform part, Vector3 delta)
        {
            if (delta.sqrMagnitude < .00000001f) return;
            Undo.RecordObject(part, "Move Rangoli station cluster");
            part.position += delta;
            EditorUtility.SetDirty(part);
            var body = part.GetComponent<Rigidbody>();
            if (body)
            {
                Undo.RecordObject(body, "Move Rangoli item body");
                body.position = part.position;
                EditorUtility.SetDirty(body);
            }
        }
    }
}
#endif
