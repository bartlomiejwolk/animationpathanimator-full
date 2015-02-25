﻿using System;
using System.Diagnostics.CodeAnalysis;
using ATP.ReorderableList;
using UnityEngine;

namespace ATP.AnimationPathTools {
    /// <summary>
    ///     Allows creating and drawing 3d paths using Unity's animation curves.
    /// </summary>
    [ExecuteInEditMode]
    public class AnimationPathBuilder : GameComponent {
        #region CONSTANTS

        /// <summary>
        ///     How many points should be drawn for one meter of a gizmo curve.
        /// </summary>
        public const int GizmoCurveSamplingFrequency = 20;

        #endregion CONSTANTS
        #region FIELDS

        public event EventHandler NodeAdded;

        public event EventHandler NodePositionChanged;

        public event EventHandler NodeRemoved;

        public event EventHandler NodeTimeChanged;

        public event EventHandler PathReset;

        #endregion FIELDS

        #region EDITOR

        /// <summary>
        ///     If true, advenced setting in the inspector will be folded out.
        /// </summary>
        [SerializeField]
#pragma warning disable 414
            private bool advancedSettingsFoldout;

#pragma warning restore 414

        /// <summary>
        ///     How many transforms should be created for 1 m of gizmo curve when
        ///     exporting nodes to transforms.
        /// </summary>
        /// <remarks>Exporting is implemented in <c>Editor</c> class.</remarks>
        [SerializeField]
#pragma warning disable 414
            private int exportSamplingFrequency = 5;

#pragma warning restore 414

        /// <summary>
        ///     Color of the gizmo curve.
        /// </summary>
        [SerializeField] private Color gizmoCurveColor = Color.yellow;

        [SerializeField] private AnimationPathBuilderHandleMode handleMode =
            AnimationPathBuilderHandleMode.MoveSingle;

        [SerializeField] private PathData pathData;

        /// <summary>
        ///     Styles for multiple GUI elements.
        /// </summary>
        [SerializeField] private GUISkin skin;

#pragma warning disable 0414
        [SerializeField] private AnimationPathBuilderTangentMode tangentMode =
            AnimationPathBuilderTangentMode.Smooth;
#pragma warning restore 0414

        #endregion EDITOR

        #region PUBLIC PROPERTIES

        /// <summary>
        ///     Color of the gizmo curve.
        /// </summary>
        public Color GizmoCurveColor {
            get { return gizmoCurveColor; }
            set { gizmoCurveColor = value; }
        }

        public AnimationPathBuilderHandleMode HandleMode {
            get { return handleMode; }
            set { handleMode = value; }
        }

        /// <summary>
        ///     Number of keys in an animation curve.
        /// </summary>
        public int NodesNo {
            get { return pathData.AnimatedObjectPath.KeysNo; }
        }

        public PathData PathData {
            get { return pathData; }
            set { pathData = value; }
        }

        public GUISkin Skin {
            get { return skin; }
        }

        public AnimationPathBuilderTangentMode TangentMode {
            get { return tangentMode; }
            set { tangentMode = value; }
        }

        #endregion PUBLIC PROPERTIES

        #region UNITY MESSAGES

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private void Awake() {
            // Load default skin.
            skin = Resources.Load("GUISkin/default") as GUISkin;
        }

        private void OnDestroy() {
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private void OnDrawGizmosSelected() {
            DrawGizmoCurve();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private void OnEnable() {
            PathReset += this_PathReset;
        }

        private void this_PathReset(object sender, EventArgs eventArgs) {
            // Change handle mode to MoveAll.
            handleMode = AnimationPathBuilderHandleMode.MoveAll;
        }

        #endregion UNITY MESSAGES

        #region EVENT INVOCATORS

        public virtual void this_PathReset() {
            var handler = PathReset;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnNodeAdded() {
            var handler = NodeAdded;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnNodePositionChanged() {
            var handler = NodePositionChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnNodeRemoved() {
            var handler = NodeRemoved;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnNodeTimeChanged() {
            var handler = NodeTimeChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion EVENT INVOCATORS

        #region PUBLIC METHODS

        public void ChangeNodeTimestamp(
            int keyIndex,
            float newTimestamp) {
            pathData.AnimatedObjectPath.ChangeNodeTimestamp(keyIndex, newTimestamp);
            OnNodeTimeChanged();
        }

        public void CreateNode(float timestamp, Vector3 position) {
            pathData.AnimatedObjectPath.CreateNewNode(timestamp, position);
            OnNodeAdded();
        }

        public void CreateNodeAtTime(float timestamp) {
            pathData.AnimatedObjectPath.AddNodeAtTime(timestamp);
            OnNodeAdded();
        }

        public void DistributeTimestamps() {
            // Calculate path curved length.
            var pathLength =
                pathData.AnimatedObjectPath.CalculatePathCurvedLength(
                    GizmoCurveSamplingFrequency);

            // Calculate time for one meter of curve length.
            var timeForMeter = 1/pathLength;

            // Helper variable.
            float prevTimestamp = 0;

            // For each node calculate and apply new timestamp.
            for (var i = 1; i < NodesNo - 1; i++) {
                // Calculate section curved length.
                var sectionLength =
                    pathData.AnimatedObjectPath.CalculateSectionCurvedLength(
                        i - 1,
                        i,
                        GizmoCurveSamplingFrequency);

                // Calculate time interval for the section.
                var sectionTimeInterval = sectionLength*timeForMeter;

                // Calculate new timestamp.
                var newTimestamp = prevTimestamp + sectionTimeInterval;

                // Update previous timestamp.
                prevTimestamp = newTimestamp;

                // NOTE When nodes on the scene overlap, it's possible that new
                // timestamp is > 0, which is invalid.
                if (newTimestamp > 1) break;

                // Update node timestamp.
                ChangeNodeTimestamp(i, newTimestamp);
            }
        }

        public Vector3[] GetNodeGlobalPositions() {
            var nodePositions = GetNodePositions();

            for (var i = 0; i < nodePositions.Length; i++) {
                // Convert each position to global coordinate.
                nodePositions[i] = transform.TransformPoint(nodePositions[i]);
            }

            return nodePositions;
        }

        public Vector3 GetNodePosition(int nodeIndex) {
            return pathData.AnimatedObjectPath.GetVectorAtKey(nodeIndex);
        }

        public Vector3[] GetNodePositions(bool globalPositions = false) {
            var result = new Vector3[NodesNo];

            for (var i = 0; i < NodesNo; i++) {
                // Get node 3d position.
                result[i] = pathData.AnimatedObjectPath.GetVectorAtKey(i);

                // Convert position to global coordinate.
                if (globalPositions) {
                    result[i] = transform.TransformPoint(result[i]);
                }
            }

            return result;
        }

        public float GetNodeTimestamp(int nodeIndex) {
            return pathData.AnimatedObjectPath.GetTimeAtKey(nodeIndex);
        }

        public float[] GetNodeTimestamps() {
            // Output array.
            var result = new float[NodesNo];

            // For each key..
            for (var i = 0; i < NodesNo; i++) {
                // Get key time.
                result[i] = pathData.AnimatedObjectPath.GetTimeAtKey(i);
            }

            return result;
        }

        public Vector3 GetVectorAtTime(float timestamp) {
            return pathData.AnimatedObjectPath.GetVectorAtTime(timestamp);
        }

        public void MoveNodeToPosition(int nodeIndex, Vector3 position) {
            pathData.AnimatedObjectPath.MovePointToPosition(nodeIndex, position);
            OnNodePositionChanged();
        }

        public void OffsetNodePositions(Vector3 moveDelta) {
            // For each node..
            for (var i = 0; i < NodesNo; i++) {
                // Old node position.
                var oldPosition = GetNodePosition(i);
                // New node position.
                var newPosition = oldPosition + moveDelta;
                // Update node positions.
                pathData.AnimatedObjectPath.MovePointToPosition(i, newPosition);

                OnNodePositionChanged();
            }
        }

        public void RemoveNode(int nodeIndex) {
            pathData.AnimatedObjectPath.RemoveNode(nodeIndex);
            OnNodeRemoved();
        }

        public void SetNodesLinear() {
            for (var i = 0; i < 3; i++) {
                Utilities.SetCurveLinear(pathData.AnimatedObjectPath[i]);
            }
        }

        public void SetNodeTangents(int index, Vector3 inOutTangent) {
            pathData.AnimatedObjectPath.ChangePointTangents(index, inOutTangent);
        }

        public void SetWrapMode(WrapMode wrapMode) {
            pathData.AnimatedObjectPath.SetWrapMode(wrapMode);
        }

        /// <summary>
        ///     Smooth tangents in all nodes in all animation curves.
        /// </summary>
        /// <param name="weight">Weight to be applied to the tangents.</param>
        public void SmoothAllNodeTangents(float weight = 0) {
            // For each key..
            for (var j = 0; j < NodesNo; j++) {
                // Smooth in and out tangents.
                pathData.AnimatedObjectPath.SmoothPointTangents(j);
            }
        }

        public void SmoothSingleNodeTangents(int nodeIndex) {
            pathData.AnimatedObjectPath.SmoothPointTangents(nodeIndex);
        }

        #endregion PUBLIC METHODS

        #region PRIVATE METHODS
        public void RemoveAllNodes() {
            var nodesNo = NodesNo;
            for (var i = 0; i < nodesNo; i++) {
                // NOTE After each removal, next node gets index 0.
                RemoveNode(0);
            }
        }


        private void DrawGizmoCurve() {
            // Return if path asset is not assigned.
            if (pathData == null) return;

            // Get transform component.
            var transform = GetComponent<Transform>();

            // Get path points.
            var points = pathData.AnimatedObjectPath.SamplePathForPoints(
                GizmoCurveSamplingFrequency);

            // Convert points to global coordinates.
            var globalPoints = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++) {
                globalPoints[i] = transform.TransformPoint(points[i]);
            }

            // There must be at least 3 points to draw a line.
            if (points.Count < 3) return;

            Gizmos.color = gizmoCurveColor;

            // Draw curve.
            for (var i = 0; i < points.Count - 1; i++) {
                Gizmos.DrawLine(globalPoints[i], globalPoints[i + 1]);
            }
        }

        #endregion PRIVATE METHODS
    }
}