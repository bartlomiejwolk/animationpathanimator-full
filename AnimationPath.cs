﻿using ATP.ReorderableList;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ATP.AnimationPathTools {

    /// <summary>
    /// Allows creating and drawing 3d paths using Unity's animation curves.
    /// </summary>
    /// <remarks>
    /// - It uses array of three AnimationCurve objects to construct the path.
    /// - Class fields are updated in <c>AnimationPath_PathChanged</c> event
    /// handler. <c>CurvesChanged</c> event is called after animation curves
    /// inside <c>_animationCurves</c> are changed.
    /// </remarks>
    [ExecuteInEditMode]
    public class AnimationPath : GameComponent {

        #region Constants

        /// <summary>
        /// Key shortcut to enable handles mode.
        /// </summary>
        /// <remarks>
        /// Handles mode will change only while key is pressed.
        /// </remarks>
        public const KeyCode HandlesModeKey = KeyCode.J;

        /// <summary>
        /// Key shortcut to toggle movement mode.
        /// </summary>
        /// <remarks>
        /// Movement mode will change only while key is pressed.
        /// </remarks>
        public const KeyCode MoveAllKey = KeyCode.H;

        /// <summary>
        /// How many points should be drawn for one meter of a gizmo curve.
        /// </summary>
        public const int GizmoCurveSamplingFrequency = 20;
        #endregion Constants
        #region Fields

        /// <summary>
        /// Animation curves that make the animation path.
        /// </summary>
        [SerializeField]
        private AnimationPathCurves _animationCurves;
        #endregion Fields

        #region Editor

        /// <summary>
        /// If true, advenced setting in the inspector will be folded out.
        /// </summary>
        [SerializeField]
#pragma warning disable 414
        private bool _advancedSettingsFoldout;
#pragma warning restore 414

        /// <summary>
        /// How many transforms should be created for 1 m of gizmo curve when
        /// exporting nodes to transforms.
        /// </summary>
        /// <remarks>Exporting is implemented in <c>Editor</c> class.</remarks>
        [SerializeField]
#pragma warning disable 414
        private int _exportSamplingFrequency = 5;
#pragma warning restore 414

        /// <summary>
        /// Color of the gizmo curve.
        /// </summary>
        [SerializeField]
        private Color _gizmoCurveColor = Color.yellow;

        /// <summary>
        /// If "Move All" mode is enabled.
        /// </summary>
        [SerializeField]
        private bool _moveAllMode;

        [SerializeField]
        private bool _sceneControls = true;

        /// <summary>
        /// Styles for multiple GUI elements.
        /// </summary>
        [SerializeField]
        private GUISkin _skin;

#pragma warning disable 0414

        /// <summary>
        /// If enabled, on-scene handles will be use to change node's in/out
        /// tangents.
        /// </summary>
        [SerializeField]
        private bool _tangentMode;
#pragma warning restore 0414

        #endregion Editor

        #region PUBLIC PROPERTIES

        public AnimationPathCurves AnimationCurves {
            get { return _animationCurves; }
        }

        public bool MoveAllMode {
            get { return _moveAllMode; }
            set { _moveAllMode = value; }
        }

        /// <summary>
        /// Number of keys in an animation curve.
        /// </summary>
        public int NodesNo {
            get { return _animationCurves.KeysNo; }
        }

        public bool SceneControls {
            get { return _sceneControls; }
            set { _sceneControls = value; }
        }

        public GUISkin Skin {
            get { return _skin; }
        }
        public bool TangentMode {
            get { return _tangentMode; }
            set { _tangentMode = value; }
        }

        /// <summary>
        /// Color of the gizmo curve.
        /// </summary>
        public Color GizmoCurveColor {
            get { return _gizmoCurveColor; }
            set { _gizmoCurveColor = value; }
        }

        #endregion PUBLIC PROPERTIES

        #region Unity Messages

        private void Awake() {
            // Load default skin.
            _skin = Resources.Load("GUISkin/default") as GUISkin;
        }

        private void OnDrawGizmosSelected() {
            DrawGizmoCurve();
        }

        private void OnEnable() {
            // Instantiate class field.
            if (_animationCurves == null) {
                _animationCurves =
                    ScriptableObject.CreateInstance<AnimationPathCurves>();
            }
        }

        #endregion Unity Messages

        #region Public Methods

        public void CreateNodeAtTime(float timestamp) {
            _animationCurves.AddNodeAtTime(timestamp);
        }

        public float CalculatePathCurvedLength(int samplingFrequency) {
            float pathLength = 0;

            for (var i = 0; i < NodesNo - 1; i++) {
                pathLength += CalculateSectionCurvedLength(
                    i,
                    i + 1,
                    GizmoCurveSamplingFrequency);
            }

            return pathLength;
        }

        /// <summary>
        /// Calculate path length using shortest path between each node.
        /// </summary>
        /// <param name="curves">
        /// Array of three animation curves, each of them represents one of
        /// three 3d axis. \return Length of the curve in meters.
        /// </param>
        /// <returns>Path length in meters.</returns>
        public float CalculatePathLinearLength() {
            // Result distance.
            float dist = 0;

            // For each node (exclude the first one)..
            for (int i = 0; i < _animationCurves.KeysNo - 1; i++) {
                dist += CalculateSectionLinearLength(i, i + 1);
            }

            return dist;
        }

        public float CalculateSectionCurvedLength(
                    int firstNodeIndex,
                    int secondNodeIndex,
                    int samplingFrequency) {

            // Sampled points.
            List<Vector3> points;

            // Result path length.
            float pathLength = 0;

            points = SampleSectionForPoints(
                firstNodeIndex,
                secondNodeIndex,
                samplingFrequency);

            for (var i = 1; i < points.Count; i++) {
                pathLength += Vector3.Distance(points[i - 1], points[i]);
            }

            return pathLength;
        }

        public float CalculateSectionLinearLength(
            int firstNodeIndex,
            int secondNodeIndex) {

            Vector3 firstNodePosition =
                _animationCurves.GetVectorAtKey(firstNodeIndex);
            Vector3 secondNodePosition =
                _animationCurves.GetVectorAtKey(secondNodeIndex);

            float sectionLength =
                Vector3.Distance(firstNodePosition, secondNodePosition);

            return sectionLength;
        }

        public void ChangeNodeTangents(int index, Vector3 inOutTangent) {
            _animationCurves.ChangePointTangents(index, inOutTangent);
        }

        public void ChangeNodeTimestamp(
                            int keyIndex,
                            float newTimestamp) {

            _animationCurves.ChangePointTimestamp(keyIndex, newTimestamp);
        }

        public void CreateNode(float timestamp, Vector3 position) {
            _animationCurves.CreateNewPoint(timestamp, position);
        }

        private void DrawGizmoCurve() {
            List<Vector3> points = SamplePathForPoints(GizmoCurveSamplingFrequency);

            if (points.Count < 3) return;

            // Draw curve.
            for (int i = 0; i < points.Count - 1; i++) {
                Gizmos.color = _gizmoCurveColor;
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }

        public Vector3 GetNodePosition(int nodeIndex) {
            return _animationCurves.GetVectorAtKey(nodeIndex);
        }

        public Vector3[] GetNodePositions() {
            Vector3[] result = new Vector3[NodesNo];

            for (int i = 0; i < NodesNo; i++) {
                // Get node 3d position.
                result[i] = _animationCurves.GetVectorAtKey(i);
            }

            return result;
        }

        public void DistributeTimestamps() {
            // Calculate path curved length.
            float pathLength = CalculatePathCurvedLength(
                GizmoCurveSamplingFrequency);
            // Calculate time for one meter of curve length.
            float timeForMeter = 1 / pathLength;
            // Helper variable.
            float prevTimestamp = 0;

            // For each node calculate and apply new timestamp.
            for (var i = 1; i < NodesNo - 1; i++) {
                // Calculate section curved length.
                float sectionLength = CalculateSectionCurvedLength(
                    i - 1,
                    i,
                    GizmoCurveSamplingFrequency);
                // Calculate time interval.
                float sectionTimeInterval = sectionLength * timeForMeter;
                // Calculate new timestamp.
                float newTimestamp = prevTimestamp + sectionTimeInterval;
                // Update previous timestamp.
                prevTimestamp = newTimestamp;

                // Update node timestamp.
                ChangeNodeTimestamp(i, newTimestamp);
            }
        }

        public float GetNodeTimestamp(int nodeIndex) {
            return _animationCurves.GetTimeAtKey(nodeIndex);
        }

        public float[] GetNodeTimestamps() {
            // Output array.
            float[] result = new float[NodesNo];

            // For each key..
            for (int i = 0; i < NodesNo; i++) {
                // Get key time.
                result[i] = _animationCurves.GetTimeAtKey(i);
            }

            return result;
        }

        public Vector3 GetVectorAtTime(float timestamp) {
            return _animationCurves.GetVectorAtTime(timestamp);
        }

        public void MoveAllNodes(Vector3 moveDelta) {
            // For each node..
            for (int i = 0; i < NodesNo; i++) {
                // Old node position.
                Vector3 oldPosition = GetNodePosition(i);
                // New node position.
                Vector3 newPosition = oldPosition + moveDelta;
                // Update node positions.
                _animationCurves.MovePointToPosition(i, newPosition);
            }
        }

        public void MoveNodeToPosition(int nodeIndex, Vector3 position) {
            _animationCurves.MovePointToPosition(nodeIndex, position);
        }
        public void RemoveNode(int nodeIndex) {
            _animationCurves.RemovePoint(nodeIndex);
        }

        /// <summary>
        /// Sample Animation Path for 3d points.
        /// </summary>
        /// <param name="samplingFrequency">
        /// How many 3d points should be extracted from the path for 1 m of its
        /// linear length.
        /// </param>
        /// <param name="pathLength">
        /// Length of the Animation Path in meters.
        /// </param>
        /// <returns>Array of 3d points.</returns>
        public List<Vector3> SamplePathForPoints(int samplingFrequency) {
            List<Vector3> points = new List<Vector3>();

            // Call reference overload.
            SamplePathForPoints(samplingFrequency, ref points);

            return points;
        }

        public void SamplePathForPoints(
                            int samplingFrequency,
                            ref List<Vector3> points) {

            float linearPathLength = CalculatePathLinearLength();

            // Calculate amount of points to extract.
            int samplingRate = (int)(linearPathLength * samplingFrequency);

            // NOTE Cannot do any sampling if sampling rate is less than 1.
            if (samplingRate < 1) return;

            // Used to read values from animation curves.
            float time = 0;

            // Time step between each point.
            float timestep = 1f / samplingRate;

            // Clear points list.
            points.Clear();

            // Fill points array with 3d points.
            for (int i = 0; i < samplingRate + 1; i++) {
                // Calculate single point.
                Vector3 point = _animationCurves.GetVectorAtTime(time);

                // Construct 3d point from animation curves at a given time.
                points.Add(point);

                // Time goes towards 1.
                time += timestep;
            }
        }

        public List<Vector3> SampleSectionForPoints(
                    int firstNodeIndex,
                    int secondNodeIndex,
                    float samplingFrequency) {

            List<Vector3> points = new List<Vector3>();

            SampleSectionForPoints(
                firstNodeIndex,
                secondNodeIndex,
                samplingFrequency,
                ref points);

            return points;
        }

        public void SampleSectionForPoints(
                    int firstNodeIndex,
                    int secondNodeIndex,
                    float samplingFrequency,
                    ref List<Vector3> points) {

            float linearPathLength = CalculatePathLinearLength();

            // Throw exception when there's nothing to draw.
            if (linearPathLength == 0) {
                throw new Exception("Animation path length is 0. At least " +
                        "two keys in a curve must differ in value.");
            }

            float sectionLinearLength = CalculateSectionLinearLength(
                firstNodeIndex,
                secondNodeIndex);

            // Calculate amount of points to extract.
            int samplingRate = (int)(sectionLinearLength * samplingFrequency);

            float firstNodeTime =
                _animationCurves.GetTimeAtKey(firstNodeIndex);
            float secondNodeTime =
                _animationCurves.GetTimeAtKey(secondNodeIndex);

            float timeInterval = secondNodeTime - firstNodeTime;

            // Used to read values from animation curves.
            float time = firstNodeTime;

            // Time step between each point.
            float timestep = timeInterval / samplingRate;

            // Clear points list.
            points.Clear();

            // Fill points array with 3d points.
            for (int i = 0; i < samplingRate + 1; i++) {
                // Calculate single point.
                Vector3 point = _animationCurves.GetVectorAtTime(time);

                // Construct 3d point from animation curves at a given time.
                points.Add(point);

                // Time goes towards 1.
                time += timestep;
            }
        }
       
        public void SetCurveLinear(AnimationCurve curve) {
            for (int i = 0; i < curve.keys.Length; ++i) {
                float intangent = 0;
                float outtangent = 0;
                bool intangent_set = false;
                bool outtangent_set = false;
                Vector2 point1;
                Vector2 point2;
                Vector2 deltapoint;
                Keyframe key = curve[i];

                if (i == 0) {
                    intangent = 0; intangent_set = true;
                }

                if (i == curve.keys.Length - 1) {
                    outtangent = 0; outtangent_set = true;
                }

                if (!intangent_set) {
                    point1.x = curve.keys[i - 1].time;
                    point1.y = curve.keys[i - 1].value;
                    point2.x = curve.keys[i].time;
                    point2.y = curve.keys[i].value;

                    deltapoint = point2 - point1;
                    intangent = deltapoint.y / deltapoint.x;
                }

                if (!outtangent_set) {
                    point1.x = curve.keys[i].time;
                    point1.y = curve.keys[i].value;
                    point2.x = curve.keys[i + 1].time;
                    point2.y = curve.keys[i + 1].value;

                    deltapoint = point2 - point1;
                    outtangent = deltapoint.y / deltapoint.x;
                }

                key.inTangent = intangent;
                key.outTangent = outtangent;

                curve.MoveKey(i, key);
            }
        }

        public void SetNodesLinear() {
            for (int i = 0; i < 3; i++) {
                SetCurveLinear(_animationCurves[i]);
            }
        }

        /// <summary>
        /// Smooth all tangents in the Animation Curves.
        /// </summary>
        /// <param name="weight">Weight to be applied to the tangents.</param>
        public void SmoothNodesTangents(float weight = 0) {
            // For each key..
            for (int j = 0; j < NodesNo; j++) {
                // Smooth in and out tangents.
                _animationCurves.SmoothPointTangents(j);
            }
        }

        public void SmoothNodeTangents(int nodeIndex) {
            _animationCurves.SmoothPointTangents(nodeIndex);
        }
        #endregion Public Methods
    }
}