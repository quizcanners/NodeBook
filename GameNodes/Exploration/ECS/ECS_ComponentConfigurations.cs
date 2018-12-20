﻿using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace NodeNotes_Visual.ECS {
    
    #region Phisics Array
    
    [Serializable]
    public struct PhisicsArrayDynamic_Component : IComponentData, IGotDisplayName, IPEGI_ListInspect, ISTD {
        public uint phisixIndex;

        public float testValue;
        
        #region Inspector
        public string NameForPEGIdisplay => "Phisics Array index";

        public bool PEGI_inList(IList list, int ind, ref int edited) {
            var changed = "PhisX Index".edit(80, ref phisixIndex);

            "Test value".edit(60, ref testValue).changes(ref changed);

            if (icon.Refresh.Click("Reset test value").changes(ref changed))
                testValue = 0;

            return changed;
        }

        #endregion

        #region Encode & Decode
        public StdEncoder Encode() => new StdEncoder()
            .Add("i", phisixIndex)
            .Add("tv", testValue);

        public bool Decode(string tag, string data) {
            switch (tag) {
                case "i": phisixIndex = data.ToUInt(); break;
                case "tv": testValue = data.ToFloat(); break;
                default: return false;
            }
            return true;
        }

        public void Decode(string data) => data.DecodeTagsFor(this);
        #endregion

    }

    [TaggedType(classTag, "Phisics Array Index")]
    public class PhisicsArrayDynamic_STD : Component_STD_Generic<PhisicsArrayDynamic_Component>
    {

        #region Tagged Class
        const string classTag = "phArr";
        public override string ClassTag => classTag;
        #endregion

        #region Set Data

        public override void SetData(Entity e) => e.Set(new PhisicsArrayDynamic_Component() { phisixIndex = 0 });

        #endregion
    }
    #endregion

    
    #region Unity Native Components
    [TaggedType(classTag, "Position")]
    public class Position_STD : Component_STD_Generic<Position>, IPEGI_ListInspect
    {
        const string classTag = "pos";

        Vector3 pos;

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_IfNotZero("pos", pos);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "pos": pos = data.ToVector3(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public override string ClassTag => classTag;

        public override bool PEGI_inList(IList list, int ind, ref int edited) => "pos".edit(30, ref pos);

        public static Entity Set(Entity e, Vector3 pos) => e.Set(new Position() { Value = new float3(pos.x, pos.y, pos.z) });

        public override void SetData(Entity e) => Set(e, pos);
    }

    [TaggedType(classTag, "Rotation")]
    public class Rotation_STD : Component_STD_Generic<Rotation>, IPEGI_ListInspect {

        const string classTag = "rot";
        Quaternion qt;
        public override string ClassTag => classTag;

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add(classTag, qt);

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case classTag: qt = data.ToQuaternion(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public override void SetData(Entity e) => e.Set(new Rotation() { Value = new quaternion() { value = new float4(qt.x, qt.y, qt.z, qt.w) } });

        public override bool PEGI_inList(IList list, int ind, ref int edited) => "Rotation".edit(60, ref qt);

    }
    #endregion

}