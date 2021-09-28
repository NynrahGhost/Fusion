﻿using System;
using System.IO;
using UAssetAPI.PropertyTypes;

namespace UAssetAPI.StructTypes
{
    public class GuidPropertyData : PropertyData<Guid>
    {
        public GuidPropertyData(FName name, UAsset asset) : base(name, asset)
        {

        }

        public GuidPropertyData()
        {

        }

        private static readonly FName CurrentPropertyType = new FName("Guid");
        public override bool HasCustomStructSerialization { get { return true; } } 
        public override FName PropertyType { get { return CurrentPropertyType; } }

        public override void Read(BinaryReader reader, bool includeHeader, long leng1, long leng2 = 0)
        {
            if (includeHeader)
            {
                reader.ReadByte();
            }

            Value = new Guid(reader.ReadBytes(16));
        }

        public override int Write(BinaryWriter writer, bool includeHeader)
        {
            if (includeHeader)
            {
                writer.Write((byte)0);
            }

            writer.Write(Value.ToByteArray());
            return 16;
        }

        public override string ToString()
        {
            return Convert.ToString(Value);
        }

        public override void FromString(string[] d)
        {
            if (Guid.TryParse(d[0], out Guid res)) Value = res;
        }

        protected override void HandleCloned(PropertyData res)
        {
            GuidPropertyData cloningProperty = (GuidPropertyData)res;

            cloningProperty.Value = new Guid(Value.ToByteArray());
        }
    }
}