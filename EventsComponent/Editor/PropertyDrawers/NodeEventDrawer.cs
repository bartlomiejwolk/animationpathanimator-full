﻿using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ATP.AnimationPathTools.EventsComponent {

    [CustomPropertyDrawer(typeof (NodeEventSlot))]
    public sealed class NodeEventDrawer : PropertyDrawer {

        // Hight of a single property.
        private const int PropHeight = 16;
        // Margin between properties.
        private const int PropMargin = 4;
        // Space between rows.
        private const int RowsSpace = 8;
        // Overall hight of the serialized property.
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label) {

            // Property with number of rows to be displayed.
            var rowsProperty = property.FindPropertyRelative("rows");
            // Copy rows number to local variable.
            var rows = rowsProperty.intValue;

            // Calculate property height.
            return base.GetPropertyHeight(property, label)
                   * rows // Each row is 16 px high.
                   + (rows - 1) * RowsSpace;
        }

        public override void OnGUI(
            Rect pos,
            SerializedProperty prop,
            GUIContent label) {

            var rowsProperty =
                prop.FindPropertyRelative("rows");

            var sourceGO =
                prop.FindPropertyRelative("sourceGO");

            var sourceCo =
                prop.FindPropertyRelative("sourceCo");

            var sourceComponentIndex =
                prop.FindPropertyRelative("sourceComponentIndex");

            var sourceMethodIndex =
                prop.FindPropertyRelative("sourceMethodIndex");

            var sourceMethodName =
                prop.FindPropertyRelative("sourceMethodName");

            var methodArg =
                prop.FindPropertyRelative("methodArg");

            EditorGUIUtility.labelWidth = 80;

            EditorGUI.PropertyField(
                new Rect(pos.x, pos.y, pos.width, PropHeight),
                sourceGO,
                new GUIContent("Source GO", ""));

            // If source GO is assigned..
            if (sourceGO.objectReferenceValue == null) {
                // Set rows number to 1.
                rowsProperty.intValue = 1;
                return;
            }
            // Set rows number to 4.
            rowsProperty.intValue = 4;

            // Get reference to source GO.
            var sourceGORef = sourceGO.objectReferenceValue as GameObject;
            // Get source game object components.
            var sourceComponents = sourceGORef.GetComponents<Component>();
            // Initialize array for source GO component names.
            var sourceCoNames = new string[sourceComponents.Length];
            // Fill array with component names.
            for (var i = 0; i < sourceCoNames.Length; i++) {
                sourceCoNames[i] = sourceComponents[i].GetType().ToString();
            }
            // Make sure that current name index corresponds to a component.
            // Important when changing source game object.
            if (sourceComponentIndex.intValue > sourceCoNames.Length - 1) {
                sourceComponentIndex.intValue = 0;
            }

            // Display dropdown game object component list.
            sourceComponentIndex.intValue = EditorGUI.Popup(
                new Rect(
                    pos.x,
                    pos.y + 1 * (PropHeight + PropMargin),
                    pos.width,
                    PropHeight),
                "Source Component",
                sourceComponentIndex.intValue,
                sourceCoNames);

            // Update source component ref. in the NodeEventSlot property.
            sourceCo.objectReferenceValue =
                sourceComponents[sourceComponentIndex.intValue];

            // Get target component method names.
            var methods = sourceComponents[sourceComponentIndex.intValue]
                .GetType()
                .GetMethods(
                    BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.DeclaredOnly);
            // Initialize array with method names.
            var methodNames = new string[methods.Length];
            // Fill array with method names.
            for (var i = 0; i < methodNames.Length; i++) {
                methodNames[i] = methods[i].Name;
            }

            // Display dropdown with component properties.
            sourceMethodIndex.intValue = EditorGUI.Popup(
                new Rect(
                    pos.x,
                    pos.y + 2 * (PropHeight + PropMargin),
                    pos.width,
                    PropHeight),
                "Methods",
                sourceMethodIndex.intValue,
                methodNames);

            // Update method name in the NodeEventSlot property.
            sourceMethodName.stringValue =
                methodNames[sourceMethodIndex.intValue];

            // Don't draw parameter field if source GO is not specified.
            if (sourceGO.objectReferenceValue == null) return;
            EditorGUI.PropertyField(
                new Rect(
                    pos.x,
                    pos.y + 3 * (PropHeight + PropMargin),
                    pos.width,
                    PropHeight),
                methodArg,
                new GUIContent("Argument", ""));
        }

    }

}