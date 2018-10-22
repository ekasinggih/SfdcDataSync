using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SfdcSvc;

namespace SfdcDataSync.Core
{
    public class DataTableObject : List<object[]>
    {
        private Dictionary<string, Int32> _fieldsNameIndex;
        private Dictionary<Int32, string> _fieldsIndexName;

        private DataTableObject()
        {
            _fieldsNameIndex = new Dictionary<string, int>();
            _fieldsIndexName = new Dictionary<int, string>();
        }

        public DataTableObject(sObject[] sObjects) : this()
        {
            if (sObjects == null) throw new ArgumentNullException(nameof(sObjects));

            if (sObjects.Length > 0)
            {
                // populate fields name and index
                int fieldsCount = sObjects[0].Any.Length;
                for (int i = 0; i < fieldsCount; i++)
                {
                    string fieldName = sObjects[0].Any[i].Name.LocalName;
                    _fieldsNameIndex.Add(fieldName, i);
                    _fieldsIndexName.Add(i, fieldName);
                }

                // populate data
                for (int i = 0; i < sObjects.Length; i++)
                {
                    object[] row = new object[fieldsCount];
                    for (int j = 0; j < fieldsCount; j++)
                    {
                        row[j] = sObjects[i].Any[j].Value;
                    }
                    this.Add(row);
                }
            }
        }

        //public DataTableObject(IDataReader dataReader)
        //{
        //    int index = 0;
        //    while (dataReader.Read())
        //    {
        //        int fieldsCount = dataReader.FieldCount;

        //        // populate fields name and index
        //        if (index == 0)
        //        {
        //            for (int i = 0; i < fieldsCount; i++)
        //            {
        //                string fieldName = dataReader.GetName(i);
        //                _fieldsNameIndex.Add(fieldName, i);
        //                _fieldsIndexName.Add(i, fieldName);
        //            }
        //        }

        //        // populate field value
        //        object[] row = new object[fieldsCount];
        //        for (int i = 0; i < fieldsCount; i++)
        //        {
        //            row[i] = dataReader.GetValue(i);
        //        }
        //        this.Add(row);
        //    }
        //}

        public DataTableObject(List<object[]> data, Dictionary<string, int> fieldsNameIndex, Dictionary<int, string> fieldsIndexName) : this()
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _fieldsNameIndex = fieldsNameIndex ?? throw new ArgumentNullException(nameof(fieldsNameIndex));
            _fieldsIndexName = fieldsIndexName ?? throw new ArgumentNullException(nameof(fieldsIndexName));

            this.AddRange(data);
        }

        public int Length => this.Count;

        public int GetFieldIndex(string fieldName)
        {
            return _fieldsNameIndex[fieldName];
        }

        public string GetFieldName(int index)
        {
            return _fieldsIndexName[index];
        }

        public sObject[] ToSObjectArray(string objectType, IEnumerable<FieldMapping> mappings = null)
        {
            sObject[] sObjects = new sObject[this.Count];
            FieldMapping[] map = null;
            if (mappings != null)
                map = mappings as FieldMapping[] ?? mappings.ToArray();

            // loop on row
            for (int i = 0; i < this.Count; i++)
            {
                sObject sObject = new sObject { type = objectType };
                XElement[] objectFields;

                if (map != null && map.Any())
                {
                    // if field mapping specified
                    objectFields = new XElement[map.Length];

                    int targetFieldIndex = 0;
                    // lopp on column mapping
                    for (int j = 0; j < map.Length; j++)
                    {
                        // skip field when updateNullValue = false and value is null
                        int sourceFieldIndex = GetFieldIndex(map[j].From);
                        if (!map[j].UpdateOnNull && (this[i][sourceFieldIndex] == null || string.IsNullOrWhiteSpace(this[i][sourceFieldIndex].ToString())))
                            continue;

                        objectFields[targetFieldIndex] = new XElement(map[j].To, this[i][sourceFieldIndex]);
                        targetFieldIndex++;
                    }
                }
                else
                {
                    // if field mapping not specified, map all field as is
                    objectFields = new XElement[this[i].Length];

                    // loop on column
                    for (int j = 0; j < this[i].Length; j++)
                    {
                        // skip field when updateNullValue = false and value is null
                        if (!map[j].UpdateOnNull && (this[i][j] == null || string.IsNullOrWhiteSpace(this[i][j].ToString())))
                            continue;

                        objectFields[j] = new XElement(GetFieldName(j), this[i][j]);
                    }
                }

                sObject.Any = objectFields;
                sObjects[i] = sObject;
            }

            return sObjects;
        }
    }
}
