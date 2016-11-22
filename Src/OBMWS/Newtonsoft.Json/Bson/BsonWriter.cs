#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Utilities;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Newtonsoft.Json.Bson
{
  /// <summary>
  /// Represents a writer that provides a fast, non-cached, forward-only way of generating Json data.
  /// </summary>
  public class BsonWriter : JsonWriter
  {
    private readonly BsonBinaryWriter _writer;

    private BsonToken _root;
    private BsonToken _parent;
    private string _propertyName;

    /// <summary>
    /// Gets or sets the <see cref="DateTimeKind" /> used when writing <see cref="DateTime"/> values to BSON.
    /// When set to <see cref="DateTimeKind.Unspecified" /> no conversion will occur.
    /// </summary>
    /// <value>The <see cref="DateTimeKind" /> used when writing <see cref="DateTime"/> values to BSON.</value>
    public DateTimeKind DateTimeKindHandling
    {
      get { return _writer.DateTimeKindHandling; }
      set { _writer.DateTimeKindHandling = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BsonWriter"/> class.
    /// </summary>
    /// <param name="stream">The stream.</param>
    public BsonWriter(Stream stream)
    {
      ValidationUtils.ArgumentNotNull(stream, "stream");
      _writer = new BsonBinaryWriter(stream);
    }

    /// <summary>
    /// Flushes whatever is in the buffer to the underlying streams and also flushes the underlying stream.
    /// </summary>
    public override void Flush()
    {
      _writer.Flush();
    }

    /// <summary>
    /// Writes the end.
    /// </summary>
    /// <param name="token">The token.</param>
    protected override void WriteEnd(JsonToken token)
    {
      base.WriteEnd(token);
      RemoveParent();

      if (Top == 0)
      {
        _writer.WriteToken(_root);
      }
    }

    /// <summary>
    /// Writes out a comment <code>/*...*/</code> containing the specified text.
    /// </summary>
    /// <param name="text">Text to place inside the comment.</param>
    public override void WriteComment(string text, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      throw new JsonWriterException("Cannot write JSON comment as BSON.");
    }

    /// <summary>
    /// Writes the start of a constructor with the given name.
    /// </summary>
    /// <param name="name">The name of the constructor.</param>
    public override void WriteStartConstructor(string name, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      throw new JsonWriterException("Cannot write JSON constructor as BSON.");
    }

    /// <summary>
    /// Writes raw JSON.
    /// </summary>
    /// <param name="json">The raw JSON to write.</param>
    public override void WriteRaw(string json)
    {
      throw new JsonWriterException("Cannot write raw JSON as BSON.");
    }

    /// <summary>
    /// Writes raw JSON where a value is expected and updates the writer's state.
    /// </summary>
    /// <param name="json">The raw JSON to write.</param>
    public override void WriteRawValue(string json, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      throw new JsonWriterException("Cannot write raw JSON as BSON.");
    }

    /// <summary>
    /// Writes the beginning of a Json array.
    /// </summary>
    public override void WriteStartArray(OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteStartArray(mode);

      AddParent(new BsonArray());
    }

    /// <summary>
    /// Writes the beginning of a Json object.
    /// </summary>
    public override void WriteStartObject(OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteStartObject(mode);

      AddParent(new BsonObject());
    }

    /// <summary>
    /// Writes the property name of a name/value pair on a Json object.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    public override void WritePropertyName(string name, OBMWS.PrintMode mode= OBMWS.PrintMode.ValueCell)
    {
      base.WritePropertyName(name);

      _propertyName = name;
    }

    /// <summary>
    /// Closes this stream and the underlying stream.
    /// </summary>
    public override void Close()
    {
      base.Close();

      if (CloseOutput && _writer != null)
        _writer.Close();
    }

    private void AddParent(BsonToken container)
    {
      AddToken(container);
      _parent = container;
    }

    private void RemoveParent()
    {
      _parent = _parent.Parent;
    }

    private void AddValue(object value, BsonType type)
    {
      AddToken(new BsonValue(value, type));
    }

    internal void AddToken(BsonToken token)
    {
      if (_parent != null)
      {
        if (_parent is BsonObject)
        {
          ((BsonObject)_parent).Add(_propertyName, token);
          _propertyName = null;
        }
        else
        {
          ((BsonArray)_parent).Add(token);
        }
      }
      else
      {
        if (token.Type != BsonType.Object && token.Type != BsonType.Array)
          throw new JsonWriterException("Error writing {0} value. BSON must start with an Object or Array.".FormatWith(CultureInfo.InvariantCulture, token.Type));

        _parent = token;
        _root = token;
      }
    }

    #region WriteValue methods
    /// <summary>
    /// Writes a null value.
    /// </summary>
    public override void WriteNull(OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteNull(mode);
      AddValue(null, BsonType.Null);
    }

    /// <summary>
    /// Writes an undefined value.
    /// </summary>
    public override void WriteUndefined(OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteUndefined(mode);
      AddValue(null, BsonType.Undefined);
    }

    /// <summary>
    /// Writes a <see cref="String"/> value.
    /// </summary>
    /// <param name="value">The <see cref="String"/> value to write.</param>
    public override void WriteValue(string value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      if (value == null)
        AddValue(null, BsonType.Null);
      else
        AddToken(new BsonString(value, true));
    }

    /// <summary>
    /// Writes a <see cref="Int32"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Int32"/> value to write.</param>
    public override void WriteValue(int value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="UInt32"/> value.
    /// </summary>
    /// <param name="value">The <see cref="UInt32"/> value to write.</param>
    [CLSCompliant(false)]
#pragma warning disable CS3021 // 'BsonWriter.WriteValue(uint)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    public override void WriteValue(uint value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
#pragma warning restore CS3021 // 'BsonWriter.WriteValue(uint)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    {
      if (value > int.MaxValue)
        throw new JsonWriterException("Value is too large to fit in a signed 32 bit integer. BSON does not support unsigned values.");

      base.WriteValue(value,mode);
      AddValue(value, BsonType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="Int64"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Int64"/> value to write.</param>
    public override void WriteValue(long value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Long);
    }

    /// <summary>
    /// Writes a <see cref="UInt64"/> value.
    /// </summary>
    /// <param name="value">The <see cref="UInt64"/> value to write.</param>
    [CLSCompliant(false)]
#pragma warning disable CS3021 // 'BsonWriter.WriteValue(ulong)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    public override void WriteValue(ulong value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
#pragma warning restore CS3021 // 'BsonWriter.WriteValue(ulong)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    {
      if (value > long.MaxValue)
        throw new JsonWriterException("Value is too large to fit in a signed 64 bit integer. BSON does not support unsigned values.");

      base.WriteValue(value,mode);
      AddValue(value, BsonType.Long);
    }

    /// <summary>
    /// Writes a <see cref="Single"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Single"/> value to write.</param>
    public override void WriteValue(float value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Number);
    }

    /// <summary>
    /// Writes a <see cref="Double"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Double"/> value to write.</param>
    public override void WriteValue(double value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Number);
    }

    /// <summary>
    /// Writes a <see cref="Boolean"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Boolean"/> value to write.</param>
    public override void WriteValue(bool value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Boolean);
    }

    /// <summary>
    /// Writes a <see cref="Int16"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Int16"/> value to write.</param>
    public override void WriteValue(short value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="UInt16"/> value.
    /// </summary>
    /// <param name="value">The <see cref="UInt16"/> value to write.</param>
    [CLSCompliant(false)]
#pragma warning disable CS3021 // 'BsonWriter.WriteValue(ushort)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    public override void WriteValue(ushort value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
#pragma warning restore CS3021 // 'BsonWriter.WriteValue(ushort)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="Char"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Char"/> value to write.</param>
    public override void WriteValue(char value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddToken(new BsonString(value.ToString(), true));
    }

    /// <summary>
    /// Writes a <see cref="Byte"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Byte"/> value to write.</param>
    public override void WriteValue(byte value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="SByte"/> value.
    /// </summary>
    /// <param name="value">The <see cref="SByte"/> value to write.</param>
    [CLSCompliant(false)]
#pragma warning disable CS3021 // 'BsonWriter.WriteValue(sbyte)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    public override void WriteValue(sbyte value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
#pragma warning restore CS3021 // 'BsonWriter.WriteValue(sbyte)' does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Integer);
    }

    /// <summary>
    /// Writes a <see cref="Decimal"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Decimal"/> value to write.</param>
    public override void WriteValue(decimal value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Number);
    }

    /// <summary>
    /// Writes a <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="value">The <see cref="DateTime"/> value to write.</param>
    public override void WriteValue(DateTime value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Date);
    }

#if !PocketPC && !NET20
    /// <summary>
    /// Writes a <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="value">The <see cref="DateTimeOffset"/> value to write.</param>
    public override void WriteValue(DateTimeOffset value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Date);
    }
#endif

    /// <summary>
    /// Writes a <see cref="T:Byte[]"/> value.
    /// </summary>
    /// <param name="value">The <see cref="T:Byte[]"/> value to write.</param>
    public override void WriteValue(byte[] value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddValue(value, BsonType.Binary);
    }

    /// <summary>
    /// Writes a <see cref="Guid"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to write.</param>
    public override void WriteValue(Guid value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddToken(new BsonString(value.ToString(), true));
    }

    /// <summary>
    /// Writes a <see cref="TimeSpan"/> value.
    /// </summary>
    /// <param name="value">The <see cref="TimeSpan"/> value to write.</param>
    public override void WriteValue(TimeSpan value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddToken(new BsonString(value.ToString(), true));
    }

    /// <summary>
    /// Writes a <see cref="Uri"/> value.
    /// </summary>
    /// <param name="value">The <see cref="Uri"/> value to write.</param>
    public override void WriteValue(Uri value, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      base.WriteValue(value,mode);
      AddToken(new BsonString(value.ToString(), true));
    }
    #endregion

    /// <summary>
    /// Writes a <see cref="T:Byte[]"/> value that represents a BSON object id.
    /// </summary>
    /// <param name="value"></param>
    public void WriteObjectId(byte[] value, OBMWS.PrintMode mode= OBMWS.PrintMode.ValueCell)
    {
      ValidationUtils.ArgumentNotNull(value, "value");

      if (value.Length != 12)
        throw new Exception("An object id must be 12 bytes");

      // hack to update the writer state
      AutoComplete(JsonToken.Undefined,mode);
      AddValue(value, BsonType.Oid);
    }

    /// <summary>
    /// Writes a BSON regex.
    /// </summary>
    /// <param name="pattern">The regex pattern.</param>
    /// <param name="options">The regex options.</param>
    public void WriteRegex(string pattern, string options, OBMWS.PrintMode mode = OBMWS.PrintMode.ValueCell)
    {
      ValidationUtils.ArgumentNotNull(pattern, "pattern");

      // hack to update the writer state
      AutoComplete(JsonToken.Undefined,mode);
      AddToken(new BsonRegex(pattern, options));
    }

        internal override void postFormating()
        {
            
        }
    }
}