﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Stardew.SCCL.NBT {
    public class NBTIntArray : NBTBase {
        public int[] Value { get; set; }

        protected override void ReadData(BinaryReader stream) {
            int length = stream.ReadInt32();
            Value = new int[length];
            for (int i = 0; i < length; i++)
                Value[i] = stream.ReadInt32();
        }

        protected override void WriteData(BinaryWriter stream) {
            stream.Write(Value.Length);
            for (int i = 0; i < Value.Length; i++)
                stream.Write(Value[i]);
        }
    }
}
