﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedTools_Stuff;
using PlayerAndEditorGUI;
using System;
using STD_Logic;

namespace LinkedNotes
{
    
    public class NodeBook : AbstractKeepUnrecognized_STD, IPEGI_ListInspect, IPEGI, IGotIndex {

        public string name;
        public int firstFree = 0;
        public CountlessSTD<Base_Node> allBaseNodes = new CountlessSTD<Base_Node>();
        public Node subNode; 

        int indexInList = 0;

        public int IndexForPEGI
        {
            get
            {
                return indexInList;
            }

            set
            {
                indexInList = value;
            }
        }

#if PEGI
        int inspectedNode = -1;
        
        public override bool PEGI()  {
            bool changed = false;

            changed |= subNode.Nested_Inspect();

            return changed;
        }
        
        public bool PEGI_inList(IList list, int ind, ref int edited) {
           var changed = pegi.edit(ref name);

            if (icon.Edit.Click())
                edited = ind;
            return changed;
        }
#endif

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("f", firstFree)
            .Add("sn", subNode)
            .Add_String("n", name)
            .Add("in", inspectedNode);
          

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "f": firstFree = data.ToInt(); break;
                case "sn": data.DecodeInto(out subNode); break;
                case "n": name = data; break;
                case "in": inspectedNode = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
    
        public override ISTD Decode(string data) {
            
           var ret = data.DecodeTagsFor(this);

            if (subNode == null)
                subNode = new Node();

            subNode.Init(this, null);
            
            return ret;
        }

    }
}