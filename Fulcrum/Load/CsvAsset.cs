using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public class CsvAsset<T> : IAsset where T : ICsvData, new()
{
    string _split;
    public List<T> Lines = new List<T>();
    Dictionary<string, FieldInfo> _fieldMap;
    string[] _headers = null;
    string _extension;
    public CsvAsset(string path, string split) : base(path)
    {
        Location = eAssetLocation.Data;
        _extension = System.IO.Path.GetExtension(path);
        _split = split;

        var type = typeof(T);

        var fieldlist = type.GetFields()
            .Concat(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance));
        _fieldMap = new Dictionary<string, FieldInfo>(); // file name to property name
        foreach (var prop in fieldlist)
        {
            var attr = prop.GetCustomAttribute<CsvField>();
            if (attr == null) continue;
            string name = attr.ColumnName ?? prop.Name;
            _fieldMap.Add(name, prop);
        }
    }
    public void HotReload(string path)
    {
        _Load(null);
    }
    public override eLoadState _Load(OLoad load)
    {
        var allLines = File.ReadAllLines(Path);
        if (allLines.Length < 1) throw new Exception("CSV file has no content");

        _headers = GUtil.Split(allLines[0].Trim(), _split);

        for (int i = 1; i < allLines.Length; i++)
        {
            var obj = new T();
            obj._RawLineText = allLines[i];

            var split = GUtil.Split(allLines[i], _split);
            for (int j = 0; j < split.Length; j++)
            {
                if (j > _headers.Length) break; // something is missing a header - weird
                var col = split[j];
                if (col == "") continue;
                var label = _headers[j];
                var prop = _fieldMap[label];

                object val;
                if (prop.FieldType == typeof(string))
                    val = col;
                else if (prop.FieldType == typeof(int))
                    val = int.Parse(col);
                else if (prop.FieldType == typeof(bool))
                    val = bool.Parse(col);
                else if (prop.FieldType == typeof(Color))
                    val = GColor.FromHex(col);
                else if (prop.FieldType.IsEnum)
                    val = Enum.Parse(prop.FieldType, col);
                else throw new NotImplementedException();
                prop.SetValue(obj, val);
            }
            Lines.Add(obj);
            obj.OnLoad();
        }
        return eLoadState.Complete;
    }
    public override void _Unload(OLoad load)
    {
        Lines.Clear();
        _headers = null;
    }
    public string FileExtension => "csv";
    public override bool SafeForParallel => true;

    public void WriteBack()
    {
        if (_headers == null) return;
        var writeHead = new List<string>();

        // this is complicated because I'm trying to handle writing a new column
        for (int i = 0; i < _headers.Length; i++)
            writeHead.Add(_headers[i]);
        foreach (var colName in _fieldMap.Keys)
            if (!writeHead.Contains(colName))
                writeHead.Add(colName);

        var colIndex = new Dictionary<string, int>();
        for (int i = 0; i < writeHead.Count; i++)
        {
            if (colIndex.ContainsKey(writeHead[i]))
                throw new Exception("Duplicate headers detected! Cannot write");
            colIndex.Add(writeHead[i], i);
        }

        var allLines = new List<string>(Lines.Count + 1)
            {
                GUtil.Join(_headers, _split)
            };
        foreach (var obj in Lines)
        {
            var fields = new string[_headers.Length];
            var orig = GUtil.Split(obj._RawLineText ?? "", _split);
            // important to leave the original alone
            for (int i = 0; i < _headers.Length; i++)
                fields[i] = (i < orig.Length) ? orig[i] : "";

            foreach (var pair in _fieldMap)
            {
                var label = pair.Key;
                var idx = colIndex[label];
                var prop = pair.Value;
                if (prop.FieldType == typeof(string))
                    fields[idx] = (string)prop.GetValue(obj) ?? "";
                else if (prop.FieldType == typeof(int))
                    fields[idx] = ((int)prop.GetValue(obj)).ToString();
                else if (prop.FieldType == typeof(bool))
                    fields[idx] = ((bool)prop.GetValue(obj)).ToString();
                else if (prop.FieldType == typeof(Color))
                    fields[idx] = GColor.ToHex((Color)prop.GetValue(obj));
                else if (prop.FieldType.IsEnum)
                    fields[idx] = ((Enum)prop.GetValue(obj)).ToString();
                else throw new NotImplementedException();
            }
            allLines.Add(GUtil.Join(fields, _split));
        }
        File.WriteAllLines(Path, allLines);
    }
}
public class ICsvData
{
    // may be null if created through alternative path (like a sync up)
    public string _RawLineText = null;
    // called after data is loaded into the mapped fields
    public virtual void OnLoad() { }
}

[AttributeUsage(AttributeTargets.Field)]
public class CsvField : Attribute
{
    // this flags a member of an ICsvData as a field to pull from a column
    // the name is important - the data is matched to the first line of the file
    // if not specified, the raw property name is used instead
    // NOTE: empty values in the sheet are left in default state
    public string ColumnName;
    public CsvField(string ColumnName = null)
    {
        this.ColumnName = ColumnName;
    }
}