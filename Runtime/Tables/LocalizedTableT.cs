﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.Metadata;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// Player version of a table entry that can contain additional data that is not serialized.
    /// </summary>
    public class TableEntry : IMetadataCollection
    {
        /// <summary>
        /// The table that this entry is part of.
        /// </summary>
        public LocalizedTable Table { get; internal set; }

        /// <summary>
        /// The serialized data
        /// </summary>
        internal TableEntryData Data { get; set; }

        /// <summary>
        /// The Metadata for this table entry.
        /// </summary>
        public IList<IMetadata> Entries => Data.Metadata.Entries;

        /// <summary>
        /// Returns the first Metadata item from <see cref="Entries"/> of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public TObject GetMetadata<TObject>() where TObject : IMetadata
        {
            return Data.Metadata.GetMetadata<TObject>();
        }

        /// <summary>
        /// Populates the list with all Metadata from <see cref="Entries"/> that is of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="foundItems"></param>
        public void GetMetadatas<TObject>(IList<TObject> foundItems) where TObject : IMetadata
        {
            Data.Metadata.GetMetadatas(foundItems);
        }

        /// <summary>
        /// Returns all Metadata from <see cref="Entries"/> that is of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public IList<TObject> GetMetadatas<TObject>() where TObject : IMetadata
        {
            return Data.Metadata.GetMetadatas<TObject>();
        }

        /// <summary>
        /// Tags are Metadata that can be shared across multiple table entries,
        /// they are often used to indicate an entry has a particular attribute or feature, e.g SmartFormat.
        /// Generally Tags do not contains data, for sharing data across multiple table entries see <see cref="AddSharedMetadata"/>.
        /// A Tag reference will be stored in <see cref="LocalizedTable.TableData"/> and <see cref="Entries"/>.
        /// </summary>
        /// <typeparam name="TShared"></typeparam>
        public void AddTagMetadata<TShared>() where TShared : SharedTableEntryMetadata, new()
        {
            TShared tag = null;
            foreach(var md in Table.TableData)
            {
                if (md is TShared shared)
                {
                    tag = shared;
                    break;
                }
            }

            if (tag == null)
            {
                tag = new TShared();
                Table.AddMetadata(tag);
            }

            tag.Register(this);
            AddMetadata(tag);
        }

        /// <summary>
        /// SharedTableEntryMetadata is Metadata that can be shared across multiple entries in a single table.
        /// The instance reference will be stored in <see cref="LocalizedTable.TableData"/> and <see cref="Entries"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddSharedMetadata(SharedTableEntryMetadata md)
        {
            if (!Table.Contains(md))
            {
                Table.AddMetadata(md);
            }

            md.Register(this);
            AddMetadata(md);
        }

        /// <summary>
        /// SharedTableCollectionMetadata is Metadata that can be applied to multiple table entries in a table collection.
        /// The Metadata is stored in the <see cref="KeyDatabase"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddSharedMetadata(SharedTableCollectionMetadata md)
        {
            if (!Table.Keys.Metadata.Contains(md))
            {
                Table.Keys.Metadata.AddMetadata(md);
            }

            md.AddEntry(Data.Id, Table.LocaleIdentifier.Code);
        }

        /// <summary>
        /// Add an entry to <see cref="Entries"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddMetadata(IMetadata md)
        {
            Data.Metadata.AddMetadata(md);
        }

        /// <summary>
        /// Removes the Metadata tag from this entry and the table if it is no longer used by any other table entries.
        /// </summary>
        /// <typeparam name="TShared"></typeparam>
        public void RemoveTagMetadata<TShared>() where TShared : SharedTableEntryMetadata
        {
            var tag = Table.GetMetadata<TShared>();
            if (tag != null)
            {
                tag.Unregister(this);
                RemoveMetadata(tag);

                // Remove the shared data if it is no longer used
                if (tag.Count == 0)
                {
                    Table.RemoveMetadata(tag);
                }
            }
        }

        /// <summary>
        /// Removes the entry from the shared Metadata in the table and removes the 
        /// shared Metadata if no other entires are using it.
        /// </summary>
        /// <param name="md"></param>
        public void RemoveSharedMetadata(SharedTableEntryMetadata md)
        {
            md.Unregister(this);
            RemoveMetadata(md);

            // Remove the shared data if it is no longer used
            if (md.Count == 0 && Table.Contains(md))
            {
                Table.RemoveMetadata(md);
            }
        }

        /// <summary>
        /// Removes the entry from the Shared Metadata and removes it from the 
        /// <see cref="KeyDatabase"/> if no other entires are using it.
        /// </summary>
        /// <param name="md"></param>
        public void RemoveSharedMetadata(SharedTableCollectionMetadata md)
        {
            md.RemoveEntry(Data.Id, Table.LocaleIdentifier.Code);

            if (md.IsEmpty)
            {
                Table.Keys.Metadata.RemoveMetadata(md);
            }
        }

        /// <summary>
        /// Remove an entry from <see cref="Entries"/>.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool RemoveMetadata(IMetadata md)
        {
            return Data.Metadata.RemoveMetadata(md);
        }

        /// <summary>
        /// Checks if the Metadata is contained within <see cref="Entries"/>.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool Contains(IMetadata md)
        {
            return Data.Metadata.Contains(md);
        }
    };

    public abstract class LocalizedTableT<TEntry> : LocalizedTable, IDictionary<uint, TEntry> ,ISerializationCallbackReceiver where TEntry : TableEntry
    {
        Dictionary<uint, TEntry> m_TableEntries = new Dictionary<uint, TEntry>();

        ICollection<uint> IDictionary<uint, TEntry>.Keys => m_TableEntries.Keys;

        /// <summary>
        /// All values in this table.
        /// </summary>
        public ICollection<TEntry> Values => m_TableEntries.Values;

        /// <summary>
        /// The number of entries in this Table.
        /// </summary>
        public int Count => m_TableEntries.Count;

        /// <summary>
        /// Will always be false. Implemented because it is required by the System.Collections.IList interface.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Get/Set a value the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntry this[uint key]
        {
            get => m_TableEntries[key];
            set
            {
                if (key == KeyDatabase.EmptyId)
                    throw new Exception("Key Id value 0, is not valid. All Key Id's must be non-zero.");

                if (value.Table != this)
                    throw new Exception("Table entry does not belong to this table. Table entries can not be shared across tables.");

                m_TableEntries[key] = value;
            }
        }

        /// <summary>
        /// Returns a new instance of TEntry.
        /// </summary>
        /// <returns></returns>
        public abstract TEntry CreateTableEntry();

        internal TEntry CreateTableEntry(TableEntryData data)
        {
            var entry = CreateTableEntry();
            entry.Data = data;
            return entry;
        }

        /// <summary>
        /// Add or update an entry in the table.
        /// </summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="localized">The localized item, a string for <see cref="StringTable"/> or asset guid for <see cref="AssetTable"/>.</param>
        /// <returns></returns>
        public TEntry AddEntry(string key, string localized)
        {
            var keyId = FindKeyId(key);
            return keyId == 0 ? null : AddEntry(keyId, localized);
        }

        /// <summary>
        /// Add or update an entry in the table.
        /// </summary>
        /// <param name="keyId">The unique key id.</param>
        /// <param name="localized">The localized item, a string for <see cref="StringTable"/> or asset guid for <see cref="AssetTable"/>.</param>
        /// <returns></returns>
        public virtual TEntry AddEntry(uint keyId, string localized)
        {
            if (!m_TableEntries.TryGetValue(keyId, out var tableEntry))
            {
                tableEntry = CreateTableEntry();
                tableEntry.Data = new TableEntryData(keyId);
                m_TableEntries[keyId] = tableEntry;
            }

            tableEntry.Data.Localized = localized;
            return tableEntry;
        }

        /// <summary>
        /// Remove an entry from the table if it exists.
        /// </summary>
        /// <param name="key">The name of the key.</param>
        /// <returns>True if the entry was found and removed.</returns>
        public bool RemoveEntry(string key)
        {
            var keyId = FindKeyId(key);
            return keyId == 0 ? false : RemoveEntry(keyId);
        }

        /// <summary>
        /// Remove an entry from the table if it exists.
        /// </summary>
        /// <param name="keyId">The key id to remove.</param>
        /// <returns>True if the entry was found and removed.</returns>
        public virtual bool RemoveEntry(uint keyId)
        {
            if (m_TableEntries.TryGetValue(keyId, out var item))
            {
                return m_TableEntries.Remove(keyId);
            }

            return false;
        }

        /// <summary>
        /// Returns the entry for the key or null if one does not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntry GetEntry(string key)
        {
            var keyId = FindKeyId(key);
            return keyId == 0 ? null : GetEntry(keyId);
        }

        /// <summary>
        /// Returns the entry for the key id or null if one does not exist.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public virtual TEntry GetEntry(uint keyId)
        {
            m_TableEntries.TryGetValue(keyId, out var tableEntry);
            return tableEntry;
        }

        /// <summary>
        /// Adds the entry with the specified keyId.
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="value"></param>
        public void Add(uint keyId, TEntry value)
        {
            if (value.Table != this)
                throw new Exception("Table entry does not belong to this table. Table entries can not be shared across tables.");

            if (keyId == KeyDatabase.EmptyId)
                throw new Exception("Key Id value 0, is not valid. All Key Id's must be non-zero.");

            m_TableEntries.Add(keyId, value);
        }

        /// <summary>
        /// Adds the item value with the specified keyId.
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<uint, TEntry> item)
        {
            if (item.Value.Table != this)
                throw new Exception("Table entry does not belong to this table. Table entries can not be shared across tables.");

            if (item.Key == KeyDatabase.EmptyId)
                throw new Exception("Key Id value 0, is not valid. All Key Id's must be non-zero.");

            m_TableEntries.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Returns true if the table contains an entry with the keyId.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public bool ContainsKey(uint keyId) => m_TableEntries.ContainsKey(keyId);

        /// <summary>
        /// Returns true if the table contains the item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<uint, TEntry> item) => m_TableEntries.Contains(item);

        /// <summary>
        /// Remove the entry with the keyId.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public bool Remove(uint keyId) => m_TableEntries.Remove(keyId);

        /// <summary>
        /// Remove the item from the table if it exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<uint, TEntry> item) => m_TableEntries.Remove(item.Key);

        /// <summary>
        /// Find the entry, if it exists in the table.
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="value"></param>
        /// <returns>Trus if the entry was found.</returns>
        public bool TryGetValue(uint keyId, out TEntry value) => m_TableEntries.TryGetValue(keyId, out value);

        /// <summary>
        /// Clear all entries in this table.
        /// </summary>
        public void Clear() => m_TableEntries.Clear();

        /// <summary>
        /// Copies the contents of the table into an array starting at the arrayIndex.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<uint, TEntry>[] array, int arrayIndex)
        {
            foreach (var entry in m_TableEntries)
            {
                array[arrayIndex++] = entry;
            }
        }

        /// <summary>
        /// Return an enumerator for the entries in this table.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<uint, TEntry>> GetEnumerator() => m_TableEntries.GetEnumerator();

        /// <summary>
        /// Return an enumerator for the entries in this table.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => m_TableEntries.GetEnumerator();

        /// <summary>
        /// Creates a string representation of the table as "{TableName}({LocaleIdentifier})".
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{TableName}({LocaleIdentifier})";

        /// <summary>
        /// Does nothing but required for <see cref="OnAfterDeserialize"/>.
        /// </summary>
        public void OnBeforeSerialize()
        {
            TableData.Clear();
            foreach(var entry in this)
            {
                // Sync the id
                entry.Value.Data.Id = entry.Key;

                TableData.Add(entry.Value.Data);
            }
        }

        /// <summary>
        /// Converts the serialized data into <see cref="m_TableEntries"/>.
        /// </summary>
        public void OnAfterDeserialize()
        {
            try
            {
                m_TableEntries = TableData.ToDictionary(o => o.Id, CreateTableEntry);
            }
            catch (Exception e)
            {
                var error = $"Error Deserializing Table Data \"{TableName}({LocaleIdentifier})\".\n{e.Message}\n{e.InnerException}";
                Debug.LogError(error, this);
            }
        }
    }
}