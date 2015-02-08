﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ATP.AnimationPathTools {

    [CustomEditor(typeof(AnimationPathAnimator))]
    public class AnimatorEditor : Editor {

        #region CONSTANTS
        private const float ArcHandleRadius = 1f;
        private const float RotationHandleSize = 0.25f;
        #endregion
        #region FIELDS

        /// <summary>
        /// If modifier is currently pressed.
        /// </summary>
        private bool modKeyPressed;

        /// <summary>
        /// Reference to target script.
        /// </summary>
        private AnimationPathAnimator script;

        #endregion FIELDS

        #region SERIALIZED PROPERTIES

        protected SerializedProperty drawRotationHandle;
        private SerializedProperty animatedObject;
        private SerializedProperty animatedObjectPath;
        private SerializedProperty animTimeRatio;
        private SerializedProperty duration;
        private SerializedProperty easeAnimationCurve;
        private SerializedProperty lookForwardCurve;
        private SerializedProperty followedObject;
        private SerializedProperty followedObjectPath;
        private SerializedProperty rotationSpeed;
        private SerializedProperty tiltingCurve;
        private SerializedProperty lookForwardMode;
        private SerializedProperty displayEaseHandles;

        #endregion SERIALIZED PROPERTIES

        #region UNITY MESSAGES

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.Slider(
                new GUIContent(
                    "Animation Time",
                    "Current animation time."),
                    animTimeRatio.floatValue,
                    0,
                    1);

            EditorGUILayout.PropertyField(
                duration,
                new GUIContent(
                    "Duration",
                    "Duration of the animation in seconds."));

            EditorGUILayout.PropertyField(
                rotationSpeed,
                new GUIContent(
                    "Rotation Speed",
                    "Controls how much time (in seconds) it'll take the " +
                    "animated object to finish rotation towards followed target."));

            EditorGUILayout.PropertyField(
                easeAnimationCurve,
                new GUIContent(
                    "Ease Curve",
                    "Use it to control speed of the animated object."));

            EditorGUILayout.PropertyField(
                tiltingCurve,
                new GUIContent(
                    "Tilting Curve",
                    "Use it to control tilting of the animated object."));

            //EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            lookForwardMode.boolValue = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Look Forward",
                    "Ignore target object and look ahead."),
                    lookForwardMode.boolValue,
                    GUILayout.Width(116));

            EditorGUILayout.PropertyField(
                lookForwardCurve,
                new GUIContent(
                    "",
                    "Use it to control how far in time animated object will " +
                    "be looking ahead on its path."));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(displayEaseHandles);
            EditorGUILayout.PropertyField(drawRotationHandle);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(
                animatedObject,
                new GUIContent(
                    "Animated Object",
                    "Object to animate."));

            EditorGUILayout.PropertyField(
                animatedObjectPath,
                new GUIContent(
                    "Animated Object Path",
                    "Path used to animate object."));

            EditorGUILayout.PropertyField(
                followedObject,
                new GUIContent(
                    "Target Object",
                    "Object that the animated object will be looking at."));

            EditorGUILayout.PropertyField(
                followedObjectPath,
                new GUIContent(
                    "Target Object Path",
                    "Path for the followed object."));

            //EditorGUILayout.Space();

            //if (GUILayout.Button(new GUIContent("Create Target", ""))) {
            //    script.CreateTargetGO();
            //}

            // Save changes.
            serializedObject.ApplyModifiedProperties();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private void OnEnable() {
            // Get target script reference.
            script = (AnimationPathAnimator)target;

            // Initialize serialized properties.
            duration = serializedObject.FindProperty("duration");
            rotationSpeed = serializedObject.FindProperty("rotationSpeed");
            animTimeRatio = serializedObject.FindProperty("animTimeRatio");
            easeAnimationCurve = serializedObject.FindProperty("easeCurve");
            tiltingCurve = serializedObject.FindProperty("tiltingCurve");
            lookForwardCurve = serializedObject.FindProperty("lookForwardCurve");
            animatedObject = serializedObject.FindProperty("animatedObject");
            animatedObjectPath = serializedObject.FindProperty("animatedObjectPath");
            followedObject = serializedObject.FindProperty("followedObject");
            followedObjectPath = serializedObject.FindProperty("followedObjectPath");
            lookForwardMode = serializedObject.FindProperty("lookForwardMode");
            displayEaseHandles = serializedObject.FindProperty("displayEaseHandles");
            drawRotationHandle = serializedObject.FindProperty("drawRotationHandle");
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private void OnSceneGUI() {
            if (Event.current.type == EventType.ValidateCommand
                && Event.current.commandName == "UndoRedoPerformed") {

                script.UpdateEaseCurve();
            }

            serializedObject.Update();

            // Update modifier key state.
            UpdateModifierKey();

            // Change current animation time with arrow keys.
            ChangeTimeWithArrowKeys();

            // Save changes
            serializedObject.ApplyModifiedProperties();

            HandleDrawingForwardPointGizmo();
            HandleDrawingTargetGizmo();
            HandleDrawingEaseHandles();
            HandleDrawingRotationHandle();

            script.UpdateAnimation();
        }
        #endregion UNITY MESSAGES

        #region DRAWING HANDLERS
        private void HandleDrawingRotationHandle() {
            if (!drawRotationHandle.boolValue) return;

            // Callback to call when node rotation is changed.
            Action<float, Vector3> callbackHandler =
                DrawRotationHandlesCallbackHandler;

            // Draw handles.
            DrawRotationHandle(callbackHandler);
        }

        private void HandleDrawingForwardPointGizmo() {
            if (!lookForwardMode.boolValue) return;

            var targetPos = script.GetForwardPoint();
            // TODO Create class field with this style.
            var style = new GUIStyle {
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold,
            };

            Handles.Label(targetPos, "Point", style);
        }

        private void HandleDrawingTargetGizmo() {
            if (followedObject.objectReferenceValue == null) return;

            var targetPos =
                ((Transform)followedObject.objectReferenceValue).position;
            // TODO Create class field with this style.
            var style = new GUIStyle {
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold,
            };

            Handles.Label(targetPos, "Target", style);

        }

        private void HandleDrawingEaseHandles() {
            if (!displayEaseHandles.boolValue) return;

            Action<int, float> callbackHandler =
                DrawEaseHandlesCallbackHandler;

            DrawEaseHandles(callbackHandler);
        }
        #endregion

        #region DRAWING METHODS
        private void DrawEaseHandles(Action<int, float> callback) {
            // Get AnimationPath node positions.
            var nodePositions = script.AnimatedObjectPath.GetNodePositions();

            // Get ease curve timestamps.
            var easeTimestamps = new float[script.EaseCurve.length];
            for (var i = 0; i < script.EaseCurve.length; i++) {
                easeTimestamps[i] = script.EaseCurve.keys[i].time;
            }

            for (var i = 1; i < nodePositions.Length - 1; i++) {
                var easeTimestamp = easeTimestamps[i];
                var arcValue = easeTimestamp * 360f;
                var handleSize = HandleUtility.GetHandleSize(nodePositions[i]);
                var arcHandleSize = handleSize * ArcHandleRadius;

                // TODO Create const.
                Handles.color = Color.red;

                Handles.DrawWireArc(
                    nodePositions[i],
                    Vector3.up,
                    // Make the arc simetrical on the left and right
                    // side of the object.
                    Quaternion.AngleAxis(
                    //-arcValue / 2,
                        0,
                        Vector3.up) * Vector3.forward,
                    arcValue,
                    arcHandleSize);

                // TODO Create const.
                Handles.color = Color.red;

                // TODO Create constant.
                var scaleHandleSize = handleSize * 1.5f;
                float newArcValue = Handles.ScaleValueHandle(
                    arcValue,
                    nodePositions[i] + Vector3.up + Vector3.forward * arcHandleSize
                        * 1.3f,
                    Quaternion.identity,
                    scaleHandleSize,
                    Handles.ConeCap,
                    1);

                // TODO Create float precision const.
                if (Math.Abs(newArcValue - arcValue) > 0.001f) {
                    // Execute callback.
                    callback(i, newArcValue / 360f);
                }
            }
        }

        private void DrawRotationHandle(Action<float, Vector3> callback) {
            Handles.color = Color.magenta;

            var currentAnimationTime = script.AnimationTimeRatio;
            //var animatedObjectPosition =
            //    script.AnimatedObjectPath.GetVectorAtTime(currentAnimationTime);
            var handlePosition = script.GetRotationAtTime(currentAnimationTime);
            var handleSize = HandleUtility.GetHandleSize(handlePosition);
            var sphereSize = handleSize * RotationHandleSize;

            // draw node's handle.
            var newPosition = Handles.FreeMoveHandle(
                handlePosition,
                Quaternion.identity,
                sphereSize,
                Vector3.zero,
                Handles.SphereCap);

            if (newPosition != handlePosition) {
                // Execute callback.
                callback(currentAnimationTime, newPosition);
            }
        }
        #endregion
        #region CALLBACK HANDLERS
        private void DrawRotationHandlesCallbackHandler(
                            float timestamp,
                            Vector3 newPosition) {

            script.ChangeRotationForTimestamp(timestamp, newPosition);
        }

        // TODO Refactor.
        private void DrawEaseHandlesCallbackHandler(int keyIndex, float newTimestamp) {
            HandleUndo();

            // Copy keyframe.
            var keyframeCopy = script.EaseCurve.keys[keyIndex];
            // Update keyframe timestamp.
            keyframeCopy.time = newTimestamp;
            var oldTimestamp = script.EaseCurve.keys[keyIndex].time;

            // If new timestamp is bigger than old timestamp..
            if (newTimestamp > oldTimestamp) {
                // Get timestamp of the node to the right.
                var rightNeighbourTimestamp = script.EaseCurve.keys[keyIndex + 1].time;
                // If new timestamp is bigger or equal to the neighbors's..
                if (newTimestamp >= rightNeighbourTimestamp) return;

                // Move key in the easeCurve.
                script.EaseCurve.MoveKey(keyIndex, keyframeCopy);
            }
            else {
                // Get timestamp of the node to the left.
                var leftNeighbourTimestamp = script.EaseCurve.keys[keyIndex - 1].time;
                // If new timestamp is smaller or equal to the neighbors's..
                if (newTimestamp <= leftNeighbourTimestamp) return;

                // Move key in the easeCurve.
                script.EaseCurve.MoveKey(keyIndex, keyframeCopy);
            }
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        /// Change current animation time with arrow keys.
        /// </summary>
        private void ChangeTimeWithArrowKeys() {
            // If a key is pressed..
            if (Event.current.type == EventType.keyDown
                // and modifier key is pressed also..
                    && modKeyPressed) {

                HandleModifiedShortcuts();
            }
            // Modifier key not pressed.
            else if (Event.current.type == EventType.keyDown) {
                HandleUnmodifiedShortcuts();
            }

        }

        // TODO Rename to GetNearestBackwardNodeTimestamp().
        private float GetNearestNodeBackwardTimestamp() {
            var targetPathTimestamps = script.GetTargetPathTimestamps();

            for (var i = targetPathTimestamps.Length - 1; i >= 0; i--) {
                if (targetPathTimestamps[i] < animTimeRatio.floatValue) {
                    return targetPathTimestamps[i];
                }
            }

            // Return timestamp of the last node.
            return 0;
        }

        // TODO Rename to GetNearestForwardNodeTimestamp().
        private float GetNearestNodeForwardTimestamp() {
            var targetPathTimestamps = script.GetTargetPathTimestamps();

            foreach (var timestamp in targetPathTimestamps
                .Where(timestamp => timestamp > animTimeRatio.floatValue)) {

                return timestamp;
            }

            // Return timestamp of the last node.
            return 1.0f;
        }

        private void HandleModifiedShortcuts() {
            // Check what key is pressed..
            switch (Event.current.keyCode) {
                // Jump backward.
                case AnimationPathAnimator.JumpBackward:
                    Event.current.Use();

                    // Update animation time.
                    animTimeRatio.floatValue -=
                        AnimationPathAnimator.JumpValue;

                    break;
                // Jump forward.
                case AnimationPathAnimator.JumpForward:
                    Event.current.Use();

                    // Update animation time.
                    animTimeRatio.floatValue +=
                        AnimationPathAnimator.JumpValue;

                    break;

                case AnimationPathAnimator.JumpToStart:
                    Event.current.Use();

                    // Jump to next node.
                    animTimeRatio.floatValue = GetNearestNodeForwardTimestamp();

                    break;

                case AnimationPathAnimator.JumpToEnd:
                    Event.current.Use();

                    // Jump to next node.
                    animTimeRatio.floatValue = GetNearestNodeBackwardTimestamp();

                    break;
            }
        }

        private void HandleUnmodifiedShortcuts() {
            // Helper variable.
            float newAnimationTimeRatio;
            switch (Event.current.keyCode) {
                // Jump backward.
                case AnimationPathAnimator.JumpBackward:
                    Event.current.Use();

                    // Calculate new time ratio.
                    newAnimationTimeRatio = animTimeRatio.floatValue
                                            - AnimationPathAnimator.ShortJumpValue;
                    // Apply rounded value.
                    animTimeRatio.floatValue =
                        (float)(Math.Round(newAnimationTimeRatio, 3));

                    break;
                // Jump forward.
                case AnimationPathAnimator.JumpForward:
                    Event.current.Use();

                    newAnimationTimeRatio = animTimeRatio.floatValue
                                            + AnimationPathAnimator.ShortJumpValue;
                    animTimeRatio.floatValue =
                        (float)(Math.Round(newAnimationTimeRatio, 3));

                    break;

                case AnimationPathAnimator.JumpToStart:
                    Event.current.Use();

                    animTimeRatio.floatValue = 1;

                    break;

                case AnimationPathAnimator.JumpToEnd:
                    Event.current.Use();

                    animTimeRatio.floatValue = 0;

                    break;
            }
        }

        /// <summary>
        /// Checked if modifier key is pressed and remember it in a class
        /// field.
        /// </summary>
        private void UpdateModifierKey() {
            // Check if modifier key is currently pressed.
            if (Event.current.type == EventType.keyDown
                    && Event.current.keyCode == AnimationPathAnimator.ModKey) {

                // Remember key state.
                modKeyPressed = true;
            }
            // If modifier key was released..
            if (Event.current.type == EventType.keyUp
                    && Event.current.keyCode == AnimationPathAnimator.ModKey) {

                modKeyPressed = false;
            }
        }

        /// <summary>
        /// Record target object state for undo.
        /// </summary>
        protected void HandleUndo() {
            Undo.RecordObject(script, "Ease curve changed.");
        }
        #endregion PRIVATE METHODS
    }
}