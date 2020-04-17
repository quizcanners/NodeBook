﻿using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace QcTriggerLogic
{

    public class LogicBranch<T> : AbstractKeepUnrecognizedCfg  , IGotName , IPEGI, IAmConditional, ICanBeDefaultCfg, IPEGI_Searchable  where T: ICfg, new() {

        public string name = "no name";

        public List<LogicBranch<T>> subBranches = new List<LogicBranch<T>>();

        public ConditionBranch conditions = new ConditionBranch();

        public List<T> elements = new List<T>();

        public override bool IsDefault => subBranches.Count ==0 && conditions.IsDefault && elements.Count == 0;

        public List<T> CollectAll(ref List<T> lst) {

            lst.AddRange(elements);

            foreach (var b in subBranches)
                b.CollectAll(ref lst);

            return lst;
        }

        public bool CheckConditions(Values values) => conditions.CheckConditions(Values.global);

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("name", name)
            .Add("cond", conditions)
            .Add_IfNotEmpty("sub", subBranches)
            .Add_IfNotEmpty("el", elements)
            .Add_IfNotNegative("ie", _inspectedElement)
            .Add_IfNotNegative("is", _inspectedItems)
            .Add_IfNotNegative("br", _inspectedBranch);
        
        public override bool Decode(string tg, string data)
        {
            switch (tg)
            {
                case "name": name = data; break;
                case "cond": conditions.Decode(data); break;
                case "sub": data.Decode_List(out subBranches); break;
                case "el": data.Decode_List(out elements); break;
                case "ie": _inspectedElement = data.ToInt(); break;
                case "is": _inspectedItems = data.ToInt(); break;
                case "br": _inspectedBranch = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector

        public virtual string NameForElements => typeof(T).ToPegiStringType();

        public string NameForPEGI
        {
            get { return name; }
            set { name = value; }
        }

        public override void ResetInspector() {
            _inspectedElement = -1;
            _inspectedBranch = -1;
            base.ResetInspector();
        }

        private int _inspectedElement = -1;
        private int _inspectedBranch = -1;
   
        LoopLock searchLoopLock = new LoopLock();

        public bool String_SearchMatch(string searchString)
        {
            if (searchLoopLock.Unlocked)
                using(searchLoopLock.Lock()){

                    if (pegi.Try_SearchMatch_Obj(conditions, searchString))
                        return true;

                    foreach (var e in elements)
                        if (pegi.Try_SearchMatch_Obj(e, searchString))
                            return true;

                    foreach (var sb in subBranches)
                        if (pegi.Try_SearchMatch_Obj(sb, searchString))
                            return true;
                }

            return false;

        }


        static LogicBranch<T> parent;

        public override bool Inspect() {
            var changed = false;
         
            pegi.nl();

            if (parent != null || conditions.CountForInspector()>0)
                conditions.enter_Inspect_AsList(ref _inspectedItems, 1).nl(ref changed);
            
            parent = this;

            NameForElements.enter_List(ref elements, ref _inspectedElement, ref _inspectedItems, 2).nl(ref changed);

            ("Subs: " + NameForPEGI).enter_List(ref subBranches, ref _inspectedBranch, ref _inspectedItems, 3).nl(ref changed);

            parent = null;
            return changed;
        }
        
        #endregion

    }
}