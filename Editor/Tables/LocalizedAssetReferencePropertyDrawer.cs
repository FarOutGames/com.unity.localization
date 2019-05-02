﻿using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization
{
    [CustomPropertyDrawer(typeof(LocalizedAssetReference), true)]
    class LocalizedAssetReferencePropertyDrawer : PropertyDrawer
    {
        public LocalizedAssetReference AssetReference { get; private set; }
        KeyDatabase m_KeyDatabase;

        SerializedProperty m_TableName;
        SerializedProperty m_Key;
        SerializedProperty m_KeyId;

        public string NoAssetString => AssetReference.AssetType != null ? $"None ({AssetReference.AssetType.Name})" : "None";

        void Init(SerializedProperty property)
        {
            AssetReference = property.GetActualObjectForSerializedProperty<LocalizedAssetReference>(fieldInfo);
            m_TableName = property.FindPropertyRelative("m_TableName");
            m_Key = property.FindPropertyRelative("m_Key");
            m_KeyId = property.FindPropertyRelative("m_KeyId");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null || label == null)
                return;

            Init(property);

            position.height = EditorGUIUtility.singleLineHeight;
            var dropDownPosition = EditorGUI.PrefixLabel(position, label);
            if (EditorGUI.DropdownButton(dropDownPosition, GetDropDownLabel(), FocusType.Keyboard))
            {
                PopupWindow.Show(dropDownPosition, new LocalizedReferencePopupWindow(new LocalizedAssetReferenceTreeView(this)) { Width = dropDownPosition.width });
            }
        }

        GUIContent GetDropDownLabel()
        {
            if (!string.IsNullOrEmpty(m_TableName.stringValue) && (!string.IsNullOrEmpty(m_Key.stringValue) || m_KeyId.intValue != KeyDatabase.EmptyId))
            {
                var icon = EditorGUIUtility.ObjectContent(null, AssetReference.AssetType);

                if (m_KeyId.intValue == KeyDatabase.EmptyId)
                {
                    return new GUIContent(m_TableName.stringValue + "/" + m_Key.stringValue, icon.image);
                }

                if (m_KeyDatabase == null)
                {
                    var tables = LocalizationEditorSettings.GetAssetTablesCollection<LocalizedAssetTable>();
                    var foundTableCollection = tables.Find(tbl => tbl.TableName == m_TableName.stringValue);
                    if (foundTableCollection != null && foundTableCollection.Keys != null)
                    {
                        m_KeyDatabase = foundTableCollection.Keys;
                    }
                }

                if (m_KeyDatabase != null)
                    return new GUIContent(m_TableName.stringValue + "/" + m_KeyDatabase.GetKey((uint)m_KeyId.intValue), icon.image);
            }
            return new GUIContent(NoAssetString);
        }

        public void ClearValue()
        {
            m_TableName.stringValue = string.Empty;
            m_Key.stringValue = string.Empty;
            m_KeyId.intValue = (int)KeyDatabase.EmptyId;

            // SetValue will be called by the Popup and outside of our OnGUI so we need to call ApplyModifiedProperties
            m_TableName.serializedObject.ApplyModifiedProperties();
        }

        public void SetValue(string table, KeyDatabase.KeyDatabaseEntry keyEntry)
        {
            m_TableName.stringValue = table;
            m_Key.stringValue = string.Empty;
            m_KeyId.intValue = (int)keyEntry.Id;
        
            // SetValue will be called by the Popup and outside of our OnGUI so we need to call ApplyModifiedProperties
            m_TableName.serializedObject.ApplyModifiedProperties();
        }
    }

    class LocalizedAssetRefTreeViewItem : TreeViewItem
    {
        public AssetTableCollection Table { get; set; }
        public KeyDatabase.KeyDatabaseEntry KeyEntry { get; set; }
        
        public LocalizedAssetRefTreeViewItem(AssetTableCollection table, KeyDatabase.KeyDatabaseEntry keyEntry, int id, int depth) : 
            base(id, depth)
        {
            Table = table;
            if (keyEntry != null)
            {
                KeyEntry = keyEntry;
                displayName = KeyEntry.Key;
            }
        }
    }

    class LocalizedAssetReferenceTreeView : TreeView
    {
        LocalizedAssetReferencePropertyDrawer m_Drawer;

        public LocalizedAssetReferenceTreeView(LocalizedAssetReferencePropertyDrawer drawer)
            : base(new TreeViewState())
        {
            m_Drawer = drawer;

            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            var id = 1;

            root.AddChild(new LocalizedAssetRefTreeViewItem(null, null, id++, 0) { displayName = m_Drawer.NoAssetString });

            var tables = LocalizationEditorSettings.GetAssetTablesCollection<LocalizedAssetTable>();
            foreach (var table in tables)
            {
                if (m_Drawer.AssetReference.AssetType != null && table.AssetType != m_Drawer.AssetReference.AssetType)
                    continue;

                var keys = table.Keys;
                var tableNode = new TreeViewItem(id++, 0, table.TableName);
                tableNode.icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(table.Tables[0])) as Texture2D;
                root.AddChild(tableNode);

                foreach (var key in keys.Entries)
                {
                    tableNode.AddChild(new LocalizedAssetRefTreeViewItem(table, key, id++, 1));
                }
            }

            if(!root.hasChildren)
            {
                root.AddChild(new TreeViewItem(1, 0, "No Asset Tables Found."));
            }

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if(FindItem(selectedIds[0], rootItem) is LocalizedAssetRefTreeViewItem keyNode)
            {
                if (keyNode.Table == null)
                    m_Drawer.ClearValue();
                else
                    m_Drawer.SetValue(keyNode.Table.TableName, keyNode.KeyEntry);
            }
            SetSelection(new int[] { });
        }
    }
}